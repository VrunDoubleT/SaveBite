using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using SaveBite.Backend.Configurations;
using SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.Uploads;

namespace SaveBite.Backend.Services.Implementations;

public class CloudinaryService : ICloudinaryService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryService> _logger;
    private readonly CloudinarySettings _settings;
    private readonly HashSet<string> _allowedContentTypes;

    public CloudinaryService(
        IOptions<CloudinarySettings> options,
        ILogger<CloudinaryService> logger, IServiceScopeFactory scopes)
    {
        _scopes = scopes;
        _logger = logger;

        _settings = options.Value;
        _allowedContentTypes = new HashSet<string>(
            _settings.AllowedContentTypes,
            StringComparer.OrdinalIgnoreCase);

        var account = new Account(
            _settings.CloudName,
            _settings.ApiKey,
            _settings.ApiSecret
        );

        _cloudinary = new Cloudinary(account);
    }

    public async Task<CloudinaryUploadResult> UploadImageAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (file is StoredUploadFile storedFile)
            return storedFile.Use();
        ValidateFile(file);

        await using var stream = file.OpenReadStream();

        if (!stream.CanSeek)
            throw new InvalidOperationException(
                "The uploaded image stream must support seeking.");

        if (!await HasValidImageSignatureAsync(
                stream,
                file.ContentType,
                cancellationToken))
        {
            throw new ArgumentException(
                "The file content does not match its declared image type.",
                nameof(file));
        }

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(
                Path.GetFileName(file.FileName),
                stream),
            Folder = _settings.UploadFolder,
            UseFilename = false,
            UniqueFilename = true,
            Overwrite = false
        };

        try
        {
            var result = await _cloudinary.UploadAsync(
                uploadParams,
                cancellationToken);

            if (result.StatusCode != System.Net.HttpStatusCode.OK ||
                result.SecureUrl is null ||
                string.IsNullOrWhiteSpace(result.PublicId))
            {
                _logger.LogError(
                    "Cloudinary upload failed. FileName: {FileName}, StatusCode: {StatusCode}, Error: {Error}",
                    file.FileName,
                    result.StatusCode,
                    result.Error?.Message);

                throw new InvalidOperationException(
                    "Cloudinary did not accept the image upload.");
            }

            _logger.LogInformation(
                "Image uploaded to Cloudinary. PublicId: {PublicId}",
                result.PublicId);

            return new CloudinaryUploadResult(
                result.SecureUrl.ToString(),
                result.PublicId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error while uploading image {FileName} to Cloudinary",
                file.FileName);

            throw;
        }
    }

    public async Task<bool> DeleteImageAsync(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            throw new ArgumentException(
                "Public ID is required.",
                nameof(publicId));

        var normalizedPublicId = publicId.Trim();
        var folderPrefix = $"{_settings.UploadFolder}/";

        if (!normalizedPublicId.StartsWith(
                folderPrefix,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The public ID is outside the configured upload folder.",
                nameof(publicId));
        }

        // Historical assets are retained even after disappearing from the live catalogue.
        await using var scope = _scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var assetPath = "/" + normalizedPublicId + ".";
        if (await db.OrderItems.AnyAsync(i => i.ProductImageUrlSnapshot != null && i.ProductImageUrlSnapshot.Contains(assetPath))
            || await db.Orders.AnyAsync(o => o.ShopLogoUrlSnapshot != null && o.ShopLogoUrlSnapshot.Contains(assetPath))
            || await db.PlatformFeeStatements.AnyAsync(s => s.ShopLogoUrlSnapshot != null && s.ShopLogoUrlSnapshot.Contains(assetPath))
            || await db.Set<SaveBite.Backend.Models.Entities.RefundItem>().AnyAsync(i => i.ImageUrl != null && i.ImageUrl.Contains(assetPath)))
        {
            _logger.LogInformation("Retaining image {PublicId} referenced by purchase history.", normalizedPublicId);
            return true;
        }

        try
        {
            var result = await _cloudinary.DestroyAsync(
                new DeletionParams(normalizedPublicId)
                {
                    ResourceType = ResourceType.Image
                });

            if (result.Result == "ok")
            {
                _logger.LogInformation(
                    "Image deleted from Cloudinary. PublicId: {PublicId}",
                    normalizedPublicId);

                return true;
            }

            _logger.LogWarning(
                "Cloudinary did not delete image. PublicId: {PublicId}, Result: {Result}, Error: {Error}",
                normalizedPublicId,
                result.Result,
                result.Error?.Message);

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error while deleting image {PublicId} from Cloudinary",
                normalizedPublicId);

            throw;
        }
    }

    private void ValidateFile(IFormFile file)
    {
        if (file.Length <= 0)
            throw new ArgumentException(
                "The image file is empty.",
                nameof(file));

        if (file.Length > _settings.MaxFileSizeBytes)
            throw new ArgumentException(
                $"The image exceeds the maximum size of {_settings.MaxFileSizeBytes} bytes.",
                nameof(file));

        if (!_allowedContentTypes.Contains(file.ContentType))
            throw new ArgumentException(
                "The image content type is not allowed.",
                nameof(file));
    }

    private static async Task<bool> HasValidImageSignatureAsync(
        Stream stream,
        string contentType,
        CancellationToken cancellationToken)
    {
        var header = new byte[12];
        var bytesRead = 0;

        while (bytesRead < header.Length)
        {
            var read = await stream.ReadAsync(
                header.AsMemory(bytesRead),
                cancellationToken);

            if (read == 0)
                break;

            bytesRead += read;
        }

        stream.Position = 0;

        return contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => bytesRead >= 3 &&
                            header[0] == 0xFF &&
                            header[1] == 0xD8 &&
                            header[2] == 0xFF,
            "image/png" => bytesRead >= 8 &&
                           header.AsSpan(0, 8).SequenceEqual(
                               new byte[]
                               {
                                   0x89, 0x50, 0x4E, 0x47,
                                   0x0D, 0x0A, 0x1A, 0x0A
                               }),
            "image/webp" => bytesRead >= 12 &&
                            header.AsSpan(0, 4).SequenceEqual(
                                "RIFF"u8) &&
                            header.AsSpan(8, 4).SequenceEqual(
                                "WEBP"u8),
            _ => false
        };
    }
    
    public async Task<CloudinaryUploadResult> UploadDocumentAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (file is StoredUploadFile storedFile)
            return storedFile.Use();

        if (file.Length <= 0)
            throw new ArgumentException("The document file is empty.", nameof(file));

        if (file.Length > _settings.MaxFileSizeBytes)
            throw new ArgumentException($"The document exceeds the maximum size of {_settings.MaxFileSizeBytes} bytes.", nameof(file));

        var allowed = new[]
        {
            "application/pdf",
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        if (!allowed.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("The document type is not allowed. Supported formats are PDF, JPG, PNG and WEBP.", nameof(file));

        await using var stream = file.OpenReadStream();

        var uploadParams = new RawUploadParams
        {
            File = new FileDescription(Path.GetFileName(file.FileName), stream),
            Folder = $"{_settings.UploadFolder}/shop_registrations/documents",
            PublicId = $"{Path.GetFileNameWithoutExtension(file.FileName)}_{Guid.NewGuid():N}",
            Overwrite = false
        };

        try
        {
            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.StatusCode != System.Net.HttpStatusCode.OK || result.SecureUrl is null || string.IsNullOrWhiteSpace(result.PublicId))
            {
                throw new InvalidOperationException("Cloudinary did not accept the document upload.");
            }

            _logger.LogInformation("Document uploaded. FileName: {FileName}, PublicId: {PublicId}", file.FileName, result.PublicId);

            return new CloudinaryUploadResult(result.SecureUrl.ToString(), result.PublicId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document {FileName} to Cloudinary.", file.FileName);
            throw;
        }
    }

    public string TemporaryPublicId(Guid assetId)
        => $"{_settings.UploadFolder}/temporary/{assetId:N}";

    public async Task<CloudinaryUploadResult> UploadTemporaryAsync(
        IFormFile file,
        Guid assetId,
        bool raw,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        if (raw)
        {
            if (file.ContentType != "application/pdf" || file.Length <= 0 ||
                file.Length > _settings.MaxFileSizeBytes)
                throw new ArgumentException("Upload a valid PDF within the configured size limit.", nameof(file));
        }
        else
        {
            ValidateFile(file);
        }

        await using var stream = file.OpenReadStream();
        if (!raw && (!stream.CanSeek ||
            !await HasValidImageSignatureAsync(stream, file.ContentType, cancellationToken)))
            throw new ArgumentException("The file content does not match its declared image type.", nameof(file));

        var publicId = TemporaryPublicId(assetId);
        UploadResult result;
        if (raw)
        {
            result = await _cloudinary.UploadAsync(new RawUploadParams
            {
                File = new FileDescription(Path.GetFileName(file.FileName), stream),
                PublicId = publicId,
                // Retries must never replace content already published under this asset ID.
                Overwrite = false,
                Invalidate = true
            });
        }
        else
        {
            result = await _cloudinary.UploadAsync(new ImageUploadParams
            {
                File = new FileDescription(Path.GetFileName(file.FileName), stream),
                PublicId = publicId,
                // Retries must never replace content already published under this asset ID.
                Overwrite = false,
                Invalidate = true
            }, cancellationToken);
        }

        if (result.StatusCode != System.Net.HttpStatusCode.OK ||
            result.SecureUrl is null || string.IsNullOrWhiteSpace(result.PublicId))
            throw new InvalidOperationException("Cloudinary did not accept the temporary upload.");

        return new CloudinaryUploadResult(result.SecureUrl.ToString(), result.PublicId);
    }

    public async Task<bool> DeleteAssetAsync(
        string publicId,
        string resourceType,
        CancellationToken cancellationToken = default)
    {
        var prefix = $"{_settings.UploadFolder}/temporary/";
        if (!publicId.StartsWith(prefix, StringComparison.Ordinal) ||
            resourceType is not ("image" or "raw"))
            throw new ArgumentException("The asset is not an allowed temporary upload.", nameof(publicId));

        cancellationToken.ThrowIfCancellationRequested();
        var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId)
        {
            ResourceType = resourceType == "raw" ? ResourceType.Raw : ResourceType.Image,
            Invalidate = true
        });
        return result.Result is "ok" or "not found";
    }
}
