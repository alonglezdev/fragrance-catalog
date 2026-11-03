using FragranceCatalog.Application.Sync;
using FragranceCatalog.Domain.Products;
using static FragranceCatalog.Application.Tests.Sync.SyncTestData;

namespace FragranceCatalog.Application.Tests.Sync;

public sealed class CatalogSyncPlannerTests
{
    private const string InvalidBrand = "لطافة";

    private static readonly Guid _supplierId = Guid.CreateVersion7();

    [Fact]
    public void Create_BrandSpelledDifferently_ReusesExistingBrand()
    {
        var brand = ExistingBrand();
        var snapshot = SnapshotOf(brands: [brand]);

        var plan = CreatePlan(snapshot, SupplierProductOf("1", brand: "LATTAFA"));

        plan.NewBrands.ShouldBeEmpty();
        plan.NewProducts.ShouldHaveSingleItem().BrandId.ShouldBe(brand.Id);
    }

    [Fact]
    public void Create_TwoNewProductsOfNewBrand_CreatesBrandOnce()
    {
        var plan = CreatePlan(SnapshotOf(), SupplierProductOf("1", brand: "Rasasi"), SupplierProductOf("2", brand: "Rasasi"));

        var brand = plan.NewBrands.ShouldHaveSingleItem();
        plan.NewProducts.ShouldAllBe(product => product.BrandId == brand.Id);
    }

    [Fact]
    public void Create_ProductSlugAlreadyTaken_AddsNextFreeSuffix()
    {
        var snapshot = SnapshotOf(takenSlugs: [Slug.FromText("asad-edp"), Slug.FromText("asad-edp-2")]);

        var plan = CreatePlan(snapshot, SupplierProductOf("1", name: "Asad EDP"));

        plan.NewProducts.ShouldHaveSingleItem().Slug.Value.ShouldBe("asad-edp-3");
    }

    [Fact]
    public void Create_TwoNewProductsWithSameName_GivesEachItsOwnSlug()
    {
        var plan = CreatePlan(SnapshotOf(), SupplierProductOf("1", name: "Asad EDP"), SupplierProductOf("2", name: "Asad EDP"));

        plan.NewProducts.Select(product => product.Slug.Value).ShouldBe(["asad-edp", "asad-edp-2"]);
    }

    [Fact]
    public void Create_TwoPagesReportSameProduct_UsesFirstOne()
    {
        var first = SupplierProductOf("1", regularPrice: 1000m);
        var duplicate = first with { RegularPrice = 2000m };

        var plan = CreatePlan(SnapshotOf(), first, duplicate);

        plan.NewProducts.ShouldHaveSingleItem().CurrentSupplierPrice.RegularPrice.ShouldBe(1000m);
        plan.Counts.Created.ShouldBe(1);
    }

    [Theory]
    [InlineData(1000, null, 1000, 900)]
    [InlineData(1000, 900, 1000, null)]
    [InlineData(1000, 900, 1000, 800)]
    [InlineData(1000, null, 1100, null)]
    public void Create_AnyAmountChanged_CountsPriceChange(int oldRegular, int? oldSale, int newRegular, int? newSale)
    {
        var existing = ExistingProductRecord("1", oldRegular, oldSale);

        var plan = CreatePlan(SnapshotOf(products: [existing]), SupplierProductOf("1", newRegular, newSale));

        plan.Counts.PriceChanged.ShouldBe(1);
    }

    [Fact]
    public void Create_AlreadyDiscontinuedProductStillMissing_IsNotCountedAgain()
    {
        var existing = ExistingProductRecord("1") with { IsDiscontinued = true };

        var plan = CreatePlan(SnapshotOf(products: [existing]), SupplierProductOf("2"));

        plan.DiscontinuedProductIds.ShouldBeEmpty();
        plan.Counts.Discontinued.ShouldBe(0);
    }

    [Fact]
    public void Create_ProductMovedToNewUrl_MatchesBySupplierProductIdAndKeepsItActive()
    {
        var existing = ExistingProductRecord("1") with { SourceUrl = "https://magnaperfumes.com/old-url/" };

        var plan = CreatePlan(SnapshotOf(products: [existing]), SupplierProductOf("1"));

        plan.DiscontinuedProductIds.ShouldBeEmpty();
        plan.ProductUpdates.ShouldHaveSingleItem().Details.SourceUrl.ShouldBe(UrlOf("1"));
    }

    [Fact]
    public void Create_UnreadPageOfExistingProduct_LeavesProductOutOfThePlan()
    {
        var existing = ExistingProductRecord("1");

        var plan = CatalogSyncPlanner.Create(SnapshotOf(products: [existing]), [], [existing.SourceUrl], Now);

        plan.DiscontinuedProductIds.ShouldBeEmpty();
        plan.ProductUpdates.ShouldBeEmpty();
    }

    [Fact]
    public void Create_BrandThatProducesNoSlug_RejectsOnlyThatProduct()
    {
        var plan = CreatePlan(SnapshotOf(), SupplierProductOf("1", brand: InvalidBrand), SupplierProductOf("2"));

        var rejected = plan.RejectedProducts.ShouldHaveSingleItem();
        rejected.SourceUrl.ShouldBe(UrlOf("1"));
        rejected.Reason.ShouldContain(InvalidBrand);
        plan.NewProducts.ShouldHaveSingleItem().SupplierProductId.ShouldBe("2");
        plan.Counts.Created.ShouldBe(1);
    }

    [Fact]
    public void Create_ProductRejectedAfterItsBrandWasResolved_LeavesNoNewBrandBehind()
    {
        var plan = CreatePlan(SnapshotOf(), SupplierProductOf("1", brand: "Marca Nueva", name: InvalidBrand));

        plan.RejectedProducts.ShouldHaveSingleItem();
        plan.NewBrands.ShouldBeEmpty();
    }

    [Fact]
    public void Create_RejectedProductThenValidProductOfSameNewBrand_CreatesBrandOnce()
    {
        var invalid = SupplierProductOf("1", brand: "Marca Nueva", regularPrice: 0m);
        var valid = SupplierProductOf("2", brand: "Marca Nueva");

        var plan = CreatePlan(SnapshotOf(), invalid, valid);

        var brand = plan.NewBrands.ShouldHaveSingleItem();
        plan.NewProducts.ShouldHaveSingleItem().BrandId.ShouldBe(brand.Id);
    }

    [Fact]
    public void Create_ExistingProductWithInvalidData_RejectsItWithoutUpdatingOrDiscontinuing()
    {
        var existing = ExistingProductRecord("1", regularPrice: 1000m);
        var invalid = SupplierProductOf("1", regularPrice: 1000m) with { Images = ["/images/relative.jpg"] };

        var plan = CreatePlan(SnapshotOf(products: [existing]), invalid);

        plan.RejectedProducts.ShouldHaveSingleItem().SourceUrl.ShouldBe(UrlOf("1"));
        plan.ProductUpdates.ShouldBeEmpty();
        plan.DiscontinuedProductIds.ShouldBeEmpty();
        plan.Counts.PriceChanged.ShouldBe(0);
    }

    [Fact]
    public void Create_RejectedProductWithFreeSlug_DoesNotReserveIt()
    {
        var invalid = SupplierProductOf("1", name: "Asad EDP", regularPrice: 0m);
        var valid = SupplierProductOf("2", name: "Asad EDP");

        var plan = CreatePlan(SnapshotOf(), invalid, valid);

        plan.NewProducts.ShouldHaveSingleItem().Slug.Value.ShouldBe("asad-edp");
    }

    [Fact]
    public void Create_NewProduct_UsesSeenAtForPriceAndDates()
    {
        var plan = CreatePlan(SnapshotOf(), SupplierProductOf("1"));

        var product = plan.NewProducts.ShouldHaveSingleItem();
        plan.SeenAt.ShouldBe(Now);
        product.LastSeenAt.ShouldBe(Now);
        product.CurrentSupplierPrice.CapturedAt.ShouldBe(Now);
    }

    private static CatalogSyncPlan CreatePlan(CatalogSnapshot snapshot, params SupplierProduct[] supplierProducts) =>
        CatalogSyncPlanner.Create(snapshot, supplierProducts, [], Now);

    private static CatalogSnapshot SnapshotOf(
        IReadOnlyList<ExistingProduct>? products = null,
        IReadOnlyList<Brand>? brands = null,
        IReadOnlyCollection<Slug>? takenSlugs = null) =>
        new(_supplierId, products ?? [], brands ?? [], takenSlugs ?? []);

    private static ExistingProduct ExistingProductRecord(string supplierProductId, decimal regularPrice = 1000m, decimal? salePrice = null) =>
        new(Guid.CreateVersion7(), supplierProductId, UrlOf(supplierProductId), false, regularPrice, salePrice);
}
