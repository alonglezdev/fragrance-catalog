using FragranceCatalog.Domain.Products;

namespace FragranceCatalog.Domain.Tests.Products;

public sealed class SlugTests
{
    [Theory]
    [InlineData("Hawas Private Dulce Extrait de Parfum 100ml Unisex", "hawas-private-dulce-extrait-de-parfum-100ml-unisex")]
    [InlineData("Señorita Café Crème", "senorita-cafe-creme")]
    [InlineData("  Lattafa -- Khamrah (Set)!  ", "lattafa-khamrah-set")]
    [InlineData("already-a-slug", "already-a-slug")]
    public void FromText_FreeText_ProducesLowercaseHyphenatedAscii(string text, string expected)
    {
        var slug = Slug.FromText(text);

        slug.Value.ShouldBe(expected);
    }

    [Fact]
    public void FromText_OnlySymbols_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => Slug.FromText("¡¿?!"));
    }

    [Fact]
    public void FromText_SameText_ProducesEqualSlugs()
    {
        var first = Slug.FromText("Rasasi");

        var second = Slug.FromText("RASASI");

        second.ShouldBe(first);
    }
}
