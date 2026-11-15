using SaveBite.Backend.BackgroundJobs.Extensions;
using Serilog;

namespace SaveBite.Backend.Extensions;

public static class ApplicationPipelineExtensions
{
    public static WebApplication UseApplicationPipeline(this WebApplication app)
    {
        if (app.Configuration.GetValue<bool>("Deployment:BehindProxy"))
            app.UseForwardedHeaders();
        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseSerilogRequestLogging();
        if (!app.Configuration.GetValue<bool>("Deployment:BehindProxy"))
            app.UseHttpsRedirection();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseBackgroundJobs();

        app.MapControllers();
        app.MapRealtime();
        app.MapGet("/api/health/live", () => Results.Text("Healthy"));
        app.MapHealthChecks("/api/health/ready");
        return app;
    }
}
