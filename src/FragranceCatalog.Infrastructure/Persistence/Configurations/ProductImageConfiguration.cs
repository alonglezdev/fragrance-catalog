using FragranceCatalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FragranceCatalog.Infrastructure.Persistence.Configurations;

internal sealed class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    private const int UrlMaxLength = 1000;

    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("product_images");
        builder.HasKey(image => image.Id);
        builder.Property(image => image.Id).ValueGeneratedNever();
        builder.Property(image => image.Url).HasMaxLength(UrlMaxLength);

        builder.HasIndex(image => new { image.ProductId, image.SortOrder });
    }
}
