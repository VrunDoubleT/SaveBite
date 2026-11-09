namespace SaveBite.Backend.Extensions;

public static class HttpClientExtensions
{
    public static IServiceCollection AddHttpClients(
        this IServiceCollection services)
    {
        services.AddHttpClient();
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

        return services;
    }
}