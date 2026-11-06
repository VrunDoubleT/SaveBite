using Microsoft.AspNetCore.DataProtection;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddServices(
        this IServiceCollection services)
    {
        services.AddDataProtection().SetApplicationName("SaveBite");
        services.AddScoped<OrderConfirmationScheduling>();
        services.AddScoped<OrderResaleService>();
        services.AddScoped<RefundEventEffects>();
        services.AddScoped<NotificationOutboxDelivery>();
        services.AddScoped<DealInventoryService>();
        services.AddScoped<IOrderInventorySynchronizer, OrderInventorySynchronizer>();
        services.AddScoped<IShopFollowService, ShopFollowService>();
        services.AddScoped<ShopDealNotificationService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IRefundService, RefundService>();
        services.AddScoped<IRefundImageService, RefundImageService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserAccessService, UserAccessService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<EmailDeliveryService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IShopApplicationService, ShopApplicationService>();
        services.AddScoped<IShopStaffService, ShopStaffService>();
        services.AddScoped<IStaffInvitationService, StaffInvitationService>();
        services.AddScoped<StaffInvitationNotificationService>();
        services.AddScoped<IUserAddressService, UserAddressService>();

        services.AddScoped<IShopService, ShopService>();
        services.AddScoped<IShopSuspensionCleanupService, ShopSuspensionCleanupService>();
        services.AddScoped<IShopStatusEventService, ShopStatusEventService>();
        services.AddScoped<IFlashDealService, FlashDealService>();
        services.AddScoped<ITrustScoreService, TrustScoreService>();
        services.AddScoped<ITrustOrderService, TrustOrderService>();
        services.AddScoped<ITrustNotificationService, TrustNotificationService>();
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
        services.AddScoped<INotificationQueue, NotificationQueue>();

        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IAdminAuditWriter, AdminAuditWriter>();
        services.AddScoped<IStoreAnalyticsService, StoreAnalyticsService>();
        services.AddScoped<IPlatformAnalyticsService, PlatformAnalyticsService>();

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
        services.AddScoped<IPlatformFeeConfigService, PlatformFeeConfigService>();
        services.AddScoped<IPlatformFeeStatementService, PlatformFeeStatementService>();
        services.AddScoped<IPlatformPayOsConfigService, PlatformPayOsConfigService>();
        
        services.AddScoped<IWarningService, WarningService>();
        services.AddScoped<IFeedbackService, FeedbackService>();

        return services;
    }
}
