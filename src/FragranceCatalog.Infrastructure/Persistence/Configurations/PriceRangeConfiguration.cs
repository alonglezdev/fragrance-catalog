using FragranceCatalog.Domain.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FragranceCatalog.Infrastructure.Persistence.Configurations;

internal sealed class PriceRangeConfiguration : IEntityTypeConfiguration<PriceRange>
{
    public void Configure(EntityTypeBuilder<PriceRange> builder)
    {
        builder.ToTable("price_ranges");
        builder.HasKey(range => range.Id);
        builder.Property(range => range.Id).ValueGeneratedNever();
        builder.Property(range => range.MinCost).IsMoney();
        builder.Property(range => range.MaxCost).IsMoney();
        builder.Property(range => range.Action).IsStoredAsText();
        builder.Property(range => range.Value).IsMoney();
        builder.Ignore(range => range.IsOpenEnded);

        builder.HasData(
            new
            {
                Id = SeedData.FirstInitialPriceRangeId,
                MinCost = 0m,
                MaxCost = (decimal?)1000m,
                Action = PriceAction.AddAmount,
                Value = 200m,
            },
            new
            {
                Id = SeedData.SecondInitialPriceRangeId,
                MinCost = 1000m,
                MaxCost = (decimal?)null,
                Action = PriceAction.AddAmount,
                Value = 300m,
            });
    }
}
