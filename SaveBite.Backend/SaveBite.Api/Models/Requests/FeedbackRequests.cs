namespace SaveBite.Backend.Models.Requests;

public static class FeedbackRequests
{
    public sealed class CreateFeedbackRequest
    {
        public Guid OrderItemId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public List<string>? Images { get; set; }
    }

    public sealed class UpdateFeedbackRequest
    {
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public List<string>? Images { get; set; }
    }

    public sealed class CreateFeedbackReplyRequest
    {
        public string Content { get; set; } = string.Empty;
    }

    public sealed class UpdateFeedbackReplyRequest
    {
        public string Content { get; set; } = string.Empty;
    }

    public sealed class ShopFeedbacksQueryRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int? Rating { get; set; }
        public bool? HasReply { get; set; }
        public Guid? ShopId { get; set; }
    }
}
