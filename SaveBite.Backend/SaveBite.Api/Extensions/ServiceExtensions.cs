using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddServices(
        this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserAccessService, UserAccessService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IUserAddressService, UserAddressService>();
        services.AddScoped<IUserProfileService, UserProfileService>();

        services.AddSingleton<IJwtService, JwtService>();
        services.AddSingleton<IPasswordHasherService,
            PasswordHasherService>();
        services.AddSingleton<ICloudinaryService, CloudinaryService>();

        return services;
    }
}
