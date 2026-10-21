namespace SaveBite.Backend.Models.Common;

// Return a page of items and the cursor needed to load the next page.
public sealed class CursorPagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public string? NextCursor { get; init; }
    public bool HasMore { get; init; }
    public int PageSize { get; init; }
    // Package items with the next cursor and page metadata for incremental loading.
    public static CursorPagedResult<T> Create(IReadOnlyList<T> items, string? nextCursor, bool hasMore, int pageSize)
        => new() { Items = items, NextCursor = hasMore ? nextCursor : null, HasMore = hasMore, PageSize = pageSize };
}
