using FragranceCatalog.Application.Pricing;
using FragranceCatalog.Application.Products;
using FragranceCatalog.Application.Tests.Pricing;
using FragranceCatalog.Application.Tests.Sync;
using FragranceCatalog.Domain.Pricing;
using FragranceCatalog.Domain.Products;

namespace FragranceCatalog.Application.Tests.Products;

public sealed class AdminProductUseCaseTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 2, 10, 0, 0, TimeSpan.Zero);

    private readonly FakeAdminProductStore _store = new();
    private readonly FakePricingStore _pricing = new();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ListAdminProducts_ProductWithBaseRange_ReturnsCostPriceProfitAndRule()
    {
        var record = Record(775m);
        _store.Records.Add(record);

        var products = await ListAsync(new AdminProductFilter());

        var product = products.ShouldHaveSingleItem();
        product.Cost.ShouldBe(775m);
        product.SalePrice.ShouldBe(975m);
        product.Profit.ShouldBe(200m);
        product.AppliedRuleType.ShouldBe(AppliedRuleType.BaseRange);
        product.AppliedRuleId.ShouldBe(_pricing.Ranges[0].Id);
        product.IsLowProfit.ShouldBeFalse();
    }

    [Fact]
    public async Task ListAdminProducts_ProductRule_ReportsThatRule()
    {
        var record = Record(775m);
        var rule = PricingRule.ForProduct(record.Id, PriceAction.FixedPrice, 800m, null, null, _now);
        _pricing.Rules.Add(rule);
        _store.Records.Add(record);

        var products = await ListAsync(new AdminProductFilter());

        var product = products.ShouldHaveSingleItem();
        product.AppliedRuleType.ShouldBe(AppliedRuleType.ProductRule);
        product.AppliedRuleId.ShouldBe(rule.Id);
        product.IsLowProfit.ShouldBeTrue();
    }

    [Fact]
    public async Task ListAdminProducts_LowProfitOnly_FiltersAfterPricing()
    {
        var lowProfit = Record(775m);
        _pricing.Rules.Add(PricingRule.ForProduct(lowProfit.Id, PriceAction.FixedPrice, 800m, null, null, _now));
        _store.Records.AddRange([lowProfit, Record(775m)]);

        var products = await ListAsync(new AdminProductFilter(IsLowProfitOnly: true));

        products.ShouldHaveSingleItem().Id.ShouldBe(lowProfit.Id);
    }

    [Fact]
    public async Task ListAdminProducts_AnyFilter_PassesItToTheStore()
    {
        var filter = new AdminProductFilter("asad", Guid.CreateVersion7(), IncludeDiscontinued: true);

        await ListAsync(filter);

        _store.ReceivedFilter.ShouldBe(filter);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetProductVisibility_ExistingProduct_ChangesAndSavesIt(bool isHidden)
    {
        var product = NewProduct();
        _store.Products.Add(product);
        if (!isHidden)
        {
            product.Hide();
        }

        var result = await new SetProductVisibility(_store).ExecuteAsync(product.Id, isHidden, Token);

        result.IsSuccess.ShouldBeTrue();
        product.IsHidden.ShouldBe(isHidden);
        _store.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task SetProductVisibility_ProductDoesNotExist_ReturnsProductNotFound()
    {
        var result = await new SetProductVisibility(_store).ExecuteAsync(Guid.CreateVersion7(), true, Token);

        result.Errors.ShouldHaveSingleItem().Code.ShouldBe(PricingErrors.ProductNotFound);
        _store.SaveCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetProductFeatured_ExistingProduct_ChangesAndSavesIt(bool isFeatured)
    {
        var product = NewProduct();
        _store.Products.Add(product);
        if (!isFeatured)
        {
            product.Feature();
        }

        var result = await new SetProductFeatured(_store).ExecuteAsync(product.Id, isFeatured, Token);

        result.IsSuccess.ShouldBeTrue();
        product.IsFeatured.ShouldBe(isFeatured);
        _store.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task SetProductFeatured_ProductDoesNotExist_ReturnsProductNotFound()
    {
        var result = await new SetProductFeatured(_store).ExecuteAsync(Guid.CreateVersion7(), true, Token);

        result.Errors.ShouldHaveSingleItem().Code.ShouldBe(PricingErrors.ProductNotFound);
    }

    private static AdminProductRecord Record(decimal cost) =>
        new(Guid.CreateVersion7(), "Asad", Guid.CreateVersion7(), "Lattafa", cost, true, false, false, false);

    private static Product NewProduct()
    {
        var details = new SupplierProductDetails("https://magnaperfumes.com/a/", "Asad", Guid.CreateVersion7(), Gender.Men, 100, string.Empty, true);
        return Product.Create(Guid.CreateVersion7(), "1", Slug.FromText("asad"), details, 775m, null, _now);
    }

    private Task<IReadOnlyList<AdminProductDto>> ListAsync(AdminProductFilter filter) =>
        new ListAdminProducts(_store, _pricing, new FixedTimeProvider(_now)).ExecuteAsync(filter, Token);
}
