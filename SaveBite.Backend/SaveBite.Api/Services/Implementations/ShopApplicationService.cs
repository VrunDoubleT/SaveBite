using FluentValidation;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.Validators;

namespace SaveBite.Backend.Services.Implementations;

public sealed class ShopApplicationService : IShopApplicationService
{
    private readonly IShopApplicationRepository _repository;
    private readonly ICloudinaryService _cloudinary;
    private readonly IValidator<CreateShopApplicationRequest> _createValidator;
    private readonly IValidator<ResubmitShopApplicationRequest> _resubmitValidator;
    private readonly IValidator<ShopApplicationDocumentsValidationRequest> _documentsValidator;

    public ShopApplicationService(
        IShopApplicationRepository repository,
        ICloudinaryService cloudinary,
        IValidator<CreateShopApplicationRequest> createValidator,
        IValidator<ResubmitShopApplicationRequest> resubmitValidator,
        IValidator<ShopApplicationDocumentsValidationRequest> documentsValidator)
    {
        _repository = repository;
        _cloudinary = cloudinary;
        _createValidator = createValidator;
        _resubmitValidator = resubmitValidator;
        _documentsValidator = documentsValidator;
    }

    public async Task<ShopApplicationResponse?> GetMyApplicationAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var application = await _repository.GetLatestForApplicantAsync(userId, cancellationToken);
        return application is null ? null : Map(application);
    }

    public async Task<IReadOnlyList<ShopApplicationResponse>> GetMyApplicationHistoryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var applications = await _repository.GetAllForApplicantAsync(userId, cancellationToken);
        return applications.Select(x => Map(x, includeAllDocuments: true)).ToList();
    }

    public async Task<ShopApplicationResponse> CreateAsync(
        Guid userId,
        CreateShopApplicationRequest request,
        IFormFile? logo,
        IFormFile? coverImage,
        IReadOnlyList<IFormFile> documents,
        IReadOnlyList<string> documentTypes,
        CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);
        await ValidateDocumentsAsync(documents, documentTypes, cancellationToken);

        if (await _repository.HasActiveApplicationAsync(userId, cancellationToken))
            throw AppException.Conflict("You already have an active shop application.");

        var now = DateTime.UtcNow;

        var application = new ShopApplication
        {
            Id = Guid.NewGuid(),
            ApplicantUserId = userId,
            Name = request.Name.Trim(),
            Description = Clean(request.Description),
            BusinessLicenseNo = Clean(request.BusinessLicenseNo),
            AddressLine = request.AddressLine.Trim(),
            Ward = Clean(request.Ward),
            District = Clean(request.District),
            City = Clean(request.City),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            OpeningTime = request.OpeningTime,
            ClosingTime = request.ClosingTime,
            BankName = request.BankName.Trim(),
            BankAccountNumber = request.BankAccountNumber.Trim(),
            BankAccountHolder = request.BankAccountHolder.Trim(),
            PayosClientId = request.PayosClientId.Trim(),
            PayosApiKey = request.PayosApiKey.Trim(),
            PayosChecksumKey = request.PayosChecksumKey.Trim(),
            Status = ShopApplicationStatus.Pending,
            RevisionNumber = 1,
            CreatedAt = now,
            UpdatedAt = now
        };

        await UploadImagesAsync(application, logo, coverImage, cancellationToken);
        await AddDocumentsAsync(application, documents, documentTypes, userId, cancellationToken);

        application.ReviewLogs.Add(new ShopApplicationReviewLog
        {
            ApplicationId = application.Id,
            FromStatus = null,
            ToStatus = ShopApplicationStatus.Pending,
            RevisionNumber = application.RevisionNumber,
            CreatedAt = now
        });

        await _repository.AddAsync(application, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Map(application);
    }

    public async Task<ShopApplicationResponse> ResubmitAsync(
        Guid userId,
        ResubmitShopApplicationRequest request,
        IFormFile? logo,
        IFormFile? coverImage,
        IReadOnlyList<IFormFile> documents,
        IReadOnlyList<string> documentTypes,
        CancellationToken cancellationToken = default)
    {
        await _resubmitValidator.ValidateAndThrowAsync(request, cancellationToken);
        await ValidateDocumentsAsync(documents, documentTypes, cancellationToken);

        var application = await _repository.GetByIdForApplicantAsync(request.ApplicationId, userId, cancellationToken)
            ?? throw AppException.NotFound("Shop application was not found.");

        ValidateRequiredDocumentTypes(
            application.Documents.Where(d => d.IsCurrent).Select(d => d.DocumentType.ToString()),
            documentTypes);

        if (application.Status is not (ShopApplicationStatus.Rejected or ShopApplicationStatus.Cancelled))
            throw AppException.Conflict("Only rejected or cancelled shop applications can be edited and resubmitted.");
        
        var now = DateTime.UtcNow;

        application.Name = request.Name.Trim();
        application.Description = Clean(request.Description);
        application.BusinessLicenseNo = Clean(request.BusinessLicenseNo);
        application.AddressLine = request.AddressLine.Trim();
        application.Ward = Clean(request.Ward);
        application.District = Clean(request.District);
        application.City = Clean(request.City);
        application.Latitude = request.Latitude;
        application.Longitude = request.Longitude;
        application.OpeningTime = request.OpeningTime;
        application.ClosingTime = request.ClosingTime;
        application.BankName = request.BankName.Trim();
        application.BankAccountNumber = request.BankAccountNumber.Trim();
        application.BankAccountHolder = request.BankAccountHolder.Trim();

        // Keep existing PayOS credentials when no replacement is provided.
        if (!string.IsNullOrWhiteSpace(request.PayosClientId))
            application.PayosClientId = request.PayosClientId.Trim();

        if (!string.IsNullOrWhiteSpace(request.PayosApiKey))
            application.PayosApiKey = request.PayosApiKey.Trim();

        if (!string.IsNullOrWhiteSpace(request.PayosChecksumKey))
            application.PayosChecksumKey = request.PayosChecksumKey.Trim();

        var fromStatus = application.Status;

        application.Status = ShopApplicationStatus.Pending;
        application.RevisionNumber++;
        application.UpdatedAt = now;

        await UploadImagesAsync(application, logo, coverImage, cancellationToken);

        if (documents.Count > 0)
            MarkReplacedDocumentsNotCurrent(application, documentTypes);

        await AddDocumentsAsync(application, documents, documentTypes, userId, cancellationToken);

        application.ReviewLogs.Add(new ShopApplicationReviewLog
        {
            ApplicationId = application.Id,
            FromStatus = fromStatus,
            ToStatus = ShopApplicationStatus.Pending,
            RevisionNumber = application.RevisionNumber,
            CreatedAt = now
        });

        await _repository.SaveChangesAsync(cancellationToken);

        return Map(application);
    }

    public async Task CancelAsync(Guid userId, Guid applicationId, CancellationToken cancellationToken = default)
    {
        var application = await _repository.GetByIdForApplicantAsync(applicationId, userId, cancellationToken)
            ?? throw AppException.NotFound("Shop application was not found.");

        if (application.Status != ShopApplicationStatus.Pending)
            throw AppException.Conflict("Only a pending shop application can be cancelled.");

        var now = DateTime.UtcNow;

        application.Status = ShopApplicationStatus.Cancelled;
        application.UpdatedAt = now;

        application.ReviewLogs.Add(new ShopApplicationReviewLog
        {
            ApplicationId = application.Id,
            FromStatus = ShopApplicationStatus.Pending,
            ToStatus = ShopApplicationStatus.Cancelled,
            RevisionNumber = application.RevisionNumber,
            CreatedAt = now
        });

        await _repository.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateDocumentsAsync(IReadOnlyList<IFormFile> documents, IReadOnlyList<string> documentTypes, CancellationToken cancellationToken)
    {
        await _documentsValidator.ValidateAndThrowAsync(new ShopApplicationDocumentsValidationRequest(documents, documentTypes), cancellationToken);
    }

    private async Task UploadImagesAsync(ShopApplication application, IFormFile? logo, IFormFile? cover, CancellationToken cancellationToken)
    {
        try
        {
            if (logo is not null)
            {
                application.LogoUrl = (await _cloudinary.UploadImageAsync(logo, cancellationToken)).Url;
            }

            if (cover is not null)
            {
                application.CoverImageUrl = (await _cloudinary.UploadImageAsync(cover, cancellationToken)).Url;
            }
        }
        catch (ArgumentException ex)
        {
            throw AppException.BadRequest(ex.Message);
        }
    }

    private async Task AddDocumentsAsync(
        ShopApplication application,
        IReadOnlyList<IFormFile> files,
        IReadOnlyList<string> types,
        Guid userId,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < files.Count; i++)
        {
            // Document types are validated before this method is called.
            var type = Enum.Parse<ShopDocumentType>(types[i], true);
            CloudinaryUploadResult upload;

            try
            {
                upload = await _cloudinary.UploadDocumentAsync(files[i], cancellationToken);
            }
            catch (ArgumentException ex)
            {
                throw AppException.BadRequest(ex.Message);
            }

            application.Documents.Add(new ShopApplicationDocument
            {
                ApplicationId = application.Id,
                DocumentType = type,
                FileUrl = upload.Url,
                OriginalFileName = Path.GetFileName(files[i].FileName),
                ContentType = files[i].ContentType,
                RevisionNumber = application.RevisionNumber,
                UploadedBy = userId,
                UploadedAt = DateTime.UtcNow,
                IsCurrent = true
            });
        }
    }

    private static void MarkReplacedDocumentsNotCurrent(ShopApplication application, IReadOnlyList<string> replacementTypes)
    {
        var types = replacementTypes
            .Select(type => Enum.TryParse<ShopDocumentType>(type, true, out var parsed) ? parsed : (ShopDocumentType?)null)
            .Where(type => type.HasValue)
            .Select(type => type!.Value)
            .ToHashSet();

        foreach (var document in application.Documents.Where(document => types.Contains(document.DocumentType)))
            document.IsCurrent = false;
    }

    private static void ValidateRequiredDocumentTypes(params IEnumerable<string>[] typeGroups)
    {
        var types = typeGroups
            .SelectMany(group => group)
            .Select(type => Enum.TryParse<ShopDocumentType>(type, true, out var parsed) ? parsed : (ShopDocumentType?)null)
            .Where(type => type.HasValue)
            .Select(type => type!.Value)
            .ToHashSet();

        if (!types.Contains(ShopDocumentType.BusinessLicense))
            throw AppException.BadRequest("Business license document is required.");

        if (!types.Contains(ShopDocumentType.FoodSafetyCertificate))
            throw AppException.BadRequest("Food safety certificate is required.");
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ShopApplicationResponse Map(ShopApplication x, bool includeAllDocuments = false) => new(
        x.Id,
        x.Name,
        x.Description,
        x.BusinessLicenseNo,
        x.AddressLine,
        x.Ward,
        x.District,
        x.City,
        x.Latitude,
        x.Longitude,
        x.LogoUrl,
        x.CoverImageUrl,
        x.OpeningTime,
        x.ClosingTime,
        x.Status.ToString(),
        x.RevisionNumber,
        x.CreatedAt,
        x.UpdatedAt,
        x.BankName,
        x.BankAccountNumber,
        x.BankAccountHolder,
        !string.IsNullOrWhiteSpace(x.PayosClientId),
        x.Documents
            .Where(d => includeAllDocuments || d.IsCurrent)
            .OrderByDescending(d => d.RevisionNumber)
            .ThenByDescending(d => d.UploadedAt)
            .Select(d => new ShopApplicationDocumentResponse(
                d.Id,
                d.DocumentType.ToString(),
                d.FileUrl,
                d.OriginalFileName,
                d.ContentType,
                d.RevisionNumber,
                d.IsCurrent))
            .ToList(),
        x.ReviewLogs
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ShopApplicationReviewLogResponse(
                r.FromStatus?.ToString(),
                r.ToStatus.ToString(),
                r.RevisionNumber,
                r.Note,
                r.CreatedAt))
            .ToList());
}