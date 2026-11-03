using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FragranceCatalog.Infrastructure.Persistence;

internal static partial class SnakeCaseNaming
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            RenameColumns(entityType);
            RenameConstraintsAndIndexes(entityType);
        }
    }

    public static string ToSnakeCase(string name) =>
        WordBoundary().Replace(name, "_").ToLowerInvariant();

    private static void RenameColumns(IMutableEntityType entityType)
    {
        var table = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());
        foreach (var property in entityType.GetProperties())
        {
            property.SetColumnName(ToSnakeCase(property.GetColumnName(table)!));
        }
    }

    private static void RenameConstraintsAndIndexes(IMutableEntityType entityType)
    {
        foreach (var key in entityType.GetKeys())
        {
            key.SetName(ToSnakeCase(key.GetName()!));
        }

        foreach (var foreignKey in entityType.GetForeignKeys())
        {
            foreignKey.SetConstraintName(ToSnakeCase(foreignKey.GetConstraintName()!));
        }

        foreach (var index in entityType.GetIndexes())
        {
            index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName()!));
        }
    }

    [GeneratedRegex("(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])")]
    private static partial Regex WordBoundary();
}
