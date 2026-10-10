namespace SaveBite.Backend.Models.DTOs;

// Holds the already-hashed password while the reset OTP is awaiting verification.
public sealed record PendingPasswordReset(string PasswordHash);
