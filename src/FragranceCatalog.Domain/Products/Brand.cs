namespace FragranceCatalog.Domain.Products;

/// <summary>Fragrance house shown above the product name and used to group products and brand pricing rules.</summary>
public sealed class Brand
{
    private Brand(Guid id, string name, Slug slug)
    {
        Id = id;
        Name = name;
        Slug = slug;
    }

    /// <summary>Identifier of the brand.</summary>
    public Guid Id { get; private set; }

    /// <summary>Brand name as shown to customers.</summary>
    public string Name { get; private set; }

    /// <summary>Unique public URL identifier of the brand.</summary>
    public Slug Slug { get; private set; }

    /// <summary>Creates a brand; the caller guarantees the slug is not already taken.</summary>
    /// <param name="name">Brand name as shown to customers.</param>
    /// <param name="slug">Unique slug, usually built from the name.</param>
    public static Brand Create(string name, Slug slug)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(slug);

        return new Brand(Guid.CreateVersion7(), name.Trim(), slug);
    }
}
