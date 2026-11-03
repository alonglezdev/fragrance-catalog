using System.Xml.Linq;

namespace FragranceCatalog.Infrastructure.Suppliers.Magna;

/// <summary>Reads sitemap XML (sitemaps.org protocol) without doing any HTTP, so it can be tested with saved files.</summary>
public sealed class SitemapReader
{
    private const string TypeParameterName = "type";
    private static readonly XNamespace _sitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";

    /// <summary>Product sitemap pages listed in a sitemap index, in the order the index lists them.</summary>
    /// <param name="sitemapIndexXml">Content of the sitemap index.</param>
    /// <param name="productSitemapType">Value of the <c>type</c> query parameter that marks product sitemaps.</param>
    public IReadOnlyList<string> ReadProductSitemapUrls(string sitemapIndexXml, string productSitemapType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productSitemapType);

        return ReadLocations(sitemapIndexXml, "sitemap")
            .Where(url => HasTypeParameter(url, productSitemapType))
            .ToList();
    }

    /// <summary>Page URLs listed in a sitemap page, without duplicates.</summary>
    /// <param name="urlSetXml">Content of a sitemap page.</param>
    public IReadOnlyList<string> ReadPageUrls(string urlSetXml) =>
        ReadLocations(urlSetXml, "url").Distinct(StringComparer.Ordinal).ToList();

    private static IEnumerable<string> ReadLocations(string xml, string entryElementName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xml);

        return XDocument.Parse(xml)
            .Descendants(_sitemapNamespace + entryElementName)
            .Select(entry => entry.Element(_sitemapNamespace + "loc")?.Value.Trim())
            .OfType<string>()
            .Where(location => location.Length > 0);
    }

    private static bool HasTypeParameter(string url, string expectedType)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(parameter => parameter.Split('=', 2))
            .Any(pair => pair.Length == 2
                && pair[0].Equals(TypeParameterName, StringComparison.OrdinalIgnoreCase)
                && Uri.UnescapeDataString(pair[1]).Equals(expectedType, StringComparison.OrdinalIgnoreCase));
    }
}
