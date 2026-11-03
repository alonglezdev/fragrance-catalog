using FragranceCatalog.Application.Common;
using FragranceCatalog.Application.Pricing;

namespace FragranceCatalog.Application.Products;

/// <summary>Hides a product from the public catalog or shows it again. The sync never undoes this.</summary>
/// <param name="store">Product data.</param>
public sealed class SetProductVisibility(IAdminProductStore store)
{
    /// <summary>Changes whether the product is hidden.</summary>
    /// <param name="productId">Product to change.</param>
    /// <param name="isHidden">Whether it should be hidden.</param>
    /// <param name="cancellationToken">Stops the operation.</param>
    public async Task<Result> ExecuteAsync(Guid productId, bool isHidden, CancellationToken cancellationToken)
    {
        var product = await store.FindForUpdateAsync(productId, cancellationToken);
        if (product is null)
        {
            return Result.Failure([Error.NotFound(PricingErrors.ProductNotFound, productId)]);
        }

        if (isHidden)
        {
            product.Hide();
        }
        else
        {
            product.Show();
        }

        await store.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
