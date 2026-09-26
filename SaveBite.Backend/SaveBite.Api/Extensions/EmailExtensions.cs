using SaveBite.Backend.Configurations;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Extensions;

public static class EmailExtensions
{
    public static IServiceCollection AddEmail(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<EmailOptions>(
            configuration.GetSection("Email"));

        services.AddScoped<IEmailService, SmtpEmailService>();

        return services;
    }
}