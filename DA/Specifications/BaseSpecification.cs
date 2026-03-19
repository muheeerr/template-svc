using System.Linq.Expressions;

namespace DA.Specifications;

public abstract class BaseSpecification<T>
{
    public Expression<Func<T, bool>>? Criteria { get; protected set; }
    public List<Expression<Func<T, object>>> Includes { get; } = [];
    public List<string> IncludeStrings { get; } = [];
    public Expression<Func<T, object>>? OrderBy { get; protected set; }
    public Expression<Func<T, object>>? OrderByDesc { get; protected set; }
    public int Take { get; protected set; }
    public int Skip { get; protected set; }
    public bool IsPagingEnabled { get; protected set; }

    protected void AddInclude(Expression<Func<T, object>> include)
        => Includes.Add(include);

    protected void AddInclude(string includeString)
        => IncludeStrings.Add(includeString);

    protected void ApplyPaging(int pageIndex, int pageSize)
    {
        const int MaxPageSize = 100;
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        Skip = (pageIndex - 1) * pageSize;
        Take = pageSize;
        IsPagingEnabled = true;
    }

    protected void ApplyOrderBy(Expression<Func<T, object>> orderBy)
        => OrderBy = orderBy;

    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDesc)
        => OrderByDesc = orderByDesc;
}
