using FragranceCatalog.Domain.Pricing;
using FragranceCatalog.Domain.Products;
using FragranceCatalog.Infrastructure.Pricing;
using FragranceCatalog.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using static FragranceCatalog.Infrastructure.Tests.Persistence.CatalogTestData;

namespace FragranceCatalog.Infrastructure.Tests.Pricing;

public sealed class PricingStoreTests(PostgresDatabaseFixture database)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetProductCostsAsync_OfferAndNewerPrice_ReturnsCurrentCostOfActiveProducts()
    {
        await using var context = database.CreateDbContext();
        var brand = NewBrand();
        var repriced = NewProduct(brand, regularPrice: 1000m);
        repriced.RecordSupplierPrice(1200m, 1100m, CapturedAt.AddDays(1));
        var discontinued = NewProduct(brand);
        discontinued.MarkAsDiscontinued();
        context.Brands.Add(brand);
        context.Products.AddRange(repriced, discontinued);
        await context.SaveChangesAsync(Token);

        var costs = await new PricingStore(context).GetProductCostsAsync(Token);

        var cost = costs.Single(item => item.ProductId == repriced.Id);
        cost.Cost.ShouldBe(1100m);
        cost.BrandName.ShouldBe(brand.Name);
        costs.ShouldNotContain(item => item.ProductId == discontinued.Id);
    }

    [Fact]
    public async Task GetConfigurationAsync_SeededDatabase_ReturnsUntrackedConfiguration()
    {
        await using var context = database.CreateDbContext();

        var configuration = await new PricingStore(context).GetConfigurationAsync(Token);

        configuration.Ranges.ShouldNotBeEmpty();
        context.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [Fact]
    public async Task UpdateAndReplace_InsideTransaction_SaveSettingsAndWholeTable()
    {
        await using var context = database.CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync(Token);
        var store = new PricingStore(context);
        var settings = await store.GetSettingsForUpdateAsync(Token);
        settings.Update(BaseStrategy.Percent, 12m, RoundingMode.EndsInNine, 150m, 20);
        await store.SaveChangesAsync(Token);

        await store.ReplaceRangesAsync([PriceRange.Create(0m, null, PriceAction.AddPercent, 25m)], Token);

        var configuration = await store.GetConfigurationAsync(Token);
        configuration.Settings.BasePercent.ShouldBe(12m);
        configuration.Ranges.ShouldHaveSingleItem().Value.ShouldBe(25m);
    }

    [Fact]
    public async Task HasOtherActiveRuleAsync_ActiveRuleOnBrand_IgnoresOnlyTheExcludedRule()
    {
        var (brand, rule) = await SaveBrandWithRuleAsync();
        await using var context = database.CreateDbContext();
        var store = new PricingStore(context);

        var hasAnyRule = await store.HasOtherActiveRuleAsync(PricingRuleScope.Brand, brand.Id, null, Token);
        var hasOtherRule = await store.HasOtherActiveRuleAsync(PricingRuleScope.Brand, brand.Id, rule.Id, Token);

        hasAnyRule.ShouldBeTrue();
        hasOtherRule.ShouldBeFalse();
    }

    [Fact]
    public async Task HasOtherActiveRuleAsync_InactiveRule_ReturnsFalse()
    {
        var (brand, rule) = await SaveBrandWithRuleAsync();
        await using var context = database.CreateDbContext();
        var store = new PricingStore(context);
        var loadedRule = await store.FindRuleForUpdateAsync(rule.Id, Token);
        loadedRule!.Deactivate();
        await store.SaveChangesAsync(Token);

        var hasRule = await store.HasOtherActiveRuleAsync(PricingRuleScope.Brand, brand.Id, null, Token);

        hasRule.ShouldBeFalse();
    }

    [Fact]
    public async Task TryAddRuleAsync_SecondActiveRuleForBrand_ReturnsFalseAndStoresNothing()
    {
        var (brand, _) = await SaveBrandWithRuleAsync();
        await using var context = database.CreateDbContext();
        var duplicate = PricingRule.ForBrand(brand.Id, PriceAction.AddAmount, 2m, null, null, CapturedAt);

        var isStored = await new PricingStore(context).TryAddRuleAsync(duplicate, Token);

        isStored.ShouldBeFalse();
        context.ChangeTracker.Entries().ShouldBeEmpty();
        (await context.PricingRules.AnyAsync(rule => rule.Id == duplicate.Id, Token)).ShouldBeFalse();
    }

    [Fact]
    public async Task TryAddRuleAsync_SecondActiveRuleForProduct_ReturnsFalse()
    {
        var brand = NewBrand();
        var product = NewProduct(brand);
        await using var context = database.CreateDbContext();
        context.Brands.Add(brand);
        context.Products.Add(product);
        context.PricingRules.Add(PricingRule.ForProduct(product.Id, PriceAction.AddAmount, 1m, null, null, CapturedAt));
        await context.SaveChangesAsync(Token);
        var duplicate = PricingRule.ForProduct(product.Id, PriceAction.AddAmount, 2m, null, null, CapturedAt);

        var isStored = await new PricingStore(context).TryAddRuleAsync(duplicate, Token);

        isStored.ShouldBeFalse();
    }

    [Fact]
    public async Task TryAddRuleAsync_ForeignKeyViolation_PropagatesTheException()
    {
        await using var context = database.CreateDbContext();
        var ruleForMissingBrand = PricingRule.ForBrand(Guid.CreateVersion7(), PriceAction.AddAmount, 2m, null, null, CapturedAt);

        var exception = await Should.ThrowAsync<DbUpdateException>(() => new PricingStore(context).TryAddRuleAsync(ruleForMissingBrand, Token));

        exception.InnerException.ShouldBeOfType<PostgresException>().SqlState.ShouldBe(PostgresErrorCodes.ForeignKeyViolation);
    }

    [Fact]
    public async Task TrySaveRuleChangesAsync_OtherUniqueViolation_PropagatesTheException()
    {
        var (brand, _) = await SaveBrandWithRuleAsync();
        await using var context = database.CreateDbContext();
        context.Brands.Add(Brand.Create(Unique("repeated"), brand.Slug));

        await Should.ThrowAsync<DbUpdateException>(() => new PricingStore(context).TrySaveRuleChangesAsync(Token));
    }

    [Fact]
    public async Task TargetExistsAsync_BrandAndProduct_ChecksTheRightTable()
    {
        var brand = NewBrand();
        var product = NewProduct(brand);
        await using var context = database.CreateDbContext();
        context.Brands.Add(brand);
        context.Products.Add(product);
        await context.SaveChangesAsync(Token);
        var store = new PricingStore(context);

        (await store.TargetExistsAsync(PricingRuleScope.Brand, brand.Id, Token)).ShouldBeTrue();
        (await store.TargetExistsAsync(PricingRuleScope.Product, product.Id, Token)).ShouldBeTrue();
        (await store.TargetExistsAsync(PricingRuleScope.Product, brand.Id, Token)).ShouldBeFalse();
    }

    private async Task<(Brand Brand, PricingRule Rule)> SaveBrandWithRuleAsync()
    {
        var brand = NewBrand();
        var rule = PricingRule.ForBrand(brand.Id, PriceAction.AddAmount, 1m, null, null, CapturedAt);
        await using var context = database.CreateDbContext();
        context.Brands.Add(brand);
        context.PricingRules.Add(rule);
        await context.SaveChangesAsync(Token);
        return (brand, rule);
    }
}
