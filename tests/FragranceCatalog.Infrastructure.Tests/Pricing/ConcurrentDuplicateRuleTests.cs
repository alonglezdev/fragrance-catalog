using FragranceCatalog.Application.Common;
using FragranceCatalog.Application.Pricing;
using FragranceCatalog.Domain.Pricing;
using FragranceCatalog.Domain.Products;
using FragranceCatalog.Infrastructure.Pricing;
using FragranceCatalog.Infrastructure.Tests.Persistence;
using Microsoft.EntityFrameworkCore;
using static FragranceCatalog.Infrastructure.Tests.Persistence.CatalogTestData;

namespace FragranceCatalog.Infrastructure.Tests.Pricing;

public sealed class ConcurrentDuplicateRuleTests(PostgresDatabaseFixture database)
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task CreatePricingRule_OtherRequestWinsAfterTheCheck_ReturnsDuplicateActiveRuleConflict()
    {
        var brand = await SaveBrandWithActiveRuleAsync();
        await using var context = database.CreateDbContext();
        var store = new StoreThatMissesConcurrentRule(new PricingStore(context), brand.Id);
        var useCase = new CreatePricingRule(store, TimeProvider.System);

        var result = await useCase.ExecuteAsync(new NewPricingRule(PricingRuleScope.Brand, brand.Id, PriceAction.AddAmount, 2m, null, null), Token);

        ShouldBeDuplicateConflict(result, brand.Id);
        (await CountRulesAsync(brand.Id)).ShouldBe(1);
    }

    [Fact]
    public async Task SetPricingRuleActive_OtherRequestWinsAfterTheCheck_ReturnsDuplicateActiveRuleConflict()
    {
        var brand = await SaveBrandWithActiveRuleAsync();
        var inactiveRule = PricingRule.ForBrand(brand.Id, PriceAction.AddAmount, 3m, null, null, CapturedAt);
        inactiveRule.Deactivate();
        await using (var setup = database.CreateDbContext())
        {
            setup.PricingRules.Add(inactiveRule);
            await setup.SaveChangesAsync(Token);
        }

        await using var context = database.CreateDbContext();
        var store = new StoreThatMissesConcurrentRule(new PricingStore(context), brand.Id);
        var useCase = new SetPricingRuleActive(store);

        var result = await useCase.ExecuteAsync(inactiveRule.Id, true, Token);

        ShouldBeDuplicateConflict(result, brand.Id);
        await using var verification = database.CreateDbContext();
        (await verification.PricingRules.SingleAsync(rule => rule.Id == inactiveRule.Id, Token)).IsActive.ShouldBeFalse();
    }

    private static void ShouldBeDuplicateConflict(Result<PricingRuleDto> result, Guid targetId)
    {
        var error = result.Errors.ShouldHaveSingleItem();
        error.Code.ShouldBe(PricingErrors.DuplicateActiveRule);
        error.Kind.ShouldBe(ErrorKind.Conflict);
        error.Value.ShouldBe(targetId.ToString());
    }

    private async Task<Brand> SaveBrandWithActiveRuleAsync()
    {
        var brand = NewBrand();
        await using var context = database.CreateDbContext();
        context.Brands.Add(brand);
        context.PricingRules.Add(PricingRule.ForBrand(brand.Id, PriceAction.AddAmount, 1m, null, null, CapturedAt));
        await context.SaveChangesAsync(Token);
        return brand;
    }

    private async Task<int> CountRulesAsync(Guid brandId)
    {
        await using var context = database.CreateDbContext();
        return await context.PricingRules.CountAsync(rule => rule.BrandId == brandId, Token);
    }

    private sealed class StoreThatMissesConcurrentRule(PricingStore store, Guid targetId) : IPricingStore
    {
        public Task<bool> HasOtherActiveRuleAsync(PricingRuleScope scope, Guid candidateTargetId, Guid? excludedRuleId, CancellationToken cancellationToken) =>
            candidateTargetId == targetId
                ? Task.FromResult(false)
                : store.HasOtherActiveRuleAsync(scope, candidateTargetId, excludedRuleId, cancellationToken);

        public Task<PricingSettings> GetSettingsForUpdateAsync(CancellationToken cancellationToken) => store.GetSettingsForUpdateAsync(cancellationToken);

        public Task ReplaceRangesAsync(IReadOnlyList<PriceRange> ranges, CancellationToken cancellationToken) => store.ReplaceRangesAsync(ranges, cancellationToken);

        public Task<PricingRule?> FindRuleForUpdateAsync(Guid ruleId, CancellationToken cancellationToken) => store.FindRuleForUpdateAsync(ruleId, cancellationToken);

        public Task<bool> TryAddRuleAsync(PricingRule rule, CancellationToken cancellationToken) => store.TryAddRuleAsync(rule, cancellationToken);

        public Task<bool> TrySaveRuleChangesAsync(CancellationToken cancellationToken) => store.TrySaveRuleChangesAsync(cancellationToken);

        public Task<bool> TargetExistsAsync(PricingRuleScope scope, Guid candidateTargetId, CancellationToken cancellationToken) => store.TargetExistsAsync(scope, candidateTargetId, cancellationToken);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => store.SaveChangesAsync(cancellationToken);
    }
}
