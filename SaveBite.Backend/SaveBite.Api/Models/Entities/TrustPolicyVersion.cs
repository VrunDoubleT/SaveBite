namespace SaveBite.Backend.Models.Entities;

/// <summary>
/// Global configuration version management for Trust Level (Latest Configuration Wins).
/// Each time you add, edit a requirement point, or enable/disable a level, CurrentVersion will increment by 1.
/// </summary>
public class TrustPolicyVersion
{
    public int Id { get; set; } = 1;

    public long CurrentVersion { get; set; } = 1;

    public DateTime UpdatedAt { get; set; }
}