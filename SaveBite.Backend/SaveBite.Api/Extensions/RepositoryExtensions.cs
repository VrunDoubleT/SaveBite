using SaveBite.Backend.Repositories.Implementations;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using FlashDealRepository = SaveBite.Backend.Repositories.Implementations.FlashDealRepository;

namespace SaveBite.Backend.Extensions;

public static class RepositoryExtensions
{
    public static IServiceCollection AddRepositories(
        this IServiceCollection services)
    {
        services.AddScoped<IUserAccessRepository, UserAccessRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IShopStaffRepository, ShopStaffRepository>();
        services.AddScoped<IStaffInvitationRepository, StaffInvitationRepository>();

        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IShopApplicationRepository, ShopApplicationRepository>();

        services.AddScoped<IUserAddressRepository, UserAddressRepository>();
        services.AddScoped<IShopRepository, ShopRepository>();
        services.AddScoped<IFlashDealRepository, FlashDealRepository>();
        services.AddScoped<ITrustScoreRepository, TrustScoreRepository>();
        services.AddScoped<ITrustOrderRepository, TrustOrderRepository>();
        services.AddScoped<ITrustConfigRepository, TrustConfigRepository>();
        

        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<ICheckoutRepository, CheckoutRepository>();
        services.AddScoped<IPaymentHistoryRepository, PaymentHistoryRepository>();
        services.AddScoped<IOrderEventRepository, OrderEventRepository>();
        services.AddScoped<IShopOrderRepository, ShopOrderRepository>();
        services.AddScoped<IPlatformFeeConfigService, PlatformFeeConfigService>();
        services.AddScoped<IPlatformFeeStatementRepository, PlatformFeeStatementRepository>();
        services.AddScoped<IPlatformFeeConfigRepository, PlatformFeeConfigRepository>();
        services.AddScoped<IRefundRepository, RefundRepository>();
        return services;
    }
}