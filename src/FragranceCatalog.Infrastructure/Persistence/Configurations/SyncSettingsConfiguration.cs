using FragranceCatalog.Domain.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FragranceCatalog.Infrastructure.Persistence.Configurations;

internal sealed class SyncSettingsConfiguration : IEntityTypeConfiguration<SyncSettings>
{
    private const int TimeZoneIdMaxLength = 64;

    public void Configure(EntityTypeBuilder<SyncSettings> builder)
    {
        builder.ToTable("sync_settings", table =>
            table.HasCheckConstraint("ck_sync_settings_single_row", $"id = {SyncSettings.SingletonId}"));
        builder.HasKey(settings => settings.Id);
        builder.Property(settings => settings.Id).ValueGeneratedNever();
        builder.Property(settings => settings.TimeZoneId).HasMaxLength(TimeZoneIdMaxLength);

        builder.HasData(SyncSettings.CreateDefault());
    }
}
