namespace FragranceCatalog.Infrastructure.Suppliers.Magna;

/// <summary>
/// Where and how to read the Magna Perfumes catalog. Everything that depends on Magna's HTML lives here so a site change
/// is fixed in appsettings, not in code. Not editable from the admin panel: a wrong value breaks the whole sync.
/// </summary>
public sealed class MagnaOptions
{
    /// <summary>Configuration section that holds these options.</summary>
    public const string SectionName = "Suppliers:Magna";

    /// <summary>Sitemap index that lists the product sitemap pages.</summary>
    public string SitemapUrl { get; set; } = "https://magnaperfumes.com/sitemap.xml";

    /// <summary>Value of the <c>type</c> query parameter that marks product sitemaps in the index.</summary>
    public string ProductSitemapType { get; set; } = "products";

    /// <summary>User-Agent sent with every request. Temporary browser value; can be replaced by one that identifies the store.</summary>
    public string UserAgent { get; set; } =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/142.0.0.0 Safari/537.36";

    /// <summary>Image width segment requested from the BigCommerce CDN, such as "1280w".</summary>
    public string ImageWidth { get; set; } = "1280w";

    /// <summary>CSS selector of the JSON-LD scripts; the one describing a schema.org Product with offers is used.</summary>
    public string JsonLdSelector { get; set; } = "script[type='application/ld+json']";

    /// <summary>CSS selector of the main product gallery images, excluding thumbnails and related products.</summary>
    public string GalleryImageSelector { get; set; } = "img[class*='product-gallery-image-background'].object-contain";

    /// <summary>Start of the inline scripts that carry the Catalyst (Next.js) page data.</summary>
    public string CatalystPayloadMarker { get; set; } = "self.__next_f.push(";

    /// <summary>Name of Magna's custom product field that states the audience (Dama, Caballero, Unisex).</summary>
    public string GenderFieldName { get; set; } = "Género";
}
