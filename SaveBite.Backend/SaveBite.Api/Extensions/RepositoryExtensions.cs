using SaveBite.Backend.Repositories.Implementations;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.Extensions;

public static class RepositoryExtensions
{
    public static IServiceCollection AddRepositories(
        this IServiceCollection services)
    {
        services.AddScoped<IUserAccessRepository,
            UserAccessRepository>();
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IAdminCategoryRepository, AdminCategoryRepository>();

        return services;
    }
}
