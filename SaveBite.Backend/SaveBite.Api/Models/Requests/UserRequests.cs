using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Models.Requests;

public sealed class GetUsersRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public UserRole? Role { get; set; }
    public UserStatus? Status { get; set; }
}

public sealed class UpdateUserStatusRequest
{
    public bool IsSuspended { get; set; }
    public bool IsCustomerProfile { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class UpdateUserProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; } 
}
