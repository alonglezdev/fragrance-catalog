using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FragranceCatalog.Infrastructure.Persistence;

/// <summary>Registers the PostgreSQL persistence of the catalog.</summary>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>Name of the connection string that points to the catalog database.</summary>
    public const string ConnectionStringName = "FragranceCatalog";

    private const string MigrationsHistoryTable = "__ef_migrations_history";

    /// <summary>Registers <see cref="FragranceCatalogDbContext"/> against the database in the "FragranceCatalog" connection string.</summary>
    /// <param name="services">Application services.</param>
    /// <param name="configuration">Configuration holding the connection string.</param>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

        services.AddDbContext<FragranceCatalogDbContext>(options => options.UseCatalogDatabase(connectionString));
        return services;
    }

    internal static DbContextOptionsBuilder UseCatalogDatabase(this DbContextOptionsBuilder options, string connectionString) =>
        options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable(MigrationsHistoryTable));
}
