using FragranceCatalog.Infrastructure.Persistence;
using FragranceCatalog.Infrastructure.Settings;
using FragranceCatalog.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FragranceCatalog.Infrastructure.Tests.Settings;

public sealed class SettingsStoreTests(PostgresDatabaseFixture database)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task SaveChangesAsync_ChangedRows_StoresThem()
    {
        await using var context = database.CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync(Token);
        var store = new SettingsStore(context);
        var syncSettings = await store.GetSyncSettingsAsync(Token);
        var storeSettings = await store.GetStoreSettingsAsync(Token);
        syncSettings.Update(false, new TimeOnly(23, 45), "America/Tijuana", 50, 250, 120);
        storeSettings.Update("Mi Tienda", "523312345678", "Quiero {product}");

        await store.SaveChangesAsync(Token);

        context.ChangeTracker.Clear();
        (await context.SyncSettings.AsNoTracking().SingleAsync(Token)).TimeZoneId.ShouldBe("America/Tijuana");
        (await context.StoreSettings.AsNoTracking().SingleAsync(Token)).StoreName.ShouldBe("Mi Tienda");
    }
}
