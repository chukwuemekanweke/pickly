using System.Linq.Expressions;

namespace BLVK.Pickly.Logistics.Domain.Common.Persistence;

public abstract class Specification<TEntity> : ISpecification<TEntity>
{
    private readonly List<Expression<Func<TEntity, object>>> _includes = [];

    public Expression<Func<TEntity, bool>>? Criteria { get; private set; }
    public IReadOnlyCollection<Expression<Func<TEntity, object>>> Includes => _includes;
    public Expression<Func<TEntity, object>>? OrderBy { get; private set; }
    public Expression<Func<TEntity, object>>? ThenBy { get; private set; }
    public Expression<Func<TEntity, object>>? OrderByDescending { get; private set; }
    public Expression<Func<TEntity, object>>? ThenByDescending { get; private set; }
    public int? Skip { get; private set; }
    public int? Take { get; private set; }
    public bool AsNoTracking { get; private set; } = true;

    protected void Where(Expression<Func<TEntity, bool>> criteria) => Criteria = criteria;

    protected void AddInclude(Expression<Func<TEntity, object>> includeExpression) =>
        _includes.Add(includeExpression);

    protected void ApplyOrderBy(Expression<Func<TEntity, object>> orderByExpression) =>
        OrderBy = orderByExpression;

    protected void ApplyThenBy(Expression<Func<TEntity, object>> thenByExpression) =>
        ThenBy = thenByExpression;

    protected void ApplyOrderByDescending(Expression<Func<TEntity, object>> orderByDescendingExpression) =>
        OrderByDescending = orderByDescendingExpression;

    protected void ApplyThenByDescending(Expression<Func<TEntity, object>> thenByDescendingExpression) =>
        ThenByDescending = thenByDescendingExpression;

    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
    }

    protected void EnableTracking() => AsNoTracking = false;
}
