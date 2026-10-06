using SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.Services.Implementations;

namespace SaveBite.Backend.Extensions;

public static class ShopStaffExtensions
{
    public static IServiceCollection AddShopStaffServices(
        this IServiceCollection services)
    {
        services.AddScoped<IShopStaffService, ShopStaffService>();

        return services;
    }
}