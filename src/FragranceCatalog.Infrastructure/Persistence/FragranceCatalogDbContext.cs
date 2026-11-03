using FragranceCatalog.Domain.Pricing;
using FragranceCatalog.Domain.Products;
using FragranceCatalog.Domain.Store;
using FragranceCatalog.Domain.Suppliers;
using FragranceCatalog.Domain.Sync;
using Microsoft.EntityFrameworkCore;

namespace FragranceCatalog.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL unit of work for the catalog. Table and column names are snake_case; images, supplier prices
/// and sync errors are saved through their parent entity.
/// </summary>
/// <param name="options">Provider and connection configured by <see cref="PersistenceServiceCollectionExtensions.AddPersistence"/>.</param>
public sealed class FragranceCatalogDbContext(DbContextOptions<FragranceCatalogDbContext> options) : DbContext(options)
{
    /// <summary>Suppliers whose catalogs are imported.</summary>
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    /// <summary>Fragrance houses.</summary>
    public DbSet<Brand> Brands => Set<Brand>();

    /// <summary>Imported products, with their images and supplier price history.</summary>
    public DbSet<Product> Products => Set<Product>();

    /// <summary>The single row of store-wide pricing settings.</summary>
    public DbSet<PricingSettings> PricingSettings => Set<PricingSettings>();

    /// <summary>Base price range table.</summary>
    public DbSet<PriceRange> PriceRanges => Set<PriceRange>();

    /// <summary>Brand and product pricing exceptions.</summary>
    public DbSet<PricingRule> PricingRules => Set<PricingRule>();

    /// <summary>The single row of sync settings.</summary>
    public DbSet<SyncSettings> SyncSettings => Set<SyncSettings>();

    /// <summary>The single row of store identity and WhatsApp settings.</summary>
    public DbSet<StoreSettings> StoreSettings => Set<StoreSettings>();

    /// <summary>History of catalog syncs, with their errors.</summary>
    public DbSet<SyncRun> SyncRuns => Set<SyncRun>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FragranceCatalogDbContext).Assembly);
        SnakeCaseNaming.Apply(modelBuilder);
    }
}
