using SaveBite.Backend.Audit;

namespace SaveBite.Backend.Models.Entities;

[NoAudit]
public class ShopPaymentConfig
{
    public int Id { get; set; }
    public Guid ShopId { get; set; }

    public string PayosClientId { get; set; } = string.Empty;
    [AuditIgnore]
    public string PayosApiKey { get; set; } = string.Empty;
    [AuditIgnore]
    public string PayosChecksumKey { get; set; } = string.Empty;

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Shop Shop { get; set; } = null!;
}
