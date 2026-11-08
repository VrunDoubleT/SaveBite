namespace SaveBite.Backend.Services.Interfaces;

public sealed record CloudinaryUploadResult(
    string Url,
    string PublicId);

public interface ICloudinaryService
{
    Task<CloudinaryUploadResult> UploadImageAsync(IFormFile file, CancellationToken cancellationToken = default);
    Task<bool> DeleteImageAsync(string publicId);
    Task<CloudinaryUploadResult> UploadDocumentAsync(IFormFile file, CancellationToken cancellationToken = default);
    string TemporaryPublicId(Guid assetId);
    Task<CloudinaryUploadResult> UploadTemporaryAsync(IFormFile file, Guid assetId, bool raw, CancellationToken cancellationToken = default);
    Task<bool> DeleteAssetAsync(string publicId, string resourceType, CancellationToken cancellationToken = default);
}
