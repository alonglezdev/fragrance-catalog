using FragranceCatalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FragranceCatalog.Infrastructure.Persistence.Configurations;

internal static class PropertyBuilderExtensions
{
    private const int MoneyPrecision = 12;
    private const int MoneyScale = 2;
    private const int EnumMaxLength = 32;
    private const int SlugMaxLength = 300;

    public static PropertyBuilder<decimal> IsMoney(this PropertyBuilder<decimal> property) =>
        property.HasPrecision(MoneyPrecision, MoneyScale);

    public static PropertyBuilder<decimal?> IsMoney(this PropertyBuilder<decimal?> property) =>
        property.HasPrecision(MoneyPrecision, MoneyScale);

    public static PropertyBuilder<TEnum> IsStoredAsText<TEnum>(this PropertyBuilder<TEnum> property)
        where TEnum : struct, Enum =>
        property.HasConversion<string>().HasMaxLength(EnumMaxLength);

    public static PropertyBuilder<Slug> IsStoredAsText(this PropertyBuilder<Slug> property) =>
        property.HasConversion(slug => slug.Value, value => Slug.FromText(value)).HasMaxLength(SlugMaxLength);
}
