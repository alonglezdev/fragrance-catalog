using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using FragranceCatalog.Application.Sync;
using FragranceCatalog.Domain.Products;
using Microsoft.Extensions.Options;

namespace FragranceCatalog.Infrastructure.Suppliers.Magna;

/// <summary>
/// Turns the HTML of a Magna product page into a <see cref="SupplierProduct"/>. It never does HTTP, so it is tested with saved pages.
/// Catalyst data gives the regular/offer split, the stable product id and the gender field; the schema.org price must match one of its two prices.
/// </summary>
/// <param name="options">Selectors and markers that depend on Magna's HTML.</param>
public sealed partial class MagnaProductPageParser(IOptions<MagnaOptions> options)
{
    private const string ImageSizePlaceholder = "{:size}";

    private readonly MagnaOptions _options = options.Value;

    /// <summary>Reads one product page.</summary>
    /// <param name="html">Full HTML of the page.</param>
    /// <param name="sourceUrl">URL the HTML was downloaded from.</param>
    /// <exception cref="SupplierCatalogException">The page lacks product data or its prices contradict each other.</exception>
    public SupplierProduct Parse(string html, string sourceUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(html);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceUrl);

        try
        {
            return ParsePage(html, sourceUrl);
        }
        catch (Exception exception) when (exception is FormatException or OverflowException or InvalidOperationException or KeyNotFoundException or JsonException)
        {
            throw new SupplierCatalogException($"Unexpected product data in {sourceUrl}: {exception.Message}", exception);
        }
    }

    private SupplierProduct ParsePage(string html, string sourceUrl)
    {
        using var document = new HtmlParser().ParseDocument(html);
        var jsonLdProduct = JsonLdProduct.Read(document, _options.JsonLdSelector)
            ?? throw new SupplierCatalogException($"No schema.org Product with offers in {sourceUrl}.");
        var catalystProduct = CatalystProduct.Find(document, _options.CatalystPayloadMarker, jsonLdProduct, _options.GenderFieldName);
        var (regularPrice, salePrice) = ReadPrices(jsonLdProduct, catalystProduct, sourceUrl);
        if (regularPrice <= 0 || salePrice <= 0)
        {
            throw new SupplierCatalogException($"Prices must be above zero in {sourceUrl}.");
        }

        return new SupplierProduct(
            ReadSupplierProductId(jsonLdProduct, catalystProduct, sourceUrl),
            sourceUrl,
            jsonLdProduct.Name,
            jsonLdProduct.Brand,
            regularPrice,
            salePrice,
            ReadImages(document, jsonLdProduct),
            jsonLdProduct.Description,
            ReadGender(catalystProduct, jsonLdProduct.Name),
            MagnaProductName.FindSizeMl(jsonLdProduct.Name),
            jsonLdProduct.IsInStock);
    }

    private static (decimal RegularPrice, decimal? SalePrice) ReadPrices(JsonLdProduct jsonLdProduct, CatalystProduct? catalystProduct, string sourceUrl)
    {
        if (catalystProduct is null)
        {
            return (jsonLdProduct.Price, null);
        }

        var isSchemaPriceFromCatalyst = jsonLdProduct.Price == catalystProduct.SalePrice || jsonLdProduct.Price == catalystProduct.BasePrice;
        if (!isSchemaPriceFromCatalyst)
        {
            throw new SupplierCatalogException(
                $"The schema.org price {jsonLdProduct.Price} matches neither the Catalyst base price {catalystProduct.BasePrice} nor its sale price {catalystProduct.SalePrice} in {sourceUrl}.");
        }

        return (catalystProduct.BasePrice, catalystProduct.SalePrice);
    }

    private static string ReadSupplierProductId(JsonLdProduct jsonLdProduct, CatalystProduct? catalystProduct, string sourceUrl)
    {
        if (catalystProduct is not null)
        {
            return catalystProduct.EntityId;
        }

        var imageMatch = jsonLdProduct.ImageUrl is null ? Match.Empty : ProductIdInImageUrl().Match(jsonLdProduct.ImageUrl);
        return imageMatch.Success
            ? imageMatch.Groups["productId"].Value
            : throw new SupplierCatalogException($"No supplier product id in {sourceUrl}.");
    }

    private static Gender ReadGender(CatalystProduct? catalystProduct, string name)
    {
        var genderFromField = MagnaProductName.FindGender(catalystProduct?.GenderLabel);
        return genderFromField != Gender.Unspecified ? genderFromField : MagnaProductName.FindGender(name);
    }

    private IReadOnlyList<string> ReadImages(IDocument document, JsonLdProduct jsonLdProduct)
    {
        var galleryUrls = document.QuerySelectorAll(_options.GalleryImageSelector)
            .Select(FirstImageUrl)
            .OfType<string>()
            .Select(WithConfiguredWidth)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (galleryUrls.Count > 0)
        {
            return galleryUrls;
        }

        return jsonLdProduct.ImageUrl is null ? [] : [WithConfiguredWidth(jsonLdProduct.ImageUrl)];
    }

    private static string? FirstImageUrl(IElement image)
    {
        var candidates = image.GetAttribute("srcset") ?? image.GetAttribute("src");
        var firstCandidate = candidates?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return firstCandidate?.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
    }

    private string WithConfiguredWidth(string imageUrl) =>
        StencilSize().Replace(imageUrl.Replace(ImageSizePlaceholder, _options.ImageWidth, StringComparison.Ordinal), $"/stencil/{_options.ImageWidth}/");

    [GeneratedRegex(@"/stencil/[^/]+/")]
    private static partial Regex StencilSize();

    [GeneratedRegex(@"/products/(?<productId>\d+)/")]
    private static partial Regex ProductIdInImageUrl();
}
