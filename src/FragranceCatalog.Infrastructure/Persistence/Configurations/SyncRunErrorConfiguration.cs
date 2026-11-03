using FragranceCatalog.Domain.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FragranceCatalog.Infrastructure.Persistence.Configurations;

internal sealed class SyncRunErrorConfiguration : IEntityTypeConfiguration<SyncRunError>
{
    private const int UrlMaxLength = 1000;

    public void Configure(EntityTypeBuilder<SyncRunError> builder)
    {
        builder.ToTable("sync_run_errors");
        builder.HasKey(error => error.Id);
        builder.Property(error => error.Id).ValueGeneratedNever();
        builder.Property(error => error.Url).HasMaxLength(UrlMaxLength);
    }
}
