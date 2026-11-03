namespace FragranceCatalog.Application.Pricing;

/// <summary>What a pricing change would do, before saving it.</summary>
/// <param name="ProductCount">Products considered: all those not discontinued.</param>
/// <param name="ChangedCount">Products whose price would change.</param>
/// <param name="Changes">Those products with their current and proposed prices.</param>
public sealed record PricingPreviewDto(int ProductCount, int ChangedCount, IReadOnlyList<PricingPreviewItemDto> Changes);
