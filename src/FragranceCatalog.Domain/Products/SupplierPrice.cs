namespace FragranceCatalog.Domain.Products;

/// <summary>
/// Price the supplier charged at a given moment. A new entry is recorded only when the amounts change, so the list is a price history.
/// </summary>
public sealed class SupplierPrice
{
    private SupplierPrice(Guid id, Guid productId, decimal regularPrice, decimal? salePrice, DateTimeOffset capturedAt)
    {
        Id = id;
        ProductId = productId;
        RegularPrice = regularPrice;
        SalePrice = salePrice;
        CapturedAt = capturedAt;
    }

    /// <summary>Identifier of the price entry.</summary>
    public Guid Id { get; private set; }

    /// <summary>Product the price belongs to.</summary>
    public Guid ProductId { get; private set; }

    /// <summary>Supplier list price in MXN.</summary>
    public decimal RegularPrice { get; private set; }

    /// <summary>Supplier discounted price in MXN, or null when there is no offer.</summary>
    public decimal? SalePrice { get; private set; }

    /// <summary>UTC instant the sync read this price.</summary>
    public DateTimeOffset CapturedAt { get; private set; }

    /// <summary>What the store actually pays: the offer when there is one, otherwise the list price.</summary>
    public decimal Cost => SalePrice ?? RegularPrice;

    internal static SupplierPrice Create(Guid productId, decimal regularPrice, decimal? salePrice, DateTimeOffset capturedAt)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(regularPrice);
        if (salePrice is { } offer)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(offer, nameof(salePrice));
        }

        return new SupplierPrice(Guid.CreateVersion7(), productId, regularPrice, salePrice, capturedAt.ToUniversalTime());
    }

    internal bool HasSameAmountsAs(decimal regularPrice, decimal? salePrice) =>
        RegularPrice == regularPrice && SalePrice == salePrice;
}
