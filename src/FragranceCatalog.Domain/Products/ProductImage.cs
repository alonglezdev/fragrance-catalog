namespace FragranceCatalog.Domain.Products;

/// <summary>Photo of a product hosted by the supplier, shown in the order the supplier lists them.</summary>
public sealed class ProductImage
{
    private ProductImage(Guid id, Guid productId, string url, int sortOrder)
    {
        Id = id;
        ProductId = productId;
        Url = url;
        SortOrder = sortOrder;
    }

    /// <summary>Identifier of the image.</summary>
    public Guid Id { get; private set; }

    /// <summary>Product the photo belongs to.</summary>
    public Guid ProductId { get; private set; }

    /// <summary>Absolute URL of the photo on the supplier CDN.</summary>
    public string Url { get; private set; }

    /// <summary>Zero-based position; the first image is the main photo.</summary>
    public int SortOrder { get; private set; }

    internal static ProductImage Create(Guid productId, string url, int sortOrder) =>
        new(Guid.CreateVersion7(), productId, url, sortOrder);
}
