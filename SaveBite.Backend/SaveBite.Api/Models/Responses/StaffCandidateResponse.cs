namespace SaveBite.Backend.Models.Responses;

public sealed record StaffCandidateResponse(
    Guid UserId,
    string FullName,
    string Email
);