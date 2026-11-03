using System.Globalization;
using System.Text.Json;
using AngleSharp.Dom;
using FragranceCatalog.Application.Sync;

namespace FragranceCatalog.Infrastructure.Suppliers.Magna;

internal sealed record JsonLdProduct(
    string Name,
    string Brand,
    string? Sku,
    string Description,
    string? ImageUrl,
    decimal Price,
    bool IsInStock)
{
    private const string InStockSuffix = "InStock";

    public static JsonLdProduct? Read(IDocument document, string selector)
    {
        foreach (var script in document.QuerySelectorAll(selector))
        {
            using var json = JsonDocuments.TryParse(script.TextContent);
            if (json is not null && IsProductWithOffers(json.RootElement))
            {
                return FromProduct(json.RootElement);
            }
        }

        return null;
    }

    private static bool IsProductWithOffers(JsonElement element) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty("@type", out var type)
        && type.ValueKind == JsonValueKind.String
        && type.GetString() == "Product"
        && element.TryGetProperty("offers", out _);

    private static JsonLdProduct FromProduct(JsonElement product)
    {
        var offers = product.GetProperty("offers");

        return new JsonLdProduct(
            JsonDocuments.GetRequiredString(product, "name"),
            ReadBrand(product),
            JsonDocuments.GetOptionalString(product, "sku"),
            JsonDocuments.GetOptionalString(product, "description")?.Trim() ?? string.Empty,
            ReadImageUrl(product),
            ReadPrice(offers),
            JsonDocuments.GetOptionalString(offers, "availability")?.EndsWith(InStockSuffix, StringComparison.Ordinal) == true);
    }

    private static string ReadBrand(JsonElement product)
    {
        if (!product.TryGetProperty("brand", out var brand))
        {
            throw new SupplierCatalogException("The schema.org Product has no brand.");
        }

        return brand.ValueKind == JsonValueKind.String
            ? brand.GetString()!
            : JsonDocuments.GetRequiredString(brand, "name");
    }

    private static string? ReadImageUrl(JsonElement product)
    {
        if (!product.TryGetProperty("image", out var image))
        {
            return null;
        }

        return image.ValueKind switch
        {
            JsonValueKind.String => image.GetString(),
            JsonValueKind.Array when image.GetArrayLength() > 0 => image[0].GetString(),
            _ => null,
        };
    }

    private static decimal ReadPrice(JsonElement offers)
    {
        var priceHolder = offers.TryGetProperty("priceSpecification", out var specification) ? specification : offers;
        if (!priceHolder.TryGetProperty("price", out var price))
        {
            throw new SupplierCatalogException("The schema.org offer has no price.");
        }

        return price.ValueKind == JsonValueKind.String
            ? decimal.Parse(price.GetString()!, NumberStyles.Number, CultureInfo.InvariantCulture)
            : price.GetDecimal();
    }
}
