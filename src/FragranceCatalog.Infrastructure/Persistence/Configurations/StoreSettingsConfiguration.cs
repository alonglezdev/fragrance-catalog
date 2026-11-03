using FragranceCatalog.Domain.Store;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FragranceCatalog.Infrastructure.Persistence.Configurations;

internal sealed class StoreSettingsConfiguration : IEntityTypeConfiguration<StoreSettings>
{
    public void Configure(EntityTypeBuilder<StoreSettings> builder)
    {
        builder.ToTable("store_settings", table =>
            table.HasCheckConstraint("ck_store_settings_single_row", $"id = {StoreSettings.SingletonId}"));
        builder.HasKey(settings => settings.Id);
        builder.Property(settings => settings.Id).ValueGeneratedNever();
        builder.Property(settings => settings.StoreName).HasMaxLength(StoreSettings.MaxStoreNameLength);
        builder.Property(settings => settings.WhatsAppNumber).HasColumnName("whatsapp_number").HasMaxLength(StoreSettings.MaxWhatsAppNumberDigits);
        builder.Property(settings => settings.WhatsAppMessageTemplate).HasColumnName("whatsapp_message_template").HasMaxLength(StoreSettings.MaxMessageTemplateLength);
        builder.Ignore(settings => settings.HasWhatsAppNumber);

        builder.HasData(StoreSettings.CreateDefault());
    }
}
