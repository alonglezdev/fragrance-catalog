namespace FragranceCatalog.Domain.Products;

/// <summary>Audience a fragrance is marketed to, inferred from the supplier's product name.</summary>
public enum Gender
{
    /// <summary>The name does not say who the fragrance is for.</summary>
    Unspecified,

    /// <summary>Marketed for women ("Dama").</summary>
    Women,

    /// <summary>Marketed for men ("Caballero").</summary>
    Men,

    /// <summary>Marketed for everyone ("Unisex").</summary>
    Unisex,
}
