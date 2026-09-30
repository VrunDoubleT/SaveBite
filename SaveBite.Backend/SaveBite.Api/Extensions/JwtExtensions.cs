using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SaveBite.Backend.Configurations;

namespace SaveBite.Backend.Extensions;

public static class JwtExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(
            JwtSettings.SectionName);
        var settings = section.Get<JwtSettings>() ?? new JwtSettings();

        ValidateSettings(settings);

        services.AddOptions<JwtSettings>()
            .Bind(section)
            .Validate(
                value => !string.IsNullOrWhiteSpace(value.Issuer),
                "JWT Issuer is required.")
            .Validate(
                value => !string.IsNullOrWhiteSpace(value.Audience),
                "JWT Audience is required.")
            .Validate(
                value => Encoding.UTF8.GetByteCount(value.SigningKey) >= 32,
                "JWT SigningKey must contain at least 32 bytes.")
            .Validate(
                value => value.AccessTokenLifetimeMinutes > 0,
                "JWT access-token lifetime must be greater than zero.")
            .Validate(
                value => value.RefreshTokenLifetimeDays > 0,
                "JWT refresh-token lifetime must be greater than zero.")
            .ValidateOnStart();

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(settings.SigningKey));

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme =
                    JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.SaveToken = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role
                };
            });

        services.AddSingleton(TimeProvider.System);

        return services;
    }

    private static void ValidateSettings(JwtSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Issuer))
            throw new InvalidOperationException(
                "JWT Issuer is required.");

        if (string.IsNullOrWhiteSpace(settings.Audience))
            throw new InvalidOperationException(
                "JWT Audience is required.");

        if (Encoding.UTF8.GetByteCount(settings.SigningKey) < 32)
            throw new InvalidOperationException(
                "JWT SigningKey must contain at least 32 bytes.");

        if (settings.AccessTokenLifetimeMinutes <= 0 ||
            settings.RefreshTokenLifetimeDays <= 0)
        {
            throw new InvalidOperationException(
                "JWT token lifetimes must be greater than zero.");
        }
    }
}
