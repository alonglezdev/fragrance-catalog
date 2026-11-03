using FragranceCatalog.Domain.Common;

namespace FragranceCatalog.Domain.Suppliers;

/// <summary>
/// A store whose catalog is imported by the sync. Its name and URL are internal data and must never reach the public catalog.
/// </summary>
public sealed class Supplier
{
    private Supplier(Guid id, string name, string baseUrl)
    {
        Id = id;
        Name = name;
        BaseUrl = baseUrl;
    }

    /// <summary>Identifier of the supplier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Display name used only in the admin panel.</summary>
    public string Name { get; private set; }

    /// <summary>Root URL of the supplier storefront, used to resolve its catalog.</summary>
    public string BaseUrl { get; private set; }

    /// <summary>Whether the sync should import this supplier's catalog.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Registers a new supplier, active from the start so the next sync imports it.</summary>
    /// <param name="name">Display name for the admin panel.</param>
    /// <param name="baseUrl">Absolute root URL of the storefront.</param>
    public static Supplier Create(string name, string baseUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Guard.ThrowIfNotAbsoluteUrl(baseUrl);

        return new Supplier(Guid.CreateVersion7(), name.Trim(), baseUrl) { IsActive = true };
    }

    /// <summary>Includes the supplier in future syncs.</summary>
    public void Activate() => IsActive = true;

    /// <summary>Excludes the supplier from future syncs without touching its existing products.</summary>
    public void Deactivate() => IsActive = false;
}
