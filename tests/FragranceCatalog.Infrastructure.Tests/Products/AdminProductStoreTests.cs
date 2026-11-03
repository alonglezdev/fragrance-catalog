using FragranceCatalog.Application.Products;
using FragranceCatalog.Domain.Products;
using FragranceCatalog.Infrastructure.Persistence;
using FragranceCatalog.Infrastructure.Products;
using FragranceCatalog.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;
using static FragranceCatalog.Infrastructure.Tests.Persistence.CatalogTestData;

namespace FragranceCatalog.Infrastructure.Tests.Products;

public sealed class AdminProductStoreTests(PostgresDatabaseFixture database)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ListAsync_SearchInDifferentCase_FindsByProductName()
    {
        var marker = Unique("Asad");
        var product = await SaveProductAsync(NewBrand(), $"Lattafa {marker} EDP");

        var records = await ListAsync(new AdminProductFilter(marker.ToUpperInvariant()));

        records.ShouldHaveSingleItem().Id.ShouldBe(product.Id);
    }

    [Fact]
    public async Task ListAsync_SearchByBrandName_FindsItsProducts()
    {
        var brand = NewBrand();
        var product = await SaveProductAsync(brand);

        var records = await ListAsync(new AdminProductFilter(brand.Name));

        records.ShouldHaveSingleItem().Id.ShouldBe(product.Id);
    }

    [Fact]
    public async Task ListAsync_SearchWithWildcardCharacters_TreatsThemAsText()
    {
        var marker = Unique("wild");
        var withPercent = await SaveProductAsync(NewBrand(), $"{marker} 100%_off");
        await SaveProductAsync(NewBrand(), $"{marker} 100 off");

        var records = await ListAsync(new AdminProductFilter($"{marker} 100%_"));

        records.ShouldHaveSingleItem().Id.ShouldBe(withPercent.Id);
    }

    [Fact]
    public async Task ListAsync_BrandFilter_ReturnsCurrentCostAndFlags()
    {
        var brand = NewBrand();
        var product = NewProduct(brand, regularPrice: 1000m, salePrice: 900m);
        product.Hide();
        product.Feature();
        await SaveAsync(brand, product);

        var records = await ListAsync(new AdminProductFilter(BrandId: brand.Id));

        var record = records.ShouldHaveSingleItem();
        record.Cost.ShouldBe(900m);
        record.BrandName.ShouldBe(brand.Name);
        record.IsHidden.ShouldBeTrue();
        record.IsFeatured.ShouldBeTrue();
    }

    [Fact]
    public async Task ListAsync_DiscontinuedProduct_OnlyAppearsWhenRequested()
    {
        var brand = NewBrand();
        var product = NewProduct(brand);
        product.MarkAsDiscontinued();
        await SaveAsync(brand, product);

        var withoutDiscontinued = await ListAsync(new AdminProductFilter(BrandId: brand.Id));
        var withDiscontinued = await ListAsync(new AdminProductFilter(BrandId: brand.Id, IncludeDiscontinued: true));

        withoutDiscontinued.ShouldBeEmpty();
        withDiscontinued.ShouldHaveSingleItem().IsDiscontinued.ShouldBeTrue();
    }

    [Fact]
    public async Task FindForUpdateAsync_ChangeSaved_IsStored()
    {
        var product = await SaveProductAsync(NewBrand());
        await using (var context = database.CreateDbContext())
        {
            var store = new AdminProductStore(context);
            var loaded = await store.FindForUpdateAsync(product.Id, Token);
            loaded!.Hide();
            await store.SaveChangesAsync(Token);
        }

        await using var verification = database.CreateDbContext();
        (await verification.Products.SingleAsync(candidate => candidate.Id == product.Id, Token)).IsHidden.ShouldBeTrue();
    }

    private async Task<IReadOnlyList<AdminProductRecord>> ListAsync(AdminProductFilter filter)
    {
        await using var context = database.CreateDbContext();
        return await new AdminProductStore(context).ListAsync(filter, Token);
    }

    private async Task<Product> SaveProductAsync(Brand brand, string? name = null)
    {
        var product = NewProduct(brand, name);
        await SaveAsync(brand, product);
        return product;
    }

    private async Task SaveAsync(Brand brand, Product product)
    {
        await using var context = database.CreateDbContext();
        context.Brands.Add(brand);
        context.Products.Add(product);
        await context.SaveChangesAsync(Token);
    }
}
