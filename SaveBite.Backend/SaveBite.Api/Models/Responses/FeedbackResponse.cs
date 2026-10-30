using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Responses;

public sealed class FeedbackResponse
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public Guid UserId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string? CustomerPhone { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public string ShopName { get; init; } = string.Empty;
    public Guid OrderItemId { get; init; }
    public int Rating { get; init; }
    public string? Comment { get; init; }
    public FeedbackStatus Status { get; init; }
    public string? CurrentReason { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? HiddenAt { get; init; }
}