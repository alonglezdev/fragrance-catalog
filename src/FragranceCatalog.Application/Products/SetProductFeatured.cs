using FragranceCatalog.Application.Common;
using FragranceCatalog.Application.Pricing;

namespace FragranceCatalog.Application.Products;

/// <summary>Adds a product to the featured list or removes it. The sync never undoes this.</summary>
/// <param name="store">Product data.</param>
public sealed class SetProductFeatured(IAdminProductStore store)
{
    /// <summary>Changes whether the product is featured.</summary>
    /// <param name="productId">Product to change.</param>
    /// <param name="isFeatured">Whether it should be featured.</param>
    /// <param name="cancellationToken">Stops the operation.</param>
    public async Task<Result> ExecuteAsync(Guid productId, bool isFeatured, CancellationToken cancellationToken)
    {
        var product = await store.FindForUpdateAsync(productId, cancellationToken);
        if (product is null)
        {
            return Result.Failure([Error.NotFound(PricingErrors.ProductNotFound, productId)]);
        }

        if (isFeatured)
        {
            product.Feature();
        }
        else
        {
            product.Unfeature();
        }

        await store.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
