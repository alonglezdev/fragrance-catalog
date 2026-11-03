using FragranceCatalog.Application.Common;
using FragranceCatalog.Domain.Pricing;

namespace FragranceCatalog.Application.Pricing;

/// <summary>
/// Shows what a pricing change would do without saving anything. Current and proposed prices both come from
/// <see cref="PriceCalculator"/>, so the preview matches the real price once the change is saved.
/// The proposal is built on new objects: the loaded configuration is never modified.
/// </summary>
/// <param name="reader">Current pricing data.</param>
/// <param name="store">Checks a proposed rule the same way saving it would.</param>
/// <param name="timeProvider">Instant the prices are calculated for.</param>
public sealed class PreviewPricingChange(IPricingReader reader, IPricingStore store, TimeProvider timeProvider)
{
    /// <summary>Validates the proposal, reporting every problem together, and compares prices if there are none.</summary>
    /// <param name="request">The change to try out.</param>
    /// <param name="cancellationToken">Stops the operation.</param>
    public async Task<Result<PricingPreviewDto>> ExecuteAsync(PreviewPricingChangeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var current = await reader.GetConfigurationAsync(cancellationToken);
        var errors = await FindErrorsAsync(request, current, cancellationToken);
        if (errors.Count > 0)
        {
            return Result.Failure<PricingPreviewDto>(errors);
        }

        var now = timeProvider.GetUtcNow();
        var proposed = BuildProposal(request, current, now);
        var products = await reader.GetProductCostsAsync(cancellationToken);
        var changes = products
            .Select(product => Compare(product, current, proposed, now))
            .Where(item => item.CurrentPrice != item.ProposedPrice)
            .ToList();
        return Result.Success(new PricingPreviewDto(products.Count, changes.Count, changes));
    }

    private static PricingPreviewItemDto Compare(ProductCost product, PricingConfiguration current, PricingConfiguration proposed, DateTimeOffset now)
    {
        var currentQuote = PriceCalculator.Calculate(product.Cost, product.BrandId, product.ProductId, current, now);
        var proposedQuote = PriceCalculator.Calculate(product.Cost, product.BrandId, product.ProductId, proposed, now);

        return new PricingPreviewItemDto(
            product.ProductId,
            product.Name,
            product.BrandName,
            product.Cost,
            currentQuote.SalePrice,
            proposedQuote.SalePrice,
            proposedQuote.Profit,
            proposedQuote.IsLowProfit);
    }

    private static PricingConfiguration BuildProposal(PreviewPricingChangeRequest request, PricingConfiguration current, DateTimeOffset now) => new(
        BuildSettings(request.Settings, current.Settings),
        BuildRanges(request.Ranges, current.Ranges),
        BuildRules(request, current.Rules, now));

    private static PricingSettings BuildSettings(PricingSettingsValues? values, PricingSettings currentSettings)
    {
        if (values is null)
        {
            return currentSettings;
        }

        var proposedSettings = PricingSettings.CreateDefault();
        values.ApplyTo(proposedSettings);
        return proposedSettings;
    }

    private static IReadOnlyList<PriceRange> BuildRanges(IReadOnlyCollection<PriceRangeDefinition>? definitions, IReadOnlyList<PriceRange> currentRanges) =>
        definitions is null
            ? currentRanges
            : definitions.Select(range => PriceRange.Create(range.MinCost, range.MaxCost, range.Action, range.Value)).ToList();

    private static List<PricingRule> BuildRules(PreviewPricingChangeRequest request, IReadOnlyCollection<PricingRule> currentRules, DateTimeOffset now)
    {
        var rules = currentRules.ToList();
        if (request.RuleChange is { } change)
        {
            var existingRule = rules.Single(rule => rule.Id == change.RuleId);
            rules.Remove(existingRule);
            rules.Add(CopyWithChange(existingRule, change, now));
        }

        if (request.NewRule is { } rule)
        {
            rules.Add(PricingRule.ForTarget(rule.Scope, rule.TargetId, rule.Action, rule.Value, rule.StartsAt, rule.EndsAt, now));
        }

        return rules;
    }

    private static PricingRule CopyWithChange(PricingRule existingRule, PricingRuleChange change, DateTimeOffset now)
    {
        var copy = PricingRule.ForTarget(existingRule.Scope, existingRule.TargetId, change.Action, change.Value, change.StartsAt, change.EndsAt, now);
        if (!existingRule.IsActive)
        {
            copy.Deactivate();
        }

        return copy;
    }

    private async Task<List<Error>> FindErrorsAsync(PreviewPricingChangeRequest request, PricingConfiguration current, CancellationToken cancellationToken)
    {
        var errors = new List<Error>();
        if (request.Settings is { } settings)
        {
            errors.AddRange(PricingErrors.From(settings.Validate()));
        }

        if (request.Ranges is { } ranges)
        {
            errors.AddRange(PricingErrors.From(PriceRangeValidator.ValidateDefinitions(ranges)));
        }

        if (request.RuleChange is { } change)
        {
            errors.AddRange(FindRuleChangeErrors(change, current));
        }

        if (request.NewRule is { } newRule)
        {
            errors.AddRange(await NewPricingRuleChecks.FindErrorsAsync(store, newRule, cancellationToken));
        }

        return errors;
    }

    private static IEnumerable<Error> FindRuleChangeErrors(PricingRuleChange change, PricingConfiguration current)
    {
        var errors = PricingErrors.From(PricingRule.Validate(change.Action, change.Value, change.StartsAt, change.EndsAt));
        return current.Rules.Any(rule => rule.Id == change.RuleId)
            ? errors
            : errors.Append(PricingErrors.RuleNotFound(change.RuleId));
    }
}
