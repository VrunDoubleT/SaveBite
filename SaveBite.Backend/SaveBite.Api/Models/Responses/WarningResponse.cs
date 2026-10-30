namespace SaveBite.Backend.Models.Responses;

public sealed class WarningResponse
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public Guid? IssuedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}