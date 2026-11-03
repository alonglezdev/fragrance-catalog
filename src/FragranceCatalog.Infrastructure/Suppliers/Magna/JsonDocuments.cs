using System.Text.Json;
using FragranceCatalog.Application.Sync;

namespace FragranceCatalog.Infrastructure.Suppliers.Magna;

internal static class JsonDocuments
{
    public static JsonDocument? TryParse(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.Length == 0 || (trimmed[0] != '{' && trimmed[0] != '['))
        {
            return null;
        }

        try
        {
            return JsonDocument.Parse(trimmed);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string GetRequiredString(JsonElement element, string propertyName) =>
        GetOptionalString(element, propertyName)
        ?? throw new SupplierCatalogException($"Required property '{propertyName}' is missing.");

    public static string? GetOptionalString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
}
