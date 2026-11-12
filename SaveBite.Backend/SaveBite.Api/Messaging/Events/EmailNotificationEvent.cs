namespace SaveBite.Backend.Messaging.Events;

public class EmailNotificationEvent
{
    public string? BusinessKind { get; set; }
    public Guid? BusinessSubjectId { get; set; }
    public DateTime? ExpectedDeadline { get; set; }
    public decimal? ExpectedOutstandingAmount { get; set; }

    public string? DeliveryKey { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public string To { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public bool IsHtml { get; set; } = true;
}
