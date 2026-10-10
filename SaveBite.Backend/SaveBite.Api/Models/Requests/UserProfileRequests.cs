namespace SaveBite.Backend.Models.Requests;

public sealed class UpdateUserProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; } 
}