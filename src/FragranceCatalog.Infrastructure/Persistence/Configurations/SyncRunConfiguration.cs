using FragranceCatalog.Domain.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FragranceCatalog.Infrastructure.Persistence.Configurations;

internal sealed class SyncRunConfiguration : IEntityTypeConfiguration<SyncRun>
{
    public void Configure(EntityTypeBuilder<SyncRun> builder)
    {
        builder.ToTable("sync_runs");
        builder.HasKey(run => run.Id);
        builder.Property(run => run.Id).ValueGeneratedNever();
        builder.Property(run => run.Trigger).IsStoredAsText();
        builder.Property(run => run.Status).IsStoredAsText();
        builder.Ignore(run => run.IsFinished);

        builder.HasIndex(run => run.StartedAt);

        builder.HasMany(run => run.Errors).WithOne().HasForeignKey(error => error.SyncRunId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(run => run.Errors).HasField("_errors");
    }
}
