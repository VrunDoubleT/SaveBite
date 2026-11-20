using SaveBite.Backend.Audit;
using SaveBite.Backend.BackgroundJobs.Extensions;
using SaveBite.Backend.Extensions;
using SaveBite.Backend.Messaging.Extensions;
using SaveBite.Backend.Payments.Webhooks;

var apiRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../SaveBite.Api"));
var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args, ContentRootPath = apiRoot, ApplicationName = typeof(SaveBite.Backend.Data.AppDbContext).Assembly.GetName().Name, EnvironmentName = "Development" });
builder.AddEnvironmentConfiguration();
var postgres = new Npgsql.NpgsqlConnectionStringBuilder(builder.Configuration.GetConnectionString("Postgres")) { Database = "savebite_uc050" };
var redis = StackExchange.Redis.ConfigurationOptions.Parse(builder.Configuration.GetConnectionString("Redis")!);
redis.DefaultDatabase = 8;
builder.Configuration.AddInMemoryCollection(new Dictionary<string,string?> {
 ["ConnectionStrings:Postgres"] = postgres.ConnectionString,
 ["ConnectionStrings:Hangfire"] = postgres.ConnectionString,
 ["ConnectionStrings:Redis"] = redis.ToString(includePassword: true),
 ["RabbitMQ:VirtualHost"] = "uc050",
 ["Email:Host"] = "127.0.0.1", ["Email:Port"] = "1025", ["Email:Username"] = "demo", ["Email:Password"] = "demo", ["Email:UseSsl"] = "false", ["Email:FromEmail"] = "demo@savebite.test",
 ["Checkout:PaymentProvider"] = "Simulated", ["Deployment:BehindProxy"] = "true"
});
builder.WebHost.ConfigureKestrel(options => { options.ListenLocalhost(8085); options.ListenLocalhost(8086, listener => listener.UseHttps("/tmp/savebite-uc050.pfx", "uc050-demo")); });
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
builder.Services.Configure<SaveBite.Backend.Configurations.CloudinarySettings>(builder.Configuration.GetSection("Cloudinary"));
builder.Services.AddSingleton<SaveBite.Backend.Services.Interfaces.ICloudinaryService, LocalDemoUploads>();
builder.Services.AddScoped<SaveBite.Backend.Services.Interfaces.ITravelTimeService, LocalDemoTravel>();
builder.Services.AddScoped<SaveBite.Backend.Services.Interfaces.IPayOsCredentialsValidator, LocalDemoPayOsValidator>();
builder.Services.AddEmail(builder.Configuration);
builder.Services.AddDeployment(builder.Configuration);
builder.Services.AddOptions<PayOsWebhookSyncOptions>()
    .Bind(builder.Configuration.GetSection("PayOsWebhookSync")).ValidateDataAnnotations().ValidateOnStart();
// Payment provider callbacks are disabled in this isolated documentation host.

var app = builder.Build();
await app.InitializeDatabaseAsync();
app.UseApplicationPipeline();
app.MapGet("/api/demo-environment", () => new { database = "savebite_uc050", redisDatabase = 8, rabbitMqVirtualHost = "uc050", email = "local", uploads = "local", payments = "simulated", travel = "deterministic-demo" });
app.MapGet("/api/demo-assets/{name}", (string name) => Results.File(Path.Combine(LocalDemoUploads.Root, Path.GetFileName(name)), "image/png"));
Console.WriteLine("DOCUMENTATION ONLY: database=savebite_uc050; redis=8; rabbitmq=uc050; uploads=local; payments=simulated; route=deterministic demo estimate");
app.Run();

public partial class Program { }
