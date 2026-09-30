using SaveBite.Backend.Configurations;

namespace SaveBite.Backend.Extensions;

public static class EmailExtensions
{
    public static IServiceCollection AddEmail(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<EmailOptions>(
            configuration.GetSection("Email"));

        return services;
    }
}
