namespace SaveBite.Backend.Models.Entities;

public class ProductImage
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }

    public int SortOrder { get; set; }
    public string? PublicId { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public Product Product { get; set; } = null!;
}
