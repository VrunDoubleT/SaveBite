using SaveBite.Backend.Audit;
using SaveBite.Backend.BackgroundJobs.Extensions;
using SaveBite.Backend.Extensions;
using SaveBite.Backend.Messaging.Extensions;
using SaveBite.Backend.Payments.Webhooks;

var builder = WebApplication.CreateBuilder(args);
builder.AddEnvironmentConfiguration();
builder.AddDeploymentConfiguration();
builder.Host.AddSerilogLogging();

builder.Services.AddCheckoutServices(builder.Configuration, builder.Environment);
builder.Services.AddApi(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAccessAuthorization();
builder.Services.AddAuthServices(builder.Configuration);
builder.Services.AddRealtime();
builder.Services.AddCorsPolicy(builder.Configuration);

builder.Services.AddAuditServices();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddRedis(builder.Configuration);
builder.Services.AddBackgroundJobs(builder.Configuration);
builder.Services.AddRabbitMq(builder.Configuration);
builder.Services.AddAllRabbitMqConsumers();

builder.Services.AddRepositories();
builder.Services.AddServices();
builder.Services.AddHttpClients();
builder.Services.AddCloudinary(builder.Configuration);
builder.Services.AddEmail(builder.Configuration);
builder.Services.AddDeployment(builder.Configuration);
builder.Services.AddOptions<PayOsWebhookSyncOptions>()
    .Bind(builder.Configuration.GetSection("PayOsWebhookSync")).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddHostedService<PayOsWebhookSyncWorker>();

var app = builder.Build();
await app.InitializeDatabaseAsync();
app.UseApplicationPipeline();
app.Run();

public partial class Program { }
