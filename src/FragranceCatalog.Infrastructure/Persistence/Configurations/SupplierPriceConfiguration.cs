using FragranceCatalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FragranceCatalog.Infrastructure.Persistence.Configurations;

internal sealed class SupplierPriceConfiguration : IEntityTypeConfiguration<SupplierPrice>
{
    public void Configure(EntityTypeBuilder<SupplierPrice> builder)
    {
        builder.ToTable("supplier_prices");
        builder.HasKey(price => price.Id);
        builder.Property(price => price.Id).ValueGeneratedNever();
        builder.Property(price => price.RegularPrice).IsMoney();
        builder.Property(price => price.SalePrice).IsMoney();
        builder.Ignore(price => price.Cost);

        builder.HasIndex(price => new { price.ProductId, price.CapturedAt });
    }
}
