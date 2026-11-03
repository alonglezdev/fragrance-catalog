using FragranceCatalog.Application.Common;
using FragranceCatalog.Domain.Pricing;

namespace FragranceCatalog.Application.Pricing;

/// <summary>Turns domain validation results and pricing business rules into the errors the panel shows.</summary>
public static class PricingErrors
{
    /// <summary>Code of an edit to a rule that does not exist.</summary>
    public const string PricingRuleNotFound = nameof(PricingRuleNotFound);

    /// <summary>Code of a rule for a brand that does not exist.</summary>
    public const string BrandNotFound = nameof(BrandNotFound);

    /// <summary>Code of a rule for a product that does not exist.</summary>
    public const string ProductNotFound = nameof(ProductNotFound);

    /// <summary>Code of a second active rule for the same brand or product.</summary>
    public const string DuplicateActiveRule = nameof(DuplicateActiveRule);

    /// <summary>Error for a rule that does not exist.</summary>
    /// <param name="ruleId">Identifier that was not found.</param>
    public static Error RuleNotFound(Guid ruleId) => Error.NotFound(PricingRuleNotFound, ruleId);

    /// <summary>Error for a rule whose brand or product does not exist.</summary>
    /// <param name="scope">Whether the target is a brand or a product.</param>
    /// <param name="targetId">Identifier that was not found.</param>
    public static Error TargetNotFound(PricingRuleScope scope, Guid targetId) =>
        Error.NotFound(scope == PricingRuleScope.Brand ? BrandNotFound : ProductNotFound, targetId);

    /// <summary>Error for a second active rule on the same target.</summary>
    /// <param name="targetId">The brand or product that already has an active rule.</param>
    public static Error DuplicateRule(Guid targetId) => Error.Conflict(DuplicateActiveRule, targetId.ToString());

    /// <summary>Errors of the pricing settings.</summary>
    /// <param name="errors">Result of the domain validation.</param>
    public static IEnumerable<Error> From(IEnumerable<PricingSettingsError> errors) =>
        errors.Select(error => Error.Validation(error.Code, error.Value));

    /// <summary>Errors of a pricing rule.</summary>
    /// <param name="errors">Result of the domain validation.</param>
    public static IEnumerable<Error> From(IEnumerable<PricingRuleError> errors) =>
        errors.Select(error => Error.Validation(error.Code, error.Value));

    /// <summary>Errors of the price ranges.</summary>
    /// <param name="errors">Result of the domain validation.</param>
    public static IEnumerable<Error> From(IEnumerable<PriceRangeError> errors) =>
        errors.Select(error => Error.Validation(error.Code, error.Value, error.Position));
}
