namespace SaveBite.Backend.Configurations;

public class CloudinarySettings
{
    public const string SectionName = "Cloudinary";

    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string UploadFolder { get; set; } = "savebite_uploads";
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
    public string[] AllowedContentTypes { get; set; } =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];
}
