using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddServices(
        this IServiceCollection services)
    {
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserAccessService, UserAccessService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IShopApplicationService, ShopApplicationService>();
        services.AddScoped<IShopStaffService, ShopStaffService>();
        services.AddScoped<IStaffInvitationService, StaffInvitationService>();
        services.AddScoped<IUserAddressService, UserAddressService>();

        services.AddScoped<IShopService, ShopService>();
        services.AddScoped<IFlashDealService, FlashDealService>();
        services.AddScoped<ITrustScoreService, TrustScoreService>();
        services.AddScoped<ITrustOrderService, TrustOrderService>();
        services.AddScoped<ITrustConfigService, TrustConfigService>();
        services.AddSingleton<IJwtService, JwtService>();
        services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
        services.AddSingleton<ICloudinaryService, CloudinaryService>();
        services.AddScoped<IFeedbackService, FeedbackService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPaymentHistoryService, PaymentHistoryService>();
        services.AddScoped<IOrderEventPublisher, OrderEventPublisher>();
        services.AddScoped<IShopOrderEventService, ShopOrderEventService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ICheckoutService, CheckoutService>();
        services.AddScoped<IShopOrderService, ShopOrderService>();
        services.AddScoped<ICheckoutPaymentService, CheckoutPaymentService>();
        services.AddScoped<ICheckoutPaymentGateway>(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var environment = provider.GetRequiredService<IHostEnvironment>();
            if (configuration["Checkout:PaymentProvider"] == "Simulated")
            {
                if (!environment.IsDevelopment()) throw new InvalidOperationException("Simulated payments require Development.");
                return new DevelopmentCheckoutGateway();
            }
            return ActivatorUtilities.CreateInstance<PayOsCheckoutGateway>(provider);
        });
        services.AddScoped<ITravelTimeService, VietMapTravelTimeService>();
        services.AddHttpClient("VietMapRouting", client =>
        {
            client.BaseAddress = new Uri("https://maps.vietmap.vn/");
            client.Timeout = TimeSpan.FromSeconds(15);
        }).RemoveAllLoggers();
        services.AddHttpClient("PayOsCheckout", client =>
        {
            client.BaseAddress = new Uri("https://api-merchant.payos.vn/");
            client.Timeout = TimeSpan.FromSeconds(15);
        }).RemoveAllLoggers();
        services.AddScoped<ICartDealEventService, CartDealEventService>();

        return services;
    }
}
