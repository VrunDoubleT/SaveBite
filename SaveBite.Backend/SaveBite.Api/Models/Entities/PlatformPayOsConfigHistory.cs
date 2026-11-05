namespace SaveBite.Backend.Models.Entities;

public sealed class PlatformPayOsConfigHistory
{
    public Guid Id { get; set; }
    public Guid ChangedByUserId { get; set; }
    public string ChangedFieldsJson { get; set; } = "[]";
    public string? PreviousMaskedClientId { get; set; }
    public string MaskedClientId { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }

    public User ChangedByUser { get; set; } = null!;
}
