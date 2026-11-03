using FragranceCatalog.Domain.Pricing;
using FragranceCatalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FragranceCatalog.Infrastructure.Persistence.Configurations;

internal sealed class PricingRuleConfiguration : IEntityTypeConfiguration<PricingRule>
{
    internal const string ActiveBrandRuleIndex = "ix_pricing_rules_active_brand_id";
    internal const string ActiveProductRuleIndex = "ix_pricing_rules_active_product_id";

    private const string OnlyActiveRules = "is_active";
    private const string TargetMatchesScope =
        "(scope = 'Brand' AND brand_id IS NOT NULL AND product_id IS NULL) OR " +
        "(scope = 'Product' AND product_id IS NOT NULL AND brand_id IS NULL)";

    public void Configure(EntityTypeBuilder<PricingRule> builder)
    {
        builder.ToTable("pricing_rules", table =>
            table.HasCheckConstraint("ck_pricing_rules_target_matches_scope", TargetMatchesScope));
        builder.HasKey(rule => rule.Id);
        builder.Property(rule => rule.Id).ValueGeneratedNever();
        builder.Property(rule => rule.Scope).IsStoredAsText();
        builder.Property(rule => rule.Action).IsStoredAsText();
        builder.Property(rule => rule.Value).IsMoney();
        builder.Ignore(rule => rule.TargetId);

        builder.HasIndex(rule => rule.BrandId).IsUnique().HasFilter(OnlyActiveRules).HasDatabaseName(ActiveBrandRuleIndex);
        builder.HasIndex(rule => rule.ProductId).IsUnique().HasFilter(OnlyActiveRules).HasDatabaseName(ActiveProductRuleIndex);

        builder.HasOne<Brand>().WithMany().HasForeignKey(rule => rule.BrandId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Product>().WithMany().HasForeignKey(rule => rule.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
