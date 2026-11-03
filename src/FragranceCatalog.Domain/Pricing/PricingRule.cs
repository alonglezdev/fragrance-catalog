using System.Globalization;
using FragranceCatalog.Domain.Common;

namespace FragranceCatalog.Domain.Pricing;

/// <summary>
/// Pricing exception for one brand or one product that replaces the base strategy. Rules never add up:
/// a product rule wins over a brand rule, and only one rule applies.
/// </summary>
public sealed class PricingRule
{
    private PricingRule(Guid id, PricingRuleScope scope, Guid? brandId, Guid? productId, DateTimeOffset createdAt)
    {
        Id = id;
        Scope = scope;
        BrandId = brandId;
        ProductId = productId;
        CreatedAt = createdAt;
    }

    /// <summary>Identifier of the rule.</summary>
    public Guid Id { get; private set; }

    /// <summary>Whether the rule targets a brand or a product.</summary>
    public PricingRuleScope Scope { get; private set; }

    /// <summary>Targeted brand when <see cref="Scope"/> is <see cref="PricingRuleScope.Brand"/>.</summary>
    public Guid? BrandId { get; private set; }

    /// <summary>Targeted product when <see cref="Scope"/> is <see cref="PricingRuleScope.Product"/>.</summary>
    public Guid? ProductId { get; private set; }

    /// <summary>How the rule turns the cost into a price.</summary>
    public PriceAction Action { get; private set; }

    /// <summary>Amount in MXN, percentage, or fixed price depending on <see cref="Action"/>.</summary>
    public decimal Value { get; private set; }

    /// <summary>UTC instant the rule starts applying, inclusive; null means it applies right away.</summary>
    public DateTimeOffset? StartsAt { get; private set; }

    /// <summary>UTC instant the rule stops applying, exclusive; null means it never expires.</summary>
    public DateTimeOffset? EndsAt { get; private set; }

    /// <summary>The brand or product the rule targets, whichever <see cref="Scope"/> says.</summary>
    public Guid TargetId => BrandId ?? ProductId ?? Guid.Empty;

    /// <summary>Whether the administrator has the rule switched on.</summary>
    public bool IsActive { get; private set; }

    /// <summary>UTC instant the rule was created.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Creates an active rule for every product of a brand.</summary>
    /// <param name="brandId">Targeted brand.</param>
    /// <param name="action">How the cost becomes a price.</param>
    /// <param name="value">Amount, zero or more; percentage within <see cref="MarkupPercent"/>; or fixed price above zero.</param>
    /// <param name="startsAt">Optional start, inclusive.</param>
    /// <param name="endsAt">Optional end, exclusive; must be after the start.</param>
    /// <param name="createdAt">Creation instant.</param>
    public static PricingRule ForBrand(Guid brandId, PriceAction action, decimal value, DateTimeOffset? startsAt, DateTimeOffset? endsAt, DateTimeOffset createdAt)
    {
        return ForTarget(PricingRuleScope.Brand, brandId, action, value, startsAt, endsAt, createdAt);
    }

    /// <summary>Creates an active rule for a single product.</summary>
    /// <param name="productId">Targeted product.</param>
    /// <param name="action">How the cost becomes a price.</param>
    /// <param name="value">Amount, zero or more; percentage within <see cref="MarkupPercent"/>; or fixed price above zero.</param>
    /// <param name="startsAt">Optional start, inclusive.</param>
    /// <param name="endsAt">Optional end, exclusive; must be after the start.</param>
    /// <param name="createdAt">Creation instant.</param>
    public static PricingRule ForProduct(Guid productId, PriceAction action, decimal value, DateTimeOffset? startsAt, DateTimeOffset? endsAt, DateTimeOffset createdAt)
    {
        return ForTarget(PricingRuleScope.Product, productId, action, value, startsAt, endsAt, createdAt);
    }

    /// <summary>Creates an active rule for the brand or product the scope says.</summary>
    /// <param name="scope">Whether the target is a brand or a product.</param>
    /// <param name="targetId">Targeted brand or product.</param>
    /// <param name="action">How the cost becomes a price.</param>
    /// <param name="value">Amount, zero or more; percentage within <see cref="MarkupPercent"/>; or fixed price above zero.</param>
    /// <param name="startsAt">Optional start, inclusive.</param>
    /// <param name="endsAt">Optional end, exclusive; must be after the start.</param>
    /// <param name="createdAt">Creation instant.</param>
    public static PricingRule ForTarget(PricingRuleScope scope, Guid targetId, PriceAction action, decimal value, DateTimeOffset? startsAt, DateTimeOffset? endsAt, DateTimeOffset createdAt)
    {
        Guard.ThrowIfInvalid(Validate(scope, targetId, action, value, startsAt, endsAt));

        var brandId = scope == PricingRuleScope.Brand ? targetId : (Guid?)null;
        var productId = scope == PricingRuleScope.Product ? targetId : (Guid?)null;
        var rule = new PricingRule(Guid.CreateVersion7(), scope, brandId, productId, createdAt.ToUniversalTime());
        rule.Update(action, value, startsAt, endsAt);
        rule.Activate();
        return rule;
    }

    /// <summary>Lists every problem of a new rule, including what it targets; an empty list means it can be created.</summary>
    /// <param name="scope">Whether the target is a brand or a product.</param>
    /// <param name="targetId">Targeted brand or product.</param>
    /// <param name="action">How the cost becomes a price.</param>
    /// <param name="value">Amount, percentage or fixed price.</param>
    /// <param name="startsAt">Optional start.</param>
    /// <param name="endsAt">Optional end.</param>
    public static IReadOnlyList<PricingRuleError> Validate(PricingRuleScope scope, Guid targetId, PriceAction action, decimal value, DateTimeOffset? startsAt, DateTimeOffset? endsAt)
    {
        var errors = new List<PricingRuleError>();
        if (!Enum.IsDefined(scope))
        {
            errors.Add(new PricingRuleError(PricingRuleErrorCode.UnknownRuleScope, scope.ToString()));
        }

        if (targetId == Guid.Empty)
        {
            errors.Add(new PricingRuleError(PricingRuleErrorCode.RuleTargetRequired, targetId.ToString()));
        }

        errors.AddRange(Validate(action, value, startsAt, endsAt));
        return errors;
    }

    /// <summary>Lists every problem of what a rule does and when it applies; an empty list means <see cref="Update"/> will accept it.</summary>
    /// <param name="action">How the cost becomes a price.</param>
    /// <param name="value">Amount, percentage or fixed price.</param>
    /// <param name="startsAt">Optional start.</param>
    /// <param name="endsAt">Optional end.</param>
    public static IReadOnlyList<PricingRuleError> Validate(PriceAction action, decimal value, DateTimeOffset? startsAt, DateTimeOffset? endsAt)
    {
        var errors = new List<PricingRuleError>();
        if (FindActionErrorCode(action, value) is { } actionErrorCode)
        {
            errors.Add(new PricingRuleError(actionErrorCode, value.ToString(CultureInfo.InvariantCulture)));
        }

        if (endsAt <= startsAt)
        {
            errors.Add(new PricingRuleError(PricingRuleErrorCode.EndsNotAfterStart, endsAt.Value.ToString("O", CultureInfo.InvariantCulture)));
        }

        return errors;
    }

    /// <summary>Changes what the rule does and when it applies; the target cannot change.</summary>
    /// <param name="action">How the cost becomes a price.</param>
    /// <param name="value">Amount, zero or more; percentage within <see cref="MarkupPercent"/>; or fixed price above zero.</param>
    /// <param name="startsAt">Optional start, inclusive.</param>
    /// <param name="endsAt">Optional end, exclusive; must be after the start.</param>
    public void Update(PriceAction action, decimal value, DateTimeOffset? startsAt, DateTimeOffset? endsAt)
    {
        Guard.ThrowIfInvalid(Validate(action, value, startsAt, endsAt));

        Action = action;
        Value = value;
        StartsAt = startsAt?.ToUniversalTime();
        EndsAt = endsAt?.ToUniversalTime();
    }

    /// <summary>Switches the rule on; the caller guarantees no other active rule targets the same brand or product.</summary>
    public void Activate() => IsActive = true;

    /// <summary>Switches the rule off without deleting it.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Whether the rule is switched on and the instant falls inside its optional time window.</summary>
    /// <param name="instant">Moment the price is calculated for.</param>
    public bool IsInEffectAt(DateTimeOffset instant) =>
        IsActive && (StartsAt is null || StartsAt <= instant) && (EndsAt is null || instant < EndsAt);

    /// <summary>Whether this is a product rule for the given product.</summary>
    /// <param name="productId">Product being priced.</param>
    public bool TargetsProduct(Guid productId) => Scope == PricingRuleScope.Product && ProductId == productId;

    /// <summary>Whether this is a brand rule for the given brand.</summary>
    /// <param name="brandId">Brand of the product being priced.</param>
    public bool TargetsBrand(Guid brandId) => Scope == PricingRuleScope.Brand && BrandId == brandId;

    private static PricingRuleErrorCode? FindActionErrorCode(PriceAction action, decimal value) =>
        PriceActionValue.FindProblem(action, value) switch
        {
            PriceActionValueProblem.UnknownAction => PricingRuleErrorCode.UnknownPriceAction,
            PriceActionValueProblem.NegativeAmount => PricingRuleErrorCode.NegativeAmount,
            PriceActionValueProblem.PercentOutOfRange => PricingRuleErrorCode.PercentOutOfRange,
            PriceActionValueProblem.FixedPriceNotAboveZero => PricingRuleErrorCode.FixedPriceNotAboveZero,
            _ => null,
        };
}
