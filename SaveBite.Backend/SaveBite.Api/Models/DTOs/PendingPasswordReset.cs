namespace SaveBite.Backend.Models.DTOs;

/// <summary>
/// Holds the already-hashed password while the reset OTP is awaiting verification.
/// </summary>
public sealed record PendingPasswordReset(string PasswordHash);
