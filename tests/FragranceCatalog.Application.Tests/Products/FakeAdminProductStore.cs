using FragranceCatalog.Application.Products;
using FragranceCatalog.Domain.Products;

namespace FragranceCatalog.Application.Tests.Products;

internal sealed class FakeAdminProductStore : IAdminProductStore
{
    public List<AdminProductRecord> Records { get; } = [];

    public List<Product> Products { get; } = [];

    public AdminProductFilter? ReceivedFilter { get; private set; }

    public int SaveCount { get; private set; }

    public Task<IReadOnlyList<AdminProductRecord>> ListAsync(AdminProductFilter filter, CancellationToken cancellationToken)
    {
        ReceivedFilter = filter;
        return Task.FromResult<IReadOnlyList<AdminProductRecord>>(Records);
    }

    public Task<Product?> FindForUpdateAsync(Guid productId, CancellationToken cancellationToken) =>
        Task.FromResult(Products.SingleOrDefault(product => product.Id == productId));

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
