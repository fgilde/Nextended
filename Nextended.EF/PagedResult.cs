using System;
using System.Collections.Generic;

namespace Nextended.EF;

/// <summary>
/// Result of a paged query: the slice of items plus the total count and paging metadata.
/// </summary>
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int PageIndex { get; init; }
    public int PageSize { get; init; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNext => PageIndex + 1 < TotalPages;
    public bool HasPrevious => PageIndex > 0;

    public static PagedResult<T> Empty(int pageIndex = 0, int pageSize = 0) =>
        new() { Items = Array.Empty<T>(), TotalCount = 0, PageIndex = pageIndex, PageSize = pageSize };
}
