namespace SaveBite.Backend.Services.Interfaces;

public sealed record CloudinaryUploadResult(
    string Url,
    string PublicId);

public interface ICloudinaryService
{
    Task<CloudinaryUploadResult> UploadImageAsync(IFormFile file, CancellationToken cancellationToken = default);
    Task<bool> DeleteImageAsync(string publicId);
    Task<CloudinaryUploadResult> UploadDocumentAsync(IFormFile file, CancellationToken cancellationToken = default);
}
