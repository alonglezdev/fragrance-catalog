using FragranceCatalog.Application.Sync;
using FragranceCatalog.Domain.Products;
using FragranceCatalog.Infrastructure.Suppliers.Magna;

namespace FragranceCatalog.Infrastructure.Tests.Suppliers.Magna;

public sealed class MagnaProductPageParserTests
{
    private const string ArabicBrandUrl = "https://magnaperfumes.com/lattafa-asad-elixir-edp-100ml-caballero/";
    private const string ArabicBrandSchemaImageAt1280 =
        "https://cdn11.bigcommerce.com/s-2vt02okold/images/stencil/1280w/products/4909/10172/HLATAE__81824.1786548540.jpg?compression=lossy";

    private readonly MagnaProductPageParser _parser = MagnaFixtures.CreateParser();

    [Fact]
    public void Parse_ArabicBrandPage_ReadsProductData()
    {
        var html = MagnaFixtures.Read(MagnaFixtures.ArabicBrandPage);

        var product = _parser.Parse(html, ArabicBrandUrl);

        product.SupplierProductId.ShouldBe("4909");
        product.SourceUrl.ShouldBe(ArabicBrandUrl);
        product.Name.ShouldBe("Lattafa Asad Elixir EDP 100ml Caballero");
        product.Brand.ShouldBe("Lattafa");
        product.RegularPrice.ShouldBe(725m);
        product.SalePrice.ShouldBeNull();
        product.Gender.ShouldBe(Gender.Men);
        product.SizeMl.ShouldBe(100);
        product.IsAvailable.ShouldBeTrue();
        product.Description.ShouldStartWith("Es una fragancia masculina");
    }

    [Fact]
    public void Parse_ArabicBrandPage_ReadsEveryGalleryPhotoOnceAtConfiguredWidthMainFirst()
    {
        var html = MagnaFixtures.Read(MagnaFixtures.ArabicBrandPage);

        var product = _parser.Parse(html, ArabicBrandUrl);

        product.Images.Count.ShouldBe(3);
        product.Images[0].ShouldBe(ArabicBrandSchemaImageAt1280);
        product.Images.ShouldAllBe(url => url.StartsWith("https://cdn11.bigcommerce.com/s-2vt02okold/images/stencil/1280w/products/4909/"));
        product.Images.ShouldBeUnique();
    }

    [Fact]
    public void Parse_RegularPricePage_DecodesNameAndTrimsDescription()
    {
        var html = MagnaFixtures.Read(MagnaFixtures.RegularPricePage);

        var product = _parser.Parse(html, "https://magnaperfumes.com/dolce-gabbana-light-blue-summer-vibes-edt-dama/");

        product.SupplierProductId.ShouldBe("4201");
        product.Name.ShouldBe("Dolce & Gabbana Light Blue Summer Vibes EDT 100ml Dama");
        product.Brand.ShouldBe("Dolce & Gabbana");
        product.RegularPrice.ShouldBe(1049m);
        product.SalePrice.ShouldBeNull();
        product.Gender.ShouldBe(Gender.Women);
        product.Description.ShouldStartWith("Es una fragancia femenina");
        product.Description.ShouldBe(product.Description.Trim());
    }

    [Fact]
    public void Parse_GiftSetWithSeveralSizesInName_TakesFirstSize()
    {
        var html = MagnaFixtures.Read(MagnaFixtures.GiftSetPage);

        var product = _parser.Parse(html, "https://magnaperfumes.com/million-gold-intense-estuche/");

        product.Name.ShouldStartWith("Million Gold Intense Estuche 3 Piezas Caballero");
        product.Brand.ShouldBe("Rabanne");
        product.SizeMl.ShouldBe(100);
        product.Gender.ShouldBe(Gender.Men);
        product.RegularPrice.ShouldBe(1689m);
    }

    [Fact]
    public void Parse_TrioSetWithoutGenderInName_UsesSupplierGenderField()
    {
        var html = MagnaFixtures.Read(MagnaFixtures.TrioSetPage);

        var product = _parser.Parse(html, "https://magnaperfumes.com/trio-hawas-private/");

        product.Name.ShouldBe("Trio Hawas Private (Dulce, Luceo y Terra 100ml c/u)");
        product.Brand.ShouldBe("Rasasi");
        product.Gender.ShouldBe(Gender.Unisex);
        product.SizeMl.ShouldBe(100);
        product.RegularPrice.ShouldBe(2949m);
    }

    [Fact]
    public void Parse_OutOfStockPage_IsNotAvailable()
    {
        var html = MagnaFixtures.Read(MagnaFixtures.OutOfStockPage);

        var product = _parser.Parse(html, "https://magnaperfumes.com/dolce-gabbana-light-blue-summer-vibes-edt-dama/");

        product.IsAvailable.ShouldBeFalse();
        product.RegularPrice.ShouldBe(1049m);
    }

    [Fact]
    public void Parse_CatalystSalePriceMatchingSchemaPrice_ReturnsRegularAndSalePrices()
    {
        var html = MagnaFixtures.Read(MagnaFixtures.ArabicBrandPage)
            .Replace("\"@type\":\"PriceSpecification\",\"price\":725", "\"@type\":\"PriceSpecification\",\"price\":650", StringComparison.Ordinal)
            .Replace("\\\"salePrice\\\":null", "\\\"salePrice\\\":{\\\"value\\\":650,\\\"currencyCode\\\":\\\"MXN\\\"}", StringComparison.Ordinal);

        var product = _parser.Parse(html, ArabicBrandUrl);

        product.RegularPrice.ShouldBe(725m);
        product.SalePrice.ShouldBe(650m);
    }

    [Fact]
    public void Parse_CatalystSaleWithSchemaShowingRegularPrice_ReturnsRegularAndSalePrices()
    {
        var html = MagnaFixtures.Read(MagnaFixtures.ArabicBrandPage)
            .Replace("\\\"salePrice\\\":null", "\\\"salePrice\\\":{\\\"value\\\":650,\\\"currencyCode\\\":\\\"MXN\\\"}", StringComparison.Ordinal);

        var product = _parser.Parse(html, ArabicBrandUrl);

        product.RegularPrice.ShouldBe(725m);
        product.SalePrice.ShouldBe(650m);
    }

    [Fact]
    public void Parse_CatalystPriceDiffersFromSchemaPrice_ThrowsSupplierCatalogException()
    {
        var html = MagnaFixtures.Read(MagnaFixtures.ArabicBrandPage)
            .Replace("\"@type\":\"PriceSpecification\",\"price\":725", "\"@type\":\"PriceSpecification\",\"price\":699", StringComparison.Ordinal);

        var exception = Should.Throw<SupplierCatalogException>(() => _parser.Parse(html, ArabicBrandUrl));

        exception.Message.ShouldContain(ArabicBrandUrl);
    }

    [Fact]
    public void Parse_PriceOfZero_ThrowsSupplierCatalogException()
    {
        var html = MagnaFixtures.Read(MagnaFixtures.ArabicBrandPage)
            .Replace("\"@type\":\"PriceSpecification\",\"price\":725", "\"@type\":\"PriceSpecification\",\"price\":0", StringComparison.Ordinal)
            .Replace("self.__next_f.push(", "self.__disabled_push(", StringComparison.Ordinal);

        var exception = Should.Throw<SupplierCatalogException>(() => _parser.Parse(html, ArabicBrandUrl));

        exception.Message.ShouldContain(ArabicBrandUrl);
    }

    [Fact]
    public void Parse_PriceThatIsNotANumber_ThrowsSupplierCatalogException()
    {
        var html = MagnaFixtures.Read(MagnaFixtures.ArabicBrandPage)
            .Replace("\"@type\":\"PriceSpecification\",\"price\":725", "\"@type\":\"PriceSpecification\",\"price\":\"Consultar\"", StringComparison.Ordinal);

        var exception = Should.Throw<SupplierCatalogException>(() => _parser.Parse(html, ArabicBrandUrl));

        exception.InnerException.ShouldBeOfType<FormatException>();
    }

    [Fact]
    public void Parse_WithoutCatalystData_UsesSchemaPriceAsRegularAndIdFromImageUrl()
    {
        var html = MagnaFixtures.Read(MagnaFixtures.ArabicBrandPage)
            .Replace("self.__next_f.push(", "self.__disabled_push(", StringComparison.Ordinal);

        var product = _parser.Parse(html, ArabicBrandUrl);

        product.SupplierProductId.ShouldBe("4909");
        product.RegularPrice.ShouldBe(725m);
        product.SalePrice.ShouldBeNull();
        product.Gender.ShouldBe(Gender.Men);
    }

    [Fact]
    public void Parse_GallerySelectorFindsNothing_UsesSchemaImage()
    {
        var html = MagnaFixtures.Read(MagnaFixtures.ArabicBrandPage)
            .Replace("product-gallery-image-background", "renamed-gallery", StringComparison.Ordinal);

        var product = _parser.Parse(html, ArabicBrandUrl);

        product.Images.ShouldBe([ArabicBrandSchemaImageAt1280]);
    }

    [Fact]
    public void Parse_PageWithoutSchemaProduct_ThrowsSupplierCatalogException()
    {
        var html = MagnaFixtures.Read(MagnaFixtures.ArabicBrandPage)
            .Replace("application/ld+json", "text/plain", StringComparison.Ordinal);

        Should.Throw<SupplierCatalogException>(() => _parser.Parse(html, ArabicBrandUrl));
    }
}
