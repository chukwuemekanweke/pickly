using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence;

public static class SpecificationEvaluator
{
    public static IQueryable<TEntity> GetQuery<TEntity>(
        IQueryable<TEntity> inputQuery,
        ISpecification<TEntity> specification)
        where TEntity : class
    {
        var query = inputQuery;

        if (specification.Criteria is not null)
        {
            query = query.Where(specification.Criteria);
        }

        query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));

        if (specification.OrderBy is not null)
        {
            var orderedQuery = query.OrderBy(specification.OrderBy);

            if (specification.ThenBy is not null)
            {
                orderedQuery = orderedQuery.ThenBy(specification.ThenBy);
            }

            query = orderedQuery;
        }
        else if (specification.OrderByDescending is not null)
        {
            var orderedQuery = query.OrderByDescending(specification.OrderByDescending);

            if (specification.ThenByDescending is not null)
            {
                orderedQuery = orderedQuery.ThenByDescending(specification.ThenByDescending);
            }

            query = orderedQuery;
        }

        if (specification.Skip.HasValue)
        {
            query = query.Skip(specification.Skip.Value);
        }

        if (specification.Take.HasValue)
        {
            query = query.Take(specification.Take.Value);
        }

        if (specification.AsNoTracking)
        {
            query = query.AsNoTracking();
        }

        return query;
    }
}
