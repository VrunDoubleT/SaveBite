namespace SaveBite.Backend.Models.DTOs;

public sealed record AccessTokenResult(
    string Token,
    DateTime ExpiresAt);
