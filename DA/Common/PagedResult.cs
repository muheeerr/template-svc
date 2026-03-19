namespace DA.Common;

public class PaginationData
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalRecords { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
}

public class PagedResult<TResult>
{
    public IEnumerable<TResult> Items { get; set; } = Enumerable.Empty<TResult>();
    public PaginationData PaginationData { get; set; } = new PaginationData();
}
