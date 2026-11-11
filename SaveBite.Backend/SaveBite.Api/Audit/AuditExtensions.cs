using SaveBite.Backend.Audit.Archives;

namespace SaveBite.Backend.Audit;

public static class AuditExtensions
{
    public static IServiceCollection AddAuditServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IAuditContext, HttpAuditContext>();
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddScoped<ArchiveStorage>();
        services.AddScoped<AuditArchiveService>();
        return services;
    }
}
