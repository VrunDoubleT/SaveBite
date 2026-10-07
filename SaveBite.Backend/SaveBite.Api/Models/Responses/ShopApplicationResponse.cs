using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Responses;

public sealed record ShopApplicationResponse(
    Guid Id,
    string Name,
    string? Description,
    string? BusinessLicenseNo,
    string AddressLine,
    string? Ward,
    string? District,
    string? City,
    double Latitude,
    double Longitude,
    string? LogoUrl,
    string? CoverImageUrl,
    TimeOnly? OpeningTime,
    TimeOnly? ClosingTime,
    string Status,
    int RevisionNumber,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string BankName,
    string BankAccountNumber,
    string BankAccountHolder,
    bool HasPayosConfiguration,
    IReadOnlyList<ShopApplicationDocumentResponse> Documents,
    IReadOnlyList<ShopApplicationReviewLogResponse> ReviewLogs);

public sealed record ShopApplicationDocumentResponse(
    Guid Id,
    ShopDocumentType DocumentType,
    string FileUrl,
    string OriginalFileName,
    string ContentType,
    int RevisionNumber,
    bool IsCurrent);

public sealed record ShopApplicationReviewLogResponse(
    string? FromStatus,
    string ToStatus,
    int RevisionNumber,
    string? Note,
    DateTime CreatedAt);
