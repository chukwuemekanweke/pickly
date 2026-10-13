using System.Linq.Expressions;

namespace BLVK.Pickly.Logistics.Domain.Common.Persistence;

public interface ISpecification<TEntity>
{
    Expression<Func<TEntity, bool>>? Criteria { get; }
    IReadOnlyCollection<Expression<Func<TEntity, object>>> Includes { get; }
    Expression<Func<TEntity, object>>? OrderBy { get; }
    Expression<Func<TEntity, object>>? ThenBy { get; }
    Expression<Func<TEntity, object>>? OrderByDescending { get; }
    Expression<Func<TEntity, object>>? ThenByDescending { get; }
    int? Skip { get; }
    int? Take { get; }
    bool AsNoTracking { get; }
}
