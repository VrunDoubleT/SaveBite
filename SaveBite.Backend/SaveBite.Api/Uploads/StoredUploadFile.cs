using SaveBite.Backend.Exceptions;
namespace SaveBite.Backend.Uploads;
// Constructed by the server-side resource filter, never accepted as a client file.
// Preserves existing multipart contracts and validators without re-uploading binary data.
public sealed class StoredUploadFile(UploadAsset asset, string fieldName) : IFormFile
{
    public UploadAsset Asset { get; } = asset;
    public string ContentType => Asset.ContentType;
    public string ContentDisposition => $"form-data; name=\"{Name}\"; filename=\"{FileName}\"";
    public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
    public long Length => Asset.Bytes;
    public string Name => fieldName;
    public string FileName => Asset.FileName;
    public Stream OpenReadStream() => throw AppException.BadRequest("Stored upload content cannot be read as a new file.");
    public void CopyTo(Stream target) => throw new NotSupportedException();
    public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Services.Interfaces.CloudinaryUploadResult Use()
    {
        if (Asset.Status != "Uploaded" || Asset.Session.Status != "Active" || Asset.ExpiresAt <= DateTime.UtcNow)
            throw AppException.Conflict("Upload is expired, cancelled or already used. Please upload again.");
        Asset.Status = "Attached";
        return new(Asset.Url, Asset.PublicId);
    }
}
