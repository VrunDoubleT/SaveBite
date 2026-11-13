using SaveBite.Backend.Audit;
using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Entities;

// Business history is independent of audit retention and audit enablement.
[NoAudit]
public sealed class ShopStatusHistory
{
    public Guid Id { get; set; }
    public Guid ShopId { get; set; }
    public ShopStatus? FromStatus { get; set; }
    public ShopStatus ToStatus { get; set; }
    public bool WasSuspendedByAdmin { get; set; }
    public bool WasSuspendedForPlatformFees { get; set; }
    public bool WasSuspendedForOverdueRefunds { get; set; }
    public bool SuspendedByAdmin { get; set; }
    public bool SuspendedForPlatformFees { get; set; }
    public bool SuspendedForOverdueRefunds { get; set; }
    public string ChangedByType { get; set; } = "System";
    public Guid? ChangedByUserId { get; set; }
    public string Reason { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public Shop Shop { get; set; } = null!;
}
