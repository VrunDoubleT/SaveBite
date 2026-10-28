namespace SaveBite.Backend.Models.Requests;

public static class NotificationRequests
{
    public sealed class GetNotificationsRequest
    {
        public int Page { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public bool? IsUnreadOnly { get; init; }
    }
}
