using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Responses;

public static class FeedbackResponses
{
    public sealed record FeedbackItemResponse(
        Guid Id,
        Guid OrderItemId,
        Guid ProductId,
        string ProductName,
        string? VariantName,
        int Rating,
        string? Comment,
        FeedbackStatus Status,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        FeedbackReplyResponse? Reply,
        Guid OrderId,
        string OrderCode,
        string ShopName,
        string? ShopLogoUrl,
        string? ImageUrl,
        IReadOnlyList<string>? Images = null,
        Guid? ShopId = null,
        string? CustomerName = null,
        string? CustomerAvatarUrl = null);

    public sealed record FeedbackReplyResponse(
        Guid Id,
        string Content,
        DateTime CreatedAt,
        DateTime? UpdatedAt = null,
        string? RepliedByName = null);
}
