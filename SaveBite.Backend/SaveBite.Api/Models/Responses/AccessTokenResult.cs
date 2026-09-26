namespace SaveBite.Backend.Models.Responses;

public sealed record AccessTokenResult(
    string Token,
    DateTime ExpiresAt);
