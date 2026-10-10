using SaveBite.Backend.Repositories.Implementations;
using SaveBite.Backend.Repositories.Interfaces;

namespace SaveBite.Backend.Extensions;

public static class RepositoryExtensions
{
    public static IServiceCollection AddRepositories(
        this IServiceCollection services)
    {
        services.AddScoped<IUserAccessRepository, UserAccessRepository>();
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IShopStaffRepository, ShopStaffRepository>();
        services.AddScoped<IStaffInvitationRepository, StaffInvitationRepository>();
       
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IShopApplicationRepository, ShopApplicationRepository>();

        services.AddScoped<IUserAddressRepository, UserAddressRepository>();
        services.AddScoped<IShopRepository, ShopRepository>();
        services.AddScoped<IFlashDealRepository, FlashDealRepository>();
        return services;
    }
}
