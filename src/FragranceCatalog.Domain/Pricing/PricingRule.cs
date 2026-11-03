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
        Guard.ThrowIfEmpty(brandId);

        return Create(PricingRuleScope.Brand, brandId, null, action, value, startsAt, endsAt, createdAt);
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
        Guard.ThrowIfEmpty(productId);

        return Create(PricingRuleScope.Product, null, productId, action, value, startsAt, endsAt, createdAt);
    }

    /// <summary>Changes what the rule does and when it applies; the target cannot change.</summary>
    /// <param name="action">How the cost becomes a price.</param>
    /// <param name="value">Amount, zero or more; percentage within <see cref="MarkupPercent"/>; or fixed price above zero.</param>
    /// <param name="startsAt">Optional start, inclusive.</param>
    /// <param name="endsAt">Optional end, exclusive; must be after the start.</param>
    public void Update(PriceAction action, decimal value, DateTimeOffset? startsAt, DateTimeOffset? endsAt)
    {
        PriceActionValue.ThrowIfInvalid(action, value);
        if (startsAt is { } start && endsAt is { } end)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(end, start, nameof(endsAt));
        }

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

    private static PricingRule Create(
        PricingRuleScope scope,
        Guid? brandId,
        Guid? productId,
        PriceAction action,
        decimal value,
        DateTimeOffset? startsAt,
        DateTimeOffset? endsAt,
        DateTimeOffset createdAt)
    {
        var rule = new PricingRule(Guid.CreateVersion7(), scope, brandId, productId, createdAt.ToUniversalTime());
        rule.Update(action, value, startsAt, endsAt);
        rule.Activate();
        return rule;
    }
}
