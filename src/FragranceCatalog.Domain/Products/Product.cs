using FragranceCatalog.Domain.Common;

namespace FragranceCatalog.Domain.Products;

/// <summary>
/// A fragrance offered by a supplier. Catalog data is refreshed by the sync, while visibility and featuring
/// belong to the administrator and are never changed by the sync.
/// </summary>
public sealed class Product
{
    private readonly List<ProductImage> _images = [];
    private readonly List<SupplierPrice> _supplierPrices = [];

    private Product(Guid id, Guid supplierId, string supplierProductId, Slug slug, string sourceUrl, string name, string description)
    {
        Id = id;
        SupplierId = supplierId;
        SupplierProductId = supplierProductId;
        Slug = slug;
        SourceUrl = sourceUrl;
        Name = name;
        Description = description;
    }

    /// <summary>Identifier of the product.</summary>
    public Guid Id { get; private set; }

    /// <summary>Supplier the product is imported from.</summary>
    public Guid SupplierId { get; private set; }

    /// <summary>The supplier's own identifier, unique per supplier, used to match products across syncs.</summary>
    public string SupplierProductId { get; private set; }

    /// <summary>Product page on the supplier site; internal data never shown publicly.</summary>
    public string SourceUrl { get; private set; }

    /// <summary>Product name as the supplier writes it.</summary>
    public string Name { get; private set; }

    /// <summary>Unique public URL identifier, kept stable even if the supplier renames the product.</summary>
    public Slug Slug { get; private set; }

    /// <summary>Brand the product belongs to.</summary>
    public Guid BrandId { get; private set; }

    /// <summary>Audience inferred from the name.</summary>
    public Gender Gender { get; private set; }

    /// <summary>Bottle size in milliliters, or null when unknown.</summary>
    public int? SizeMl { get; private set; }

    /// <summary>Plain-text description; may be empty.</summary>
    public string Description { get; private set; }

    /// <summary>Whether the supplier currently has stock.</summary>
    public bool IsAvailable { get; private set; }

    /// <summary>The product left the supplier catalog; it is kept instead of deleted so its history and rules survive.</summary>
    public bool IsDiscontinued { get; private set; }

    /// <summary>The administrator removed it from the public catalog.</summary>
    public bool IsHidden { get; private set; }

    /// <summary>The administrator promotes it in the featured list.</summary>
    public bool IsFeatured { get; private set; }

    /// <summary>UTC instant the product was first imported.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>UTC instant the product was last found in the supplier catalog.</summary>
    public DateTimeOffset LastSeenAt { get; private set; }

    /// <summary>Photos in display order.</summary>
    public IReadOnlyList<ProductImage> Images => _images;

    /// <summary>Every distinct price the supplier has charged.</summary>
    public IReadOnlyList<SupplierPrice> SupplierPrices => _supplierPrices;

    /// <summary>The price the supplier charges now, which is the cost used to calculate the sale price.</summary>
    public SupplierPrice CurrentSupplierPrice =>
        _supplierPrices.MaxBy(price => price.CapturedAt)
        ?? throw new InvalidOperationException($"Product {Id} was loaded without its supplier prices.");

    /// <summary>
    /// Checks supplier data before it is used, so the sync can set aside a product the domain would reject instead of failing while saving it.
    /// </summary>
    /// <param name="details">Supplier-owned product data.</param>
    /// <param name="regularPrice">Supplier list price.</param>
    /// <param name="salePrice">Supplier offer price, or null when there is no offer.</param>
    /// <param name="imageUrls">Photo URLs.</param>
    /// <exception cref="ArgumentException">Some of the data is not acceptable; the message says which.</exception>
    public static void ValidateSupplierData(SupplierProductDetails details, decimal regularPrice, decimal? salePrice, IEnumerable<string> imageUrls)
    {
        ValidateDetails(details);
        SupplierPrice.ValidateAmounts(regularPrice, salePrice);
        ArgumentNullException.ThrowIfNull(imageUrls);
        foreach (var url in imageUrls)
        {
            Guard.ThrowIfNotAbsoluteUrl(url, nameof(imageUrls));
        }
    }

    /// <summary>Imports a product found for the first time in the supplier catalog, together with its first price, so a product never exists without a cost.</summary>
    /// <param name="supplierId">Supplier the product comes from.</param>
    /// <param name="supplierProductId">The supplier's own identifier for the product.</param>
    /// <param name="slug">Unique slug; the caller guarantees it is not already taken.</param>
    /// <param name="details">Supplier-owned product data.</param>
    /// <param name="regularPrice">Supplier list price, above zero.</param>
    /// <param name="salePrice">Supplier offer price above zero, or null when there is no offer.</param>
    /// <param name="seenAt">Instant the sync read the product.</param>
    public static Product Create(
        Guid supplierId,
        string supplierProductId,
        Slug slug,
        SupplierProductDetails details,
        decimal regularPrice,
        decimal? salePrice,
        DateTimeOffset seenAt)
    {
        Guard.ThrowIfEmpty(supplierId);
        ArgumentException.ThrowIfNullOrWhiteSpace(supplierProductId);
        ArgumentNullException.ThrowIfNull(slug);
        ValidateDetails(details);

        var product = new Product(Guid.CreateVersion7(), supplierId, supplierProductId.Trim(), slug, details.SourceUrl, details.Name, details.Description)
        {
            CreatedAt = seenAt.ToUniversalTime(),
        };
        product.ApplyDetails(details, seenAt);
        product._supplierPrices.Add(SupplierPrice.Create(product.Id, regularPrice, salePrice, seenAt));
        return product;
    }

    /// <summary>Refreshes supplier-owned data and records that the product is still in the catalog.</summary>
    /// <param name="details">Latest supplier-owned product data.</param>
    /// <param name="seenAt">Instant the sync read the product.</param>
    public void UpdateFromSupplier(SupplierProductDetails details, DateTimeOffset seenAt)
    {
        ValidateDetails(details);
        ApplyDetails(details, seenAt);
    }

    /// <summary>Replaces the photos with the supplier's current list, dropping duplicates and keeping the supplier's order. An unchanged list keeps the existing rows, so a daily sync does not rewrite them.</summary>
    /// <param name="imageUrls">Absolute photo URLs, main photo first.</param>
    public void ReplaceImages(IEnumerable<string> imageUrls)
    {
        ArgumentNullException.ThrowIfNull(imageUrls);

        var uniqueUrls = imageUrls.Distinct(StringComparer.Ordinal).ToList();
        foreach (var url in uniqueUrls)
        {
            Guard.ThrowIfNotAbsoluteUrl(url, nameof(imageUrls));
        }

        if (_images.Select(image => image.Url).SequenceEqual(uniqueUrls, StringComparer.Ordinal))
        {
            return;
        }

        _images.Clear();
        _images.AddRange(uniqueUrls.Select((url, position) => ProductImage.Create(Id, url, position)));
    }

    /// <summary>Adds a price history entry only when the amounts differ from the current price.</summary>
    /// <param name="regularPrice">Supplier list price.</param>
    /// <param name="salePrice">Supplier offer price, or null when there is no offer.</param>
    /// <param name="capturedAt">Instant the sync read the price.</param>
    /// <returns>True when the price changed and a new entry was recorded.</returns>
    public bool RecordSupplierPrice(decimal regularPrice, decimal? salePrice, DateTimeOffset capturedAt)
    {
        if (CurrentSupplierPrice.HasSameAmountsAs(regularPrice, salePrice))
        {
            return false;
        }

        _supplierPrices.Add(SupplierPrice.Create(Id, regularPrice, salePrice, capturedAt));
        return true;
    }

    /// <summary>Marks the product as gone from the supplier catalog.</summary>
    public void MarkAsDiscontinued() => IsDiscontinued = true;

    /// <summary>Brings back a discontinued product that reappeared in the supplier catalog.</summary>
    public void Reactivate() => IsDiscontinued = false;

    /// <summary>Removes the product from the public catalog by the administrator's decision.</summary>
    public void Hide() => IsHidden = true;

    /// <summary>Returns a hidden product to the public catalog.</summary>
    public void Show() => IsHidden = false;

    /// <summary>Adds the product to the featured list.</summary>
    public void Feature() => IsFeatured = true;

    /// <summary>Removes the product from the featured list.</summary>
    public void Unfeature() => IsFeatured = false;

    private static void ValidateDetails(SupplierProductDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);
        Guard.ThrowIfNotAbsoluteUrl(details.SourceUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(details.Name);
        Guard.ThrowIfEmpty(details.BrandId);
        Guard.ThrowIfUndefined(details.Gender);
        ArgumentNullException.ThrowIfNull(details.Description);
        if (details.SizeMl is { } sizeMl)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sizeMl, nameof(details.SizeMl));
        }
    }

    private void ApplyDetails(SupplierProductDetails details, DateTimeOffset seenAt)
    {
        SourceUrl = details.SourceUrl;
        Name = details.Name.Trim();
        BrandId = details.BrandId;
        Gender = details.Gender;
        SizeMl = details.SizeMl;
        Description = details.Description.Trim();
        IsAvailable = details.IsAvailable;
        LastSeenAt = seenAt.ToUniversalTime();
    }
}
