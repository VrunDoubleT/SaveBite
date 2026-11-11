namespace SaveBite.Backend.Models.Responses;

public sealed record PlatformPayOsConfigResponse(
    string? MaskedClientId,
    bool HasClientId,
    bool HasApiKey,
    bool HasChecksumKey,
    DateTime? UpdatedAt,
    Guid? UpdatedByUserId,
    string? UpdatedByName);

public sealed record PlatformPayOsConfigHistoryResponse(
    Guid Id,
    Guid ChangedByUserId,
    string ChangedByName,
    IReadOnlyList<string> ChangedFields,
    string? PreviousMaskedClientId,
    string MaskedClientId,
    DateTime ChangedAt);

public sealed record PlatformPayOsCredentials(
    string ClientId,
    string ApiKey,
    string ChecksumKey);
