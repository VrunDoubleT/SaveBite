namespace SaveBite.Backend.Models.Requests;

public sealed class UpdatePlatformPayOsConfigRequest
{
    public string? ClientId { get; set; }
    public string? ApiKey { get; set; }
    public string? ChecksumKey { get; set; }
}
