using SaveBite.Backend.Audit;

namespace SaveBite.Backend.Models.Entities;

public sealed class PlatformPayOsConfig
{
    public Guid Id { get; set; }
    public string ClientId { get; set; } = string.Empty;
    [AuditIgnore]
    public string ProtectedApiKey { get; set; } = string.Empty;
    [AuditIgnore]
    public string ProtectedChecksumKey { get; set; } = string.Empty;
    public Guid UpdatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User UpdatedByUser { get; set; } = null!;
}
