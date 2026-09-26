using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Extensions;

public static class PasswordHasherExtensions
{
    public static IServiceCollection AddPasswordHasher(
        this IServiceCollection services)
    {
        services.AddSingleton<
            IPasswordHasherService,
            PasswordHasherService>();

        return services;
    }
}
