using Microsoft.EntityFrameworkCore;

namespace FlightManager.Data.Models;

/// <summary>
/// Represents a paginated list of items with metadata about the pagination.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
public class PaginatedList<T> : List<T>
{
    /// <summary>
    /// Gets the current page index (1-based).
    /// </summary>
    public int PageIndex { get; private set; }

    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages { get; private set; }

    /// <summary>
    /// Gets the number of items per page.
    /// </summary>
    public int PageSize { get; private set; }

    /// <summary>
    /// Gets the total number of items across all pages.
    /// </summary>
    public int TotalCount { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PaginatedList{T}"/> class.
    /// </summary>
    /// <param name="items">The items for the current page.</param>
    /// <param name="count">The total number of items.</param>
    /// <param name="pageIndex">The current page index (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
        TotalCount = count;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);

        this.AddRange(items);
    }

    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => PageIndex > 1;

    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNextPage => PageIndex < TotalPages;

    /// <summary>
    /// Creates a paginated list asynchronously from an IQueryable source.
    /// </summary>
    /// <param name="source">The queryable data source.</param>
    /// <param name="pageIndex">The page index to retrieve (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A task that represents the asynchronous operation and contains the paginated list.</returns>
    public static async Task<PaginatedList<T>> CreateAsync(
        IQueryable<T> source,
        int pageIndex,
        int pageSize)
    {
        var count = await source.CountAsync();
        var items = await source.Skip(
            (pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return new PaginatedList<T>(items, count, pageIndex, pageSize);
    }

    /// <summary>
    /// Creates a paginated list from an IEnumerable source with a known total count.
    /// </summary>
    /// <param name="source">The enumerable data source.</param>
    /// <param name="pageIndex">The page index to retrieve (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="totalCount">The total number of items across all pages.</param>
    /// <returns>The paginated list.</returns>
    public static PaginatedList<T> CreateFromEnumerable(
        IEnumerable<T> source, int pageIndex, int pageSize, int totalCount)
    {
        var items = source.Skip((pageIndex - 1) * pageSize)
                         .Take(pageSize)
                         .ToList();
        return new PaginatedList<T>(items, totalCount, pageIndex, pageSize);
    }
}