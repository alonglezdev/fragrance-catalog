using FragranceCatalog.Domain.Products;

namespace FragranceCatalog.Domain.Tests.Products;

public sealed class ProductTests
{
    private const string ImageUrl = "https://cdn11.bigcommerce.com/s-2vt02okold/images/stencil/640w/products/123/456/a.jpg";
    private const string OtherImageUrl = "https://cdn11.bigcommerce.com/s-2vt02okold/images/stencil/640w/products/123/457/b.jpg";

    private static readonly DateTimeOffset _seenAt = new(2026, 11, 2, 10, 0, 0, TimeSpan.Zero);

    private readonly Guid _brandId = Guid.NewGuid();

    [Fact]
    public void Create_ValidDetails_StartsVisibleNotFeaturedAndNotDiscontinued()
    {
        var product = CreateProduct();

        product.IsHidden.ShouldBeFalse();
        product.IsFeatured.ShouldBeFalse();
        product.IsDiscontinued.ShouldBeFalse();
        product.CreatedAt.ShouldBe(_seenAt);
        product.LastSeenAt.ShouldBe(_seenAt);
    }

    [Fact]
    public void Create_InitialPriceWithOffer_RecordsItAsCurrentPrice()
    {
        var product = CreateProduct(regularPrice: 3269m, salePrice: 2949m);

        product.SupplierPrices.Count.ShouldBe(1);
        product.CurrentSupplierPrice.RegularPrice.ShouldBe(3269m);
        product.CurrentSupplierPrice.SalePrice.ShouldBe(2949m);
        product.CurrentSupplierPrice.Cost.ShouldBe(2949m);
        product.CurrentSupplierPrice.CapturedAt.ShouldBe(_seenAt);
    }

    [Fact]
    public void Create_InitialPriceWithoutOffer_UsesRegularPriceAsCost()
    {
        var product = CreateProduct(regularPrice: 3269m, salePrice: null);

        product.CurrentSupplierPrice.Cost.ShouldBe(3269m);
    }

    [Fact]
    public void Create_ZeroRegularPrice_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => CreateProduct(regularPrice: 0m));
    }

    [Fact]
    public void Create_ZeroSalePrice_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => CreateProduct(salePrice: 0m));
    }

    [Fact]
    public void Create_BlankName_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => CreateProduct(Details() with { Name = " " }));
    }

    [Fact]
    public void Create_RelativeSourceUrl_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => CreateProduct(Details() with { SourceUrl = "/hawas-private" }));
    }

    [Fact]
    public void Create_ZeroSize_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => CreateProduct(Details() with { SizeMl = 0 }));
    }

    [Fact]
    public void UpdateFromSupplier_HiddenAndFeaturedProduct_KeepsAdministratorFlags()
    {
        var product = CreateProduct();
        product.Hide();
        product.Feature();

        product.UpdateFromSupplier(Details() with { Name = "Hawas Private Dulce 50ml Unisex", IsAvailable = false }, _seenAt.AddDays(1));

        product.Name.ShouldBe("Hawas Private Dulce 50ml Unisex");
        product.IsAvailable.ShouldBeFalse();
        product.LastSeenAt.ShouldBe(_seenAt.AddDays(1));
        product.IsHidden.ShouldBeTrue();
        product.IsFeatured.ShouldBeTrue();
    }

    [Fact]
    public void UpdateFromSupplier_RenamedProduct_KeepsSlug()
    {
        var product = CreateProduct();

        product.UpdateFromSupplier(Details() with { Name = "Another Name" }, _seenAt);

        product.Slug.ShouldBe(Slug.FromText("hawas-private-dulce"));
    }

    [Fact]
    public void RecordSupplierPrice_SameAmountsAsCurrent_DoesNotAddEntry()
    {
        var product = CreateProduct(regularPrice: 3269m, salePrice: 2949m);

        var wasRecorded = product.RecordSupplierPrice(3269m, 2949m, _seenAt.AddDays(1));

        wasRecorded.ShouldBeFalse();
        product.SupplierPrices.Count.ShouldBe(1);
        product.CurrentSupplierPrice.CapturedAt.ShouldBe(_seenAt);
    }

    [Fact]
    public void RecordSupplierPrice_OfferEnded_AddsEntryWithRegularPriceAsCost()
    {
        var product = CreateProduct(regularPrice: 3269m, salePrice: 2949m);

        var wasRecorded = product.RecordSupplierPrice(3269m, null, _seenAt.AddDays(1));

        wasRecorded.ShouldBeTrue();
        product.SupplierPrices.Count.ShouldBe(2);
        product.CurrentSupplierPrice.Cost.ShouldBe(3269m);
    }

    [Fact]
    public void RecordSupplierPrice_RegularPriceRose_BecomesCurrentPrice()
    {
        var product = CreateProduct(regularPrice: 3269m, salePrice: null);

        product.RecordSupplierPrice(3499m, null, _seenAt.AddDays(1));

        product.CurrentSupplierPrice.RegularPrice.ShouldBe(3499m);
    }

    [Fact]
    public void RecordSupplierPrice_ZeroRegularPrice_ThrowsAndKeepsCurrentPrice()
    {
        var product = CreateProduct(regularPrice: 3269m, salePrice: null);

        Should.Throw<ArgumentOutOfRangeException>(() => product.RecordSupplierPrice(0m, null, _seenAt.AddDays(1)));

        product.SupplierPrices.Count.ShouldBe(1);
        product.CurrentSupplierPrice.RegularPrice.ShouldBe(3269m);
    }

    [Fact]
    public void ReplaceImages_DuplicateUrls_KeepsFirstOccurrenceInSupplierOrder()
    {
        var product = CreateProduct();

        product.ReplaceImages([ImageUrl, OtherImageUrl, ImageUrl]);

        product.Images.Select(image => image.Url).ShouldBe([ImageUrl, OtherImageUrl]);
        product.Images.Select(image => image.SortOrder).ShouldBe([0, 1]);
    }

    [Fact]
    public void ReplaceImages_CalledAgain_DropsPreviousImages()
    {
        var product = CreateProduct();
        product.ReplaceImages([ImageUrl, OtherImageUrl]);

        product.ReplaceImages([OtherImageUrl]);

        product.Images.Select(image => image.Url).ShouldBe([OtherImageUrl]);
    }

    [Fact]
    public void ValidateSupplierData_ValidData_DoesNotThrow()
    {
        var details = new SupplierProductDetails("https://magnaperfumes.com/a", "Asad", Guid.CreateVersion7(), Gender.Men, 100, string.Empty, true);

        Should.NotThrow(() => Product.ValidateSupplierData(details, 1000m, 900m, [ImageUrl]));
    }

    [Theory]
    [InlineData("https://magnaperfumes.com/a", " ", 1000, null, ImageUrl)]
    [InlineData("not-a-url", "Asad", 1000, null, ImageUrl)]
    [InlineData("https://magnaperfumes.com/a", "Asad", 0, null, ImageUrl)]
    [InlineData("https://magnaperfumes.com/a", "Asad", 1000, 0, ImageUrl)]
    [InlineData("https://magnaperfumes.com/a", "Asad", 1000, null, "/images/a.jpg")]
    public void ValidateSupplierData_InvalidData_ThrowsArgumentException(string sourceUrl, string name, int regularPrice, int? salePrice, string imageUrl)
    {
        var details = new SupplierProductDetails(sourceUrl, name, Guid.CreateVersion7(), Gender.Men, 100, string.Empty, true);

        Should.Throw<ArgumentException>(() => Product.ValidateSupplierData(details, regularPrice, salePrice, [imageUrl]));
    }

    [Fact]
    public void ReplaceImages_SameUrlsInSameOrder_KeepsExistingImages()
    {
        var product = CreateProduct();
        product.ReplaceImages([ImageUrl, OtherImageUrl]);
        var existingImages = product.Images.ToList();

        product.ReplaceImages([ImageUrl, OtherImageUrl]);

        product.Images.ShouldBe(existingImages);
    }

    [Fact]
    public void ReplaceImages_SameUrlsInNewOrder_FollowsSupplierOrder()
    {
        var product = CreateProduct();
        product.ReplaceImages([ImageUrl, OtherImageUrl]);

        product.ReplaceImages([OtherImageUrl, ImageUrl]);

        product.Images.Select(image => image.Url).ShouldBe([OtherImageUrl, ImageUrl]);
    }

    [Fact]
    public void ReplaceImages_RelativeUrl_ThrowsAndKeepsPreviousImages()
    {
        var product = CreateProduct();
        product.ReplaceImages([ImageUrl]);

        Should.Throw<ArgumentException>(() => product.ReplaceImages(["/images/a.jpg"]));

        product.Images.Select(image => image.Url).ShouldBe([ImageUrl]);
    }

    [Fact]
    public void MarkAsDiscontinued_ActiveProduct_IsDiscontinued()
    {
        var product = CreateProduct();

        product.MarkAsDiscontinued();

        product.IsDiscontinued.ShouldBeTrue();
    }

    [Fact]
    public void Reactivate_DiscontinuedProduct_IsNoLongerDiscontinued()
    {
        var product = CreateProduct();
        product.MarkAsDiscontinued();

        product.Reactivate();

        product.IsDiscontinued.ShouldBeFalse();
    }

    [Fact]
    public void Show_HiddenProduct_IsNoLongerHidden()
    {
        var product = CreateProduct();
        product.Hide();

        product.Show();

        product.IsHidden.ShouldBeFalse();
    }

    [Fact]
    public void Unfeature_FeaturedProduct_IsNoLongerFeatured()
    {
        var product = CreateProduct();
        product.Feature();

        product.Unfeature();

        product.IsFeatured.ShouldBeFalse();
    }

    private Product CreateProduct(SupplierProductDetails? details = null, decimal regularPrice = 3269m, decimal? salePrice = 2949m) =>
        Product.Create(Guid.NewGuid(), "123", Slug.FromText("Hawas Private Dulce"), details ?? Details(), regularPrice, salePrice, _seenAt);

    private SupplierProductDetails Details() => new(
        "https://magnaperfumes.com/hawas-private-dulce",
        "Hawas Private Dulce Extrait de Parfum 100ml Unisex",
        _brandId,
        Gender.Unisex,
        100,
        "Fragancia oriental.",
        IsAvailable: true);
}
