using FragranceCatalog.Application.Sync;
using FragranceCatalog.Domain.Sync;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

namespace FragranceCatalog.Infrastructure.Suppliers.Magna;

/// <summary>Registers the Magna Perfumes catalog reader as the <see cref="ISupplierCatalogSource"/>.</summary>
public static class MagnaServiceCollectionExtensions
{
    private const int CircuitBreakerSamplingFactor = 2;

    /// <summary>
    /// Binds <see cref="MagnaOptions"/> and registers a typed HTTP client with the standard resilience pipeline.
    /// The pipeline's own timeouts are set to the longest timeout the admin may configure, so the sync's
    /// <see cref="SupplierRequestPacing.RequestTimeout"/> is the one that actually applies.
    /// </summary>
    /// <param name="services">Application services.</param>
    /// <param name="configuration">Configuration holding the "Suppliers:Magna" section.</param>
    public static IServiceCollection AddMagnaCatalogSource(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MagnaOptions>()
            .Bind(configuration.GetSection(MagnaOptions.SectionName))
            .Validate(HasRequiredValues, $"The '{MagnaOptions.SectionName}' configuration is incomplete.");
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<SitemapReader>();
        services.AddSingleton<MagnaProductPageParser>();
        services.AddHttpClient<ISupplierCatalogSource, MagnaCatalogSource>(ConfigureClient)
            .AddStandardResilienceHandler(ConfigureResilience);
        return services;
    }

    private static bool HasRequiredValues(MagnaOptions options) =>
        Uri.IsWellFormedUriString(options.SitemapUrl, UriKind.Absolute)
        && new[]
        {
            options.ProductSitemapType,
            options.UserAgent,
            options.ImageWidth,
            options.JsonLdSelector,
            options.GalleryImageSelector,
            options.CatalystPayloadMarker,
            options.GenderFieldName,
        }.All(value => !string.IsNullOrWhiteSpace(value));

    private static void ConfigureClient(IServiceProvider serviceProvider, HttpClient client)
    {
        var options = serviceProvider.GetRequiredService<IOptions<MagnaOptions>>().Value;
        client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
        client.Timeout = Timeout.InfiniteTimeSpan;
    }

    private static void ConfigureResilience(HttpStandardResilienceOptions resilience)
    {
        var longestAllowedTimeout = TimeSpan.FromSeconds(SyncSettings.MaxRequestTimeoutSeconds);
        resilience.AttemptTimeout.Timeout = longestAllowedTimeout;
        resilience.TotalRequestTimeout.Timeout = longestAllowedTimeout * (resilience.Retry.MaxRetryAttempts + 1);
        resilience.CircuitBreaker.SamplingDuration = longestAllowedTimeout * CircuitBreakerSamplingFactor;
    }
}
