using FragranceCatalog.Application.Alerts;
using FragranceCatalog.Application.Pricing;
using FragranceCatalog.Application.Products;
using FragranceCatalog.Application.Settings;
using FragranceCatalog.Domain.Sync;
using FragranceCatalog.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FragranceCatalog.Infrastructure.Tests;

public sealed class AdministrationServiceCollectionExtensionsTests
{
    [Theory]
    [InlineData(typeof(GetPricingConfiguration))]
    [InlineData(typeof(UpdatePricingSettings))]
    [InlineData(typeof(ReplacePriceRanges))]
    [InlineData(typeof(CreatePricingRule))]
    [InlineData(typeof(UpdatePricingRule))]
    [InlineData(typeof(SetPricingRuleActive))]
    [InlineData(typeof(PreviewPricingChange))]
    [InlineData(typeof(ListAdminProducts))]
    [InlineData(typeof(SetProductVisibility))]
    [InlineData(typeof(SetProductFeatured))]
    [InlineData(typeof(GetPricingAlerts))]
    [InlineData(typeof(GetSyncSettings))]
    [InlineData(typeof(UpdateSyncSettings))]
    [InlineData(typeof(GetStoreSettings))]
    [InlineData(typeof(UpdateStoreSettings))]
    public void AddAdministration_WithPersistenceAndScheduler_ResolvesUseCase(Type useCaseType)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{PersistenceServiceCollectionExtensions.ConnectionStringName}"] = "Host=localhost",
            })
            .Build();
        var services = new ServiceCollection()
            .AddPersistence(configuration)
            .AddAdministration()
            .AddSingleton<ISyncScheduler, NoOpSyncScheduler>();
        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        var useCase = scope.ServiceProvider.GetRequiredService(useCaseType);

        useCase.ShouldNotBeNull();
    }

    [Fact]
    public void AddAdministration_WithinOneScope_ReaderAndStoreShareOneUnitOfWork()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{PersistenceServiceCollectionExtensions.ConnectionStringName}"] = "Host=localhost",
            })
            .Build();
        var services = new ServiceCollection().AddPersistence(configuration).AddAdministration();
        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        var reader = scope.ServiceProvider.GetRequiredService<IPricingReader>();
        var store = scope.ServiceProvider.GetRequiredService<IPricingStore>();

        reader.ShouldBeSameAs(store);
    }

    private sealed class NoOpSyncScheduler : ISyncScheduler
    {
        public Task ApplyScheduleAsync(SyncSettings settings, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
