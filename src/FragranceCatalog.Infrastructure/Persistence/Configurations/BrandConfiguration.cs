using FragranceCatalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FragranceCatalog.Infrastructure.Persistence.Configurations;

internal sealed class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    private const int NameMaxLength = 200;

    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("brands");
        builder.HasKey(brand => brand.Id);
        builder.Property(brand => brand.Id).ValueGeneratedNever();
        builder.Property(brand => brand.Name).HasMaxLength(NameMaxLength);
        builder.Property(brand => brand.Slug).IsStoredAsText();

        builder.HasIndex(brand => brand.Slug).IsUnique();
    }
}
