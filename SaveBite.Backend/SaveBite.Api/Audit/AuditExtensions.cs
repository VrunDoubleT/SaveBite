using Microsoft.Extensions.DependencyInjection;

namespace SaveBite.Backend.Audit;

public static class AuditExtensions
{
    public static IServiceCollection AddAuditServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IAuditContext, HttpAuditContext>();
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<AuditSaveChangesInterceptor>();
        return services;
    }
}
