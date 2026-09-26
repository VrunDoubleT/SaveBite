namespace SaveBite.Backend.Extensions;

public static class HttpClientExtensions
{
    public static IServiceCollection AddHttpClients(
        this IServiceCollection services)
    {
        services.AddHttpClient();

        return services;
    }
}