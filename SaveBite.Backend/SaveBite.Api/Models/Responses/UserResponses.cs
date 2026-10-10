namespace SaveBite.Backend.Models.Responses;

public record UserLogResponse(
    string Action,        // Examples: "SuspendCustomer" and "ChangeRole".
    string Reason,
    string ChangedValues, // JSON example: {"CustomerStatus":"Suspended"}.
    DateTime CreatedAt,
    Guid? AdminId          // Administrator ID.
);

public record UserRoleLogResponse(
    string OldRole,
    string NewRole,
    string Reason,
    DateTime CreatedAt
);

public record UserSummaryResponse(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    string Status,
    string CustomerStatus,
    DateTime CreatedAt
);

public record UserDetailsResponse(
    Guid Id,
    string Email,
    string? Phone,
    string FullName,
    string? AvatarUrl,
    string Role,
    string Status,
    string CustomerStatus,
    string ShopStatus,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<UserLogResponse> Logs,
    List<UserRoleLogResponse> RoleLogs
);
