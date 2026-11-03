using FragranceCatalog.Domain.Products;
using FragranceCatalog.Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FragranceCatalog.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    private const int SupplierProductIdMaxLength = 100;
    private const int SourceUrlMaxLength = 1000;
    private const int NameMaxLength = 300;

    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Id).ValueGeneratedNever();
        builder.Property(product => product.SupplierProductId).HasMaxLength(SupplierProductIdMaxLength);
        builder.Property(product => product.SourceUrl).HasMaxLength(SourceUrlMaxLength);
        builder.Property(product => product.Name).HasMaxLength(NameMaxLength);
        builder.Property(product => product.Slug).IsStoredAsText();
        builder.Property(product => product.Gender).IsStoredAsText();
        builder.Ignore(product => product.CurrentSupplierPrice);

        builder.HasIndex(product => product.Slug).IsUnique();
        builder.HasIndex(product => new { product.SupplierId, product.SupplierProductId }).IsUnique();

        builder.HasOne<Supplier>().WithMany().HasForeignKey(product => product.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Brand>().WithMany().HasForeignKey(product => product.BrandId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(product => product.Images).WithOne().HasForeignKey(image => image.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(product => product.Images).HasField("_images");

        builder.HasMany(product => product.SupplierPrices).WithOne().HasForeignKey(price => price.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(product => product.SupplierPrices).HasField("_supplierPrices");
    }
}
