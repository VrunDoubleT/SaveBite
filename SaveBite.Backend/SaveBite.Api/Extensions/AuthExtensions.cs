using SaveBite.Backend.Configurations;

namespace SaveBite.Backend.Extensions;

public static class AuthExtensions
{
    public static IServiceCollection AddAuthServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<AuthSettings>()
            .Bind(configuration.GetSection(AuthSettings.SectionName))
            .Validate(x => x.OtpLifetimeMinutes > 0, "OTP lifetime must be positive.")
            .Validate(x => x.OtpResendCooldownSeconds > 0, "OTP cooldown must be positive.")
            .Validate(x => x.MaxOtpRequestsPerEmailPerDay > 0, "Email OTP limit must be positive.")
            .Validate(x => x.MaxOtpVerificationAttempts > 0, "OTP attempt limit must be positive.")
            .Validate(x => x.LoginAttemptWindowMinutes > 0, "Login window must be positive.")
            .Validate(x => x.MaxLoginAttemptsPerEmail > 0, "Email login limit must be positive.")
            .ValidateOnStart();

        return services;
    }
}
