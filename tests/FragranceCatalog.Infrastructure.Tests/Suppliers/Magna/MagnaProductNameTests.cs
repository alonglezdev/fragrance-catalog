using FragranceCatalog.Domain.Products;
using FragranceCatalog.Infrastructure.Suppliers.Magna;

namespace FragranceCatalog.Infrastructure.Tests.Suppliers.Magna;

public sealed class MagnaProductNameTests
{
    [Theory]
    [InlineData("Good Girl Very EDP 80 ml Dama", Gender.Women)]
    [InlineData("Armaf Imperiale Agua de Perfume 100 Mujer", Gender.Women)]
    [InlineData("Lattafa Asad Elixir EDP 100ml Caballero", Gender.Men)]
    [InlineData("Lacoste Original EDP 100ml Hombre", Gender.Men)]
    [InlineData("Hawas Private Dulce Extrait de Parfum 100ml Unisex", Gender.Unisex)]
    [InlineData("Dama", Gender.Women)]
    [InlineData("UNISEX", Gender.Unisex)]
    [InlineData("Trio Hawas Private (Dulce, Luceo y Terra 100ml c/u)", Gender.Unspecified)]
    [InlineData("Damasco Oud EDP 100ml", Gender.Unspecified)]
    [InlineData("", Gender.Unspecified)]
    [InlineData(null, Gender.Unspecified)]
    public void FindGender_Text_MapsSupplierWordToGender(string? text, Gender expected)
    {
        var gender = MagnaProductName.FindGender(text);

        gender.ShouldBe(expected);
    }

    [Theory]
    [InlineData("Lattafa Asad Elixir EDP 100ml Caballero", 100)]
    [InlineData("Good Girl Very EDP 80 ml Dama", 80)]
    [InlineData("Moonlight Body 236ML Dama", 236)]
    [InlineData("Million Gold Intense Estuche 3 Piezas Caballero EDP 100ml + Desodorante 150ml + Miniatura 10ml", 100)]
    [InlineData("Trio Hawas Private (Dulce, Luceo y Terra 100ml c/u)", 100)]
    [InlineData("Moonlight Estuche 3pzs", null)]
    [InlineData("The Icon Set 2 Piezas Dama", null)]
    [InlineData("Armaf Imperiale Agua de Perfume 100 Dama", null)]
    [InlineData("Perfume 100mlx", null)]
    [InlineData("Muestra 0ml", null)]
    public void FindSizeMl_Name_ReturnsFirstMillilitersOrNull(string name, int? expected)
    {
        var sizeMl = MagnaProductName.FindSizeMl(name);

        sizeMl.ShouldBe(expected);
    }
}
