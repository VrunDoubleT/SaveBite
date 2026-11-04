namespace SaveBite.Backend.Models.Entities;

// Sending survives crashes: an ambiguous SMTP outcome must not be automatically resent.
public sealed class EmailDelivery
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DeliveryKey { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime UpdatedAt { get; set; }
    public DateTime? SentAt { get; set; }
}
