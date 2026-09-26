namespace SaveBite.Backend.Models.DTOs;

public sealed record PendingRegistration(
    string Email,
    string PasswordHash,
    string FullName);

public sealed record OtpRecord(string Salt, string Hash);
