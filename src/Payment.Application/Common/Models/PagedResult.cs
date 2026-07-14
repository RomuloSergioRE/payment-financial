namespace Payment.Application.Common.Models;

// Generic paginated result wrapper that holds a page of items
// along with pagination metadata.
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages)
{
    // Factory method that creates a PagedResult, computing total pages from the given count.
    public static PagedResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResult<T>(items, page, pageSize, totalCount, totalPages);
    }
}
