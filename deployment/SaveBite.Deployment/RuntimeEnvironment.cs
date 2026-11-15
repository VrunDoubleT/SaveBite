using System.Text;
using System.Text.RegularExpressions;

namespace SaveBite.Deployment;

internal static partial class RuntimeEnvironment
{
    private const string RuntimeEnvironmentPath = "/run/savebite/runtime.env";

    public static async Task PrepareAsync()
    {
        const string rootEnvironment = "/opt/savebite/private/root.env";
        const string backendEnvironment = "/opt/savebite/private/backend.env";
        var values = DeploymentPreparation.ReadEnv(backendEnvironment);
        foreach (var (key, value) in DeploymentPreparation.ReadEnv(rootEnvironment))
            values[key] = value;
        foreach (System.Collections.DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            var key = (string)entry.Key;
            if (values.ContainsKey(key) || key.Contains("__", StringComparison.Ordinal)) values[key] = (string)entry.Value!;
        }

        var required = new[]
        {
            "POSTGRES_DB", "POSTGRES_USER", "POSTGRES_PASSWORD", "REDIS_PASSWORD",
            "RABBITMQ_USER", "RABBITMQ_PASSWORD", "MINIO_ROOT_USER", "MINIO_ROOT_PASSWORD",
            "BOOTSTRAP_ADMIN_EMAIL", "BOOTSTRAP_ADMIN_PASSWORD"
        };
        var missing = required.Where(key => !values.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value));
        if (missing.Any())
            throw new PreparationException("Image configuration is missing: " + string.Join(", ", missing));

        var overrides = new Dictionary<string, string>
        {
            ["ASPNETCORE_ENVIRONMENT"] = "Production",
            ["ASPNETCORE_URLS"] = "http://127.0.0.1:8080",
            ["Deployment__Containerized"] = "true",
            ["Deployment__SingleContainer"] = "true",
            ["Deployment__BehindProxy"] = "true",
            ["Deployment__DataProtectionPath"] = "/var/lib/savebite/keys",
            ["Deployment__PublicEndpointFile"] = TunnelSettings.DefaultStatePath,
            ["Deployment__PaymentCreationEnabled"] = Environment.GetEnvironmentVariable("Deployment__PaymentCreationEnabled")
                ?? values.GetValueOrDefault("Deployment__PaymentCreationEnabled") ?? "true",
            ["TUNNEL_ORIGIN"] = TunnelSettings.DefaultOrigin,
            ["PUBLIC_ENDPOINT_FILE"] = TunnelSettings.DefaultStatePath,
            ["RabbitMQ__HostName"] = "127.0.0.1",
            ["RabbitMQ__Port"] = "5672",
            ["RabbitMQ__UserName"] = values["RABBITMQ_USER"],
            ["RabbitMQ__Password"] = values["RABBITMQ_PASSWORD"],
            ["RABBITMQ_NODENAME"] = "rabbit@localhost",
            ["AuditArchive__Endpoint"] = "http://127.0.0.1:9000",
            ["AuditArchive__AccessKey"] = values["MINIO_ROOT_USER"],
            ["AuditArchive__SecretKey"] = values["MINIO_ROOT_PASSWORD"],
            ["BootstrapAdmin__Email"] = values["BOOTSTRAP_ADMIN_EMAIL"],
            ["BootstrapAdmin__Password"] = values["BOOTSTRAP_ADMIN_PASSWORD"],
            ["Checkout__PaymentProvider"] = "PayOS",
            ["Hangfire__DashboardEnabled"] = "false",
            ["Checkout__BackendBaseUrl"] = "http://127.0.0.1:8080",
            ["Checkout__FrontendBaseUrl"] = "http://127.0.0.1:80",
            ["SAVEBITE_ENABLE_CLOUDFLARE"] = Environment.GetEnvironmentVariable("SAVEBITE_ENABLE_CLOUDFLARE")
                ?? values.GetValueOrDefault("SAVEBITE_ENABLE_CLOUDFLARE") ?? "true"
        };
        foreach (var (key, value) in overrides) values[key] = value;

        var content = new StringBuilder();
        foreach (var (key, value) in values.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (!ValidKeyRegex().IsMatch(key) || value.Contains('\r') || value.Contains('\n'))
                throw new PreparationException("Image configuration contains an unsupported environment value.");
            content.Append("export ").Append(key).Append('=').Append(Quote(value)).Append('\n');
        }

        await AtomicFile.WriteAsync(RuntimeEnvironmentPath, content.ToString(), privateFile: true, CancellationToken.None);
        // Never allow a fresh container to trust a snapshot left by a hard-killed tunnel.
        await new EndpointStore(values["PUBLIC_ENDPOINT_FILE"], TimeProvider.System)
            .WriteAsync("", false, Guid.NewGuid().ToString());
    }

    private static string Quote(string value) => "'" + value.Replace("'", "'\\''", StringComparison.Ordinal) + "'";

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex ValidKeyRegex();
}
