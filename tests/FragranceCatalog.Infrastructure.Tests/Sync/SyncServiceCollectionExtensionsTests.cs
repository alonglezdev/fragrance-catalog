using FragranceCatalog.Application.Sync;
using FragranceCatalog.Infrastructure.Persistence;
using FragranceCatalog.Infrastructure.Suppliers.Magna;
using FragranceCatalog.Infrastructure.Sync;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FragranceCatalog.Infrastructure.Tests.Sync;

public sealed class SyncServiceCollectionExtensionsTests
{
    [Fact]
    public void AddCatalogSync_WithPersistenceAndMagna_ResolvesUseCaseInsideScope()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{PersistenceServiceCollectionExtensions.ConnectionStringName}"] = "Host=localhost",
            })
            .Build();
        var services = new ServiceCollection()
            .AddPersistence(configuration)
            .AddMagnaCatalogSource(configuration)
            .AddCatalogSync();
        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        var useCase = scope.ServiceProvider.GetRequiredService<SyncSupplierCatalog>();

        useCase.ShouldNotBeNull();
    }
}
