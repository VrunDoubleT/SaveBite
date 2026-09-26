namespace SaveBite.Backend.Models.Responses;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
 
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
 
    public int TotalPages =>
        PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);
 
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
 
    public static PagedResult<T> Create(
        IReadOnlyList<T> items, int page, int pageSize, int totalItems)
        => new()
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
}