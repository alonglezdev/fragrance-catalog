using FragranceCatalog.Domain.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FragranceCatalog.Infrastructure.Persistence.Configurations;

internal sealed class PricingSettingsConfiguration : IEntityTypeConfiguration<PricingSettings>
{
    private const int PercentPrecision = 5;
    private const int PercentScale = 2;

    public void Configure(EntityTypeBuilder<PricingSettings> builder)
    {
        builder.ToTable("pricing_settings", table =>
            table.HasCheckConstraint("ck_pricing_settings_single_row", $"id = {PricingSettings.SingletonId}"));
        builder.HasKey(settings => settings.Id);
        builder.Property(settings => settings.Id).ValueGeneratedNever();
        builder.Property(settings => settings.ActiveBaseStrategy).IsStoredAsText();
        builder.Property(settings => settings.BasePercent).HasPrecision(PercentPrecision, PercentScale);
        builder.Property(settings => settings.RoundingMode).IsStoredAsText();
        builder.Property(settings => settings.MinProfit).IsMoney();

        builder.HasData(PricingSettings.CreateDefault());
    }
}
