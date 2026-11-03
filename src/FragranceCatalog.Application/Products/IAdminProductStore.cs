using FragranceCatalog.Domain.Products;

namespace FragranceCatalog.Application.Products;

/// <summary>The product data the administrator reads and changes.</summary>
public interface IAdminProductStore
{
    /// <summary>Reads the products matching the search, brand and discontinued filters, by name. Low profit is decided later, after pricing them.</summary>
    /// <param name="filter">The filters.</param>
    /// <param name="cancellationToken">Stops the read.</param>
    Task<IReadOnlyList<AdminProductRecord>> ListAsync(AdminProductFilter filter, CancellationToken cancellationToken);

    /// <summary>Loads a product to change it, or returns null if it does not exist.</summary>
    /// <param name="productId">Identifier of the product.</param>
    /// <param name="cancellationToken">Stops the operation.</param>
    Task<Product?> FindForUpdateAsync(Guid productId, CancellationToken cancellationToken);

    /// <summary>Saves the changes made to the loaded product.</summary>
    /// <param name="cancellationToken">Stops the operation.</param>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
