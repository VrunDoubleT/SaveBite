namespace SaveBite.Backend.Models.Requests;

public static class FeedbackRequests
{
    public sealed class CreateFeedbackRequest
    {
        public Guid OrderItemId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }

    public sealed class UpdateFeedbackRequest
    {
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}
