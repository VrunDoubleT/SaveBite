using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Extensions;

public static class CheckoutExtensions
{
    public static IServiceCollection AddCheckoutServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var paymentProvider = configuration["Checkout:PaymentProvider"] ?? "PayOS";
        if (paymentProvider is not ("PayOS" or "Simulated"))
            throw new InvalidOperationException("Checkout:PaymentProvider must be PayOS or Simulated.");
        if (paymentProvider == "Simulated" && !environment.IsDevelopment())
            throw new InvalidOperationException("Simulated payments are allowed only in Development.");

        services.AddScoped<ICheckoutService, CheckoutService>();
        services.AddScoped<IShopOrderService, ShopOrderService>();
        services.AddScoped<ICheckoutPaymentService, CheckoutPaymentService>();
        services.AddScoped<ICheckoutPaymentGateway>(provider => paymentProvider == "Simulated"
            ? new DevelopmentCheckoutGateway()
            : ActivatorUtilities.CreateInstance<PayOsCheckoutGateway>(provider));

        return services;
    }
}
