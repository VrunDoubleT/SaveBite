using SaveBite.Backend.BackgroundJobs.Extensions;
using Serilog;

namespace SaveBite.Backend.Extensions;

public static class ApplicationPipelineExtensions
{
    public static WebApplication UseApplicationPipeline(this WebApplication app)
    {
        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseSerilogRequestLogging();
        app.UseHttpsRedirection();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseBackgroundJobs();

        app.MapControllers();
        app.MapRealtime();
        return app;
    }
}
