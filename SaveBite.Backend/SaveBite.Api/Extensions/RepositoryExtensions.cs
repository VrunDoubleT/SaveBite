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
        services.AddScoped<IShopStaffRepository,
            ShopStaffRepository>();
       
        services.AddScoped<IAdminCategoryRepository, AdminCategoryRepository>();
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();

        return services;
    }
}
