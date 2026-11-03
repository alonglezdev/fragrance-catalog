using FragranceCatalog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace FragranceCatalog.Infrastructure.Tests.Persistence;

public sealed class PostgresDatabaseFixture : IAsyncLifetime
{
    private const string PostgresImage = "postgres:17";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder(PostgresImage).Build();

    public FragranceCatalogDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FragranceCatalogDbContext>();
        options.UseCatalogDatabase(_container.GetConnectionString());
        return new FragranceCatalogDbContext(options.Options);
    }

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
    }

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
