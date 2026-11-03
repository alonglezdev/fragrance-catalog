using FragranceCatalog.Domain.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FragranceCatalog.Infrastructure.Persistence.Configurations;

internal sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    private const int NameMaxLength = 100;
    private const int BaseUrlMaxLength = 500;

    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("suppliers");
        builder.HasKey(supplier => supplier.Id);
        builder.Property(supplier => supplier.Id).ValueGeneratedNever();
        builder.Property(supplier => supplier.Name).HasMaxLength(NameMaxLength);
        builder.Property(supplier => supplier.BaseUrl).HasMaxLength(BaseUrlMaxLength);

        builder.HasData(new
        {
            Id = SeedData.MagnaSupplierId,
            Name = "Magna Perfumes",
            BaseUrl = "https://magnaperfumes.com",
            IsActive = true,
        });
    }
}
