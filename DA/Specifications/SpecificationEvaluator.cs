using Microsoft.EntityFrameworkCore;

namespace DA.Specifications;

public static class SpecificationEvaluator<T> where T : class
{
    public static IQueryable<T> GetQuery(IQueryable<T> query, BaseSpecification<T> spec)
    {
        if (spec.Criteria is not null)
            query = query.Where(spec.Criteria);

        query = spec.Includes.Aggregate(query, (q, i) => q.Include(i));
        query = spec.IncludeStrings.Aggregate(query, (q, i) => q.Include(i));

        if (spec.OrderBy is not null)
            query = query.OrderBy(spec.OrderBy);
        else if (spec.OrderByDesc is not null)
            query = query.OrderByDescending(spec.OrderByDesc);

        if (spec.IsPagingEnabled)
            query = query.Skip(spec.Skip).Take(spec.Take);

        return query;
    }
}
