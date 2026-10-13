using BLVK.Pickly.Logistics.Domain.Common.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence;

public sealed class EfReadRepository<TEntity>(AppReadDbContext dbContext) : IReadRepository<TEntity>
    where TEntity : Entity, IAggregateRoot
{
    public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Set<TEntity>().FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

    public Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default) =>
        SpecificationEvaluator.GetQuery(dbContext.Set<TEntity>(), specification).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<TEntity>> ListAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default) =>
        await SpecificationEvaluator.GetQuery(dbContext.Set<TEntity>(), specification).ToListAsync(cancellationToken);

    public Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default) =>
        SpecificationEvaluator.GetQuery(dbContext.Set<TEntity>(), specification).AnyAsync(cancellationToken);

    public Task<int> CountAsync(ISpecification<TEntity> specification, CancellationToken cancellationToken = default) =>
        SpecificationEvaluator.GetQuery(dbContext.Set<TEntity>(), specification).CountAsync(cancellationToken);
}
