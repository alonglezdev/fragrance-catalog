using FragranceCatalog.Application.Alerts;
using FragranceCatalog.Application.Pricing;
using FragranceCatalog.Application.Products;
using FragranceCatalog.Application.Settings;
using FragranceCatalog.Infrastructure.Alerts;
using FragranceCatalog.Infrastructure.Pricing;
using FragranceCatalog.Infrastructure.Products;
using FragranceCatalog.Infrastructure.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FragranceCatalog.Infrastructure;

/// <summary>Registers the administration use cases and the stores behind them.</summary>
public static class AdministrationServiceCollectionExtensions
{
    /// <summary>
    /// Registers everything as scoped, one unit of work per request. <see cref="ISyncScheduler"/> is not registered here:
    /// it belongs to the job host, which must provide it for <see cref="UpdateSyncSettings"/> to resolve.
    /// </summary>
    /// <param name="services">Application services.</param>
    public static IServiceCollection AddAdministration(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<PricingStore>();
        services.AddScoped<IPricingStore>(provider => provider.GetRequiredService<PricingStore>());
        services.AddScoped<IPricingReader>(provider => provider.GetRequiredService<PricingStore>());
        services.AddScoped<IAdminProductStore, AdminProductStore>();
        services.AddScoped<IPricingAlertStore, PricingAlertStore>();
        services.AddScoped<ISettingsStore, SettingsStore>();
        return services.AddAdministrationUseCases();
    }

    private static IServiceCollection AddAdministrationUseCases(this IServiceCollection services) => services
        .AddScoped<GetPricingConfiguration>()
        .AddScoped<UpdatePricingSettings>()
        .AddScoped<ReplacePriceRanges>()
        .AddScoped<CreatePricingRule>()
        .AddScoped<UpdatePricingRule>()
        .AddScoped<SetPricingRuleActive>()
        .AddScoped<PreviewPricingChange>()
        .AddScoped<ListAdminProducts>()
        .AddScoped<SetProductVisibility>()
        .AddScoped<SetProductFeatured>()
        .AddScoped<GetPricingAlerts>()
        .AddScoped<GetSyncSettings>()
        .AddScoped<UpdateSyncSettings>()
        .AddScoped<GetStoreSettings>()
        .AddScoped<UpdateStoreSettings>();
}
