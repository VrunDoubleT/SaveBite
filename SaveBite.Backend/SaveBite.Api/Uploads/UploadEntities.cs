namespace SaveBite.Backend.Uploads;

public sealed class UploadSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Purpose { get; set; } = "";
    public Guid? TargetId { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public Guid? ResultId { get; set; }
    public List<UploadAsset> Assets { get; set; } = [];
}
public sealed class UploadAsset
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public UploadSession Session { get; set; } = null!;
    public string Status { get; set; } = "Uploading";
    public string Url { get; set; } = "";
    public string PublicId { get; set; } = "";
    public string ResourceType { get; set; } = "image";
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Bytes { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
