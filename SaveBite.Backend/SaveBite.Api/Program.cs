using SaveBite.Backend.Data;
using SaveBite.Backend.Data.Seed;
using SaveBite.Backend.Extensions;
using SaveBite.Backend.Messaging.Extensions;
using SaveBite.Backend.Middlewares;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddEnvironmentConfiguration();

builder.Services.AddFluentValidation();

builder.Services.AddApi(builder.Configuration);
builder.Services.AddApiBehavior();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAccessAuthorization();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddDatabase(builder.Configuration);

builder.Services.AddRabbitMq(builder.Configuration);
builder.Services.AddAllRabbitMqConsumers();

builder.Services.AddRedis(builder.Configuration);
builder.Services.AddAuthServices(builder.Configuration);
builder.Services.AddShopStaffServices();

builder.Services.AddRepositories();
builder.Services.AddServices();
builder.Services.AddCloudinary(builder.Configuration);

builder.Services.AddEmail(builder.Configuration);

builder.Host.AddSerilogLogging();

builder.Services.AddCorsPolicy(builder.Configuration);

builder.Services.AddHttpClient();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    await DbSeeder.SeedAsync(context);
}

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

app.MapControllers();

app.Run();
