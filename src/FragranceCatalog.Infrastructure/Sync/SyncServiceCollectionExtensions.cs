using FragranceCatalog.Application.Sync;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FragranceCatalog.Infrastructure.Sync;

/// <summary>Registers the supplier sync.</summary>
public static class SyncServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="SyncSupplierCatalog"/> and its store as scoped: each sync runs in its own scope,
    /// so it uses one supplier source from start to end and never shares it with another sync.
    /// </summary>
    /// <param name="services">Application services.</param>
    public static IServiceCollection AddCatalogSync(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ICatalogSyncStore, CatalogSyncStore>();
        services.AddScoped<SyncSupplierCatalog>();
        return services;
    }
}
