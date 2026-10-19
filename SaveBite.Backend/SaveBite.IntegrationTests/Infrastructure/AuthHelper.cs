using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;

namespace SaveBite.IntegrationTests.Infrastructure;

public static class AuthHelper
{
    public const string DefaultIssuer = "SaveBite.Api";
    public const string DefaultAudience = "SaveBite.Client";
    public const string DefaultSigningKey = "SaeveBiteSecretKeyForJwtTokenGeneration123456";

    public static string GenerateAccessToken(
        Guid userId,
        string email,
        UserRole role,
        string issuer = DefaultIssuer,
        string audience = DefaultAudience,
        string signingKey = DefaultSigningKey)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddHours(1);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role.ToString())
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string GenerateAccessToken(User user)
    {
        return GenerateAccessToken(user.Id, user.Email, user.Role);
    }
}
