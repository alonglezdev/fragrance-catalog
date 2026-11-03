using System.Text;
using System.Text.Json;
using AngleSharp.Dom;
using FragranceCatalog.Application.Sync;

namespace FragranceCatalog.Infrastructure.Suppliers.Magna;

internal sealed record CatalystProduct(string EntityId, decimal BasePrice, decimal? SalePrice, string? GenderLabel)
{
    private const int PayloadChunkKind = 1;

    public static CatalystProduct? Find(IDocument document, string payloadMarker, JsonLdProduct jsonLdProduct, string genderFieldName)
    {
        foreach (var row in ReadPayload(document, payloadMarker).Split('\n'))
        {
            var separator = row.IndexOf(':', StringComparison.Ordinal);
            if (separator < 0 || !row.Contains("\"inventory\"", StringComparison.Ordinal))
            {
                continue;
            }

            using var json = JsonDocuments.TryParse(row[(separator + 1)..]);
            if (json is not null && FindMatchingProduct(json.RootElement, jsonLdProduct) is { } product)
            {
                return FromProduct(product, genderFieldName);
            }
        }

        return null;
    }

    private static string ReadPayload(IDocument document, string payloadMarker)
    {
        var payload = new StringBuilder();
        foreach (var script in document.Scripts)
        {
            var text = script.TextContent.Trim();
            if (text.StartsWith(payloadMarker, StringComparison.Ordinal) && text.EndsWith(')'))
            {
                payload.Append(ReadChunk(text[payloadMarker.Length..^1]));
            }
        }

        return payload.ToString();
    }

    private static string ReadChunk(string arguments)
    {
        using var json = JsonDocuments.TryParse(arguments);
        if (json is null || json.RootElement.ValueKind != JsonValueKind.Array || json.RootElement.GetArrayLength() < 2)
        {
            return string.Empty;
        }

        var kind = json.RootElement[0];
        var chunk = json.RootElement[1];
        var isTextChunk = kind.ValueKind == JsonValueKind.Number && kind.GetInt32() == PayloadChunkKind && chunk.ValueKind == JsonValueKind.String;
        return isTextChunk ? chunk.GetString()! : string.Empty;
    }

    private static JsonElement? FindMatchingProduct(JsonElement element, JsonLdProduct jsonLdProduct)
    {
        if (element.ValueKind == JsonValueKind.Object && IsSameProduct(element, jsonLdProduct))
        {
            return element;
        }

        var children = element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject().Select(property => property.Value),
            JsonValueKind.Array => element.EnumerateArray(),
            _ => [],
        };

        return children.Select(child => FindMatchingProduct(child, jsonLdProduct)).FirstOrDefault(match => match is not null);
    }

    private static bool IsSameProduct(JsonElement element, JsonLdProduct jsonLdProduct)
    {
        var isCatalystProduct = element.TryGetProperty("entityId", out _)
            && element.TryGetProperty("prices", out _)
            && element.TryGetProperty("inventory", out _);
        if (!isCatalystProduct)
        {
            return false;
        }

        return jsonLdProduct.Sku is { } sku
            ? JsonDocuments.GetOptionalString(element, "sku") == sku
            : JsonDocuments.GetOptionalString(element, "name") == jsonLdProduct.Name;
    }

    private static CatalystProduct FromProduct(JsonElement product, string genderFieldName)
    {
        var prices = product.GetProperty("prices");

        return new CatalystProduct(
            product.GetProperty("entityId").GetRawText(),
            ReadMoney(prices, "basePrice") ?? throw new SupplierCatalogException("The Catalyst product has no base price."),
            ReadMoney(prices, "salePrice"),
            ReadCustomField(product, genderFieldName));
    }

    private static decimal? ReadMoney(JsonElement prices, string propertyName) =>
        prices.TryGetProperty(propertyName, out var money)
        && money.ValueKind == JsonValueKind.Object
        && money.TryGetProperty("value", out var value)
            ? value.GetDecimal()
            : null;

    private static string? ReadCustomField(JsonElement product, string fieldName)
    {
        if (!product.TryGetProperty("customFields", out var customFields) || !customFields.TryGetProperty("edges", out var edges))
        {
            return null;
        }

        return edges.EnumerateArray()
            .Select(edge => edge.GetProperty("node"))
            .Where(node => JsonDocuments.GetOptionalString(node, "name") == fieldName)
            .Select(node => JsonDocuments.GetOptionalString(node, "value"))
            .FirstOrDefault();
    }
}
