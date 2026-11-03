using FragranceCatalog.Domain.Pricing;
using FragranceCatalog.Domain.Products;
using FragranceCatalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FragranceCatalog.Infrastructure.Tests.Persistence;

public sealed class ProductPersistenceTests(PostgresDatabaseFixture database)
{
    private const string MainImageUrl = "https://cdn11.bigcommerce.com/s-2vt02okold/images/stencil/640w/products/123/456/main.jpg";
    private const string SideImageUrl = "https://cdn11.bigcommerce.com/s-2vt02okold/images/stencil/640w/products/123/457/side.jpg";

    private static readonly DateTimeOffset _seenAt = new(2026, 11, 2, 10, 0, 0, TimeSpan.Zero);

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task SaveChanges_ProductWithImagesAndPrices_ReadsBackEverythingAndCurrentCost()
    {
        var brand = await SaveNewBrandAsync();
        var product = NewProduct(brand.Id);
        product.ReplaceImages([MainImageUrl, SideImageUrl]);
        product.RecordSupplierPrice(3269m, null, _seenAt.AddDays(1));
        await SaveAsync(product);

        var stored = await LoadProductAsync(product.Id);

        stored.Name.ShouldBe(product.Name);
        stored.Slug.ShouldBe(product.Slug);
        stored.Gender.ShouldBe(Gender.Unisex);
        stored.SizeMl.ShouldBe(100);
        stored.Images.Select(image => image.Url).ShouldBe([MainImageUrl, SideImageUrl]);
        stored.SupplierPrices.Count.ShouldBe(2);
        stored.CurrentSupplierPrice.CapturedAt.ShouldBe(_seenAt.AddDays(1));
        stored.CurrentSupplierPrice.Cost.ShouldBe(3269m);
    }

    [Fact]
    public async Task SaveChanges_NewPriceOnLoadedProduct_InsertsPriceRow()
    {
        var brand = await SaveNewBrandAsync();
        var product = NewProduct(brand.Id);
        await SaveAsync(product);

        await using (var context = database.CreateDbContext())
        {
            var loaded = await context.Products.Include(candidate => candidate.SupplierPrices).SingleAsync(candidate => candidate.Id == product.Id, Token);
            loaded.RecordSupplierPrice(3499m, null, _seenAt.AddDays(2));
            await context.SaveChangesAsync(Token);
        }

        var stored = await LoadProductAsync(product.Id);
        stored.SupplierPrices.Count.ShouldBe(2);
        stored.CurrentSupplierPrice.RegularPrice.ShouldBe(3499m);
    }

    [Fact]
    public async Task SaveChanges_ImagesReplacedOnLoadedProduct_DeletesPreviousImages()
    {
        var brand = await SaveNewBrandAsync();
        var product = NewProduct(brand.Id);
        product.ReplaceImages([MainImageUrl, SideImageUrl]);
        await SaveAsync(product);

        await using (var context = database.CreateDbContext())
        {
            var loaded = await context.Products.Include(candidate => candidate.Images).SingleAsync(candidate => candidate.Id == product.Id, Token);
            loaded.ReplaceImages([SideImageUrl]);
            await context.SaveChangesAsync(Token);
        }

        await using var verification = database.CreateDbContext();
        var storedUrls = await verification.Set<ProductImage>().Where(image => image.ProductId == product.Id).Select(image => image.Url).ToListAsync(Token);
        storedUrls.ShouldBe([SideImageUrl]);
    }

    [Fact]
    public async Task SaveChanges_Product_StoresSlugAsPlainText()
    {
        var brand = await SaveNewBrandAsync();
        var product = NewProduct(brand.Id);
        await SaveAsync(product);

        await using var context = database.CreateDbContext();
        var storedSlug = await context.Database
            .SqlQuery<string>($"SELECT slug AS \"Value\" FROM products WHERE id = {product.Id}")
            .SingleAsync(Token);

        storedSlug.ShouldBe(product.Slug.Value);
    }

    [Fact]
    public async Task SaveChanges_DuplicateSupplierProductId_IsRejectedByUniqueIndex()
    {
        var brand = await SaveNewBrandAsync();
        var supplierProductId = Guid.NewGuid().ToString("N");
        await SaveAsync(NewProduct(brand.Id, supplierProductId));

        var exception = await Should.ThrowAsync<DbUpdateException>(() => SaveAsync(NewProduct(brand.Id, supplierProductId)));

        ShouldBeUniqueViolation(exception, "ix_products_supplier_id_supplier_product_id");
    }

    [Fact]
    public async Task SaveChanges_DuplicateProductSlug_IsRejectedByUniqueIndex()
    {
        var brand = await SaveNewBrandAsync();
        var slug = Slug.FromText($"hawas-{Guid.NewGuid():N}");
        await SaveAsync(NewProduct(brand.Id, slug: slug));

        var exception = await Should.ThrowAsync<DbUpdateException>(() => SaveAsync(NewProduct(brand.Id, slug: slug)));

        ShouldBeUniqueViolation(exception, "ix_products_slug");
    }

    [Fact]
    public async Task SaveChanges_SecondActiveRuleForSameBrand_IsRejectedByUniqueIndex()
    {
        var brand = await SaveNewBrandAsync();
        await SaveAsync(PricingRule.ForBrand(brand.Id, PriceAction.AddPercent, 8m, null, null, _seenAt));

        var exception = await Should.ThrowAsync<DbUpdateException>(() => SaveAsync(PricingRule.ForBrand(brand.Id, PriceAction.AddAmount, 100m, null, null, _seenAt)));

        ShouldBeUniqueViolation(exception, "ix_pricing_rules_active_brand_id");
    }

    [Fact]
    public async Task SaveChanges_InactiveRuleNextToActiveRuleForSameBrand_IsAccepted()
    {
        var brand = await SaveNewBrandAsync();
        await SaveAsync(PricingRule.ForBrand(brand.Id, PriceAction.AddPercent, 8m, null, null, _seenAt));
        var inactiveRule = PricingRule.ForBrand(brand.Id, PriceAction.AddAmount, 100m, null, null, _seenAt);
        inactiveRule.Deactivate();

        await SaveAsync(inactiveRule);

        await using var context = database.CreateDbContext();
        var ruleCount = await context.PricingRules.CountAsync(rule => rule.BrandId == brand.Id, Token);
        ruleCount.ShouldBe(2);
    }

    private static void ShouldBeUniqueViolation(DbUpdateException exception, string constraintName)
    {
        var postgresException = exception.InnerException.ShouldBeOfType<PostgresException>();
        postgresException.SqlState.ShouldBe(PostgresErrorCodes.UniqueViolation);
        postgresException.ConstraintName.ShouldBe(constraintName);
    }

    private static Product NewProduct(Guid brandId, string? supplierProductId = null, Slug? slug = null) => Product.Create(
        SeedData.MagnaSupplierId,
        supplierProductId ?? Guid.NewGuid().ToString("N"),
        slug ?? Slug.FromText($"hawas-private-dulce-{Guid.NewGuid():N}"),
        new SupplierProductDetails(
            "https://magnaperfumes.com/hawas-private-dulce",
            "Hawas Private Dulce Extrait de Parfum 100ml Unisex",
            brandId,
            Gender.Unisex,
            100,
            "Fragancia oriental.",
            IsAvailable: true),
        3269m,
        2949m,
        _seenAt);

    private async Task<Brand> SaveNewBrandAsync()
    {
        var brand = Brand.Create("Rasasi", Slug.FromText($"rasasi-{Guid.NewGuid():N}"));
        await SaveAsync(brand);
        return brand;
    }

    private async Task SaveAsync(object entity)
    {
        await using var context = database.CreateDbContext();
        context.Add(entity);
        await context.SaveChangesAsync(Token);
    }

    private async Task<Product> LoadProductAsync(Guid productId)
    {
        await using var context = database.CreateDbContext();
        return await context.Products
            .Include(product => product.Images.OrderBy(image => image.SortOrder))
            .Include(product => product.SupplierPrices)
            .SingleAsync(product => product.Id == productId, Token);
    }
}
