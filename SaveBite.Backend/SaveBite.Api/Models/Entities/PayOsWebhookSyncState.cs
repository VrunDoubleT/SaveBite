using SaveBite.Backend.Audit;

namespace SaveBite.Backend.Models.Entities;

[NoAudit]
public sealed class PayOsWebhookSyncState
{
    public string AccountId { get; set; } = string.Empty;
    public string CredentialVersion { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
    public string EndpointSession { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public bool CredentialsVerified { get; set; }
    public int Attempts { get; set; }
    public DateTime NextAttemptAt { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? SucceededAt { get; set; }
    public string? LastErrorCode { get; set; }
    public DateTime UpdatedAt { get; set; }
}
