namespace SaveBite.Backend.Models.Responses;

public sealed record TokenPairResult(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);
