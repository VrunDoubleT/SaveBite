using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DotNetEnv;

namespace SaveBite.Deployment;

public sealed class PreparationException(string message) : Exception(message);

public static class DeploymentPreparation
{
    private static readonly string[] Keys = ["POSTGRES_DB", "POSTGRES_USER", "POSTGRES_PASSWORD", "REDIS_PASSWORD",
        "RABBITMQ_USER", "RABBITMQ_PASSWORD", "MINIO_ROOT_USER", "MINIO_ROOT_PASSWORD"];

    public static string FindRoot(string start)
    {
        for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
            if (Directory.Exists(Path.Combine(directory.FullName, "SaveBite.Backend"))) return directory.FullName;
        throw new PreparationException("Cannot find the SaveBite root. Pass --root with the project directory.");
    }

    public static Dictionary<string, string> ReadEnv(string path)
    {
        try
        {
            // Compose supports escaped apostrophes in single quotes; DotNetEnv does not.
            // Mask only those literals while parsing, without interpolating their dollar signs.
            var marker = "__SAVEBITE_QUOTE_" + Guid.NewGuid().ToString("N");
            var contents = File.ReadAllText(path);
            contents = Regex.Replace(contents,
                @"(?m)^(?<prefix>\s*(?:export\s+)?[A-Za-z_][A-Za-z0-9_]*\s*=\s*)'(?<value>(?:\\'|[^'\r\n])*)'(?<suffix>[^\S\r\n]*(?:#.*)?)$",
                match => match.Groups["prefix"].Value + "'" + match.Groups["value"].Value.Replace("\\'", marker, StringComparison.Ordinal)
                    + "'" + match.Groups["suffix"].Value);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(contents));
            return Env.Load(stream, new LoadOptions(setEnvVars: false)).GroupBy(pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Last().Value.Replace(marker, "'", StringComparison.Ordinal), StringComparer.Ordinal);
        }
        // Parser errors can contain the source line, including its secret value.
        catch (Exception ex) when (ex is not IOException and not UnauthorizedAccessException)
        {
            throw new PreparationException("Unable to parse the environment file. Check quoting in " + Path.GetFileName(path) + ".");
        }
    }

    public static string Quote(string value)
    {
        if (value.Contains('\r') || value.Contains('\n'))
            throw new PreparationException("Multiline infrastructure credentials are not supported.");
        if (value.EndsWith('\\') || value.Contains("\\'", StringComparison.Ordinal))
            throw new PreparationException("A credential ends with a backslash or contains a backslash before an apostrophe; configure its Compose quoting explicitly before preparing deployment.");
        return "'" + value.Replace("'", "\\'", StringComparison.Ordinal) + "'";
    }

    // Returns false when existing credentials already match. Never prints secret values.
    public static async Task<bool> PrepareAsync(string root, bool refresh = false, CancellationToken ct = default)
    {
        root = Path.GetFullPath(root);
        var source = Path.Combine(root, "SaveBite.Backend", ".env");
        var settings = Path.Combine(root, "SaveBite.Backend", "SaveBite.Api", "appsettings.json");
        if (!File.Exists(source) || !File.Exists(settings))
            throw new PreparationException("Provide backend .env and appsettings.json first.");
        try { using var json = JsonDocument.Parse(await File.ReadAllTextAsync(settings, ct)); }
        catch (JsonException) { throw new PreparationException("Backend appsettings.json is not valid JSON."); }
        var values = ReadEnv(source);
        var missing = Keys.Where(key => !values.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value)).ToArray();
        if (missing.Length > 0) throw new PreparationException("Missing infrastructure settings: " + string.Join(", ", missing));
        var target = Path.Combine(root, ".env");
        var existing = File.Exists(target) ? ReadEnv(target) : new Dictionary<string, string>();
        if (File.Exists(target) && !refresh)
        {
            var different = Keys.Where(key => existing.GetValueOrDefault(key) != values[key]).ToArray();
            if (different.Length > 0)
                throw new PreparationException("Root .env differs for: " + string.Join(", ", different)
                    + ". Review credentials before using --refresh; existing volumes keep their passwords.");
            if (!string.IsNullOrWhiteSpace(existing.GetValueOrDefault("BOOTSTRAP_ADMIN_EMAIL"))
                && !string.IsNullOrWhiteSpace(existing.GetValueOrDefault("BOOTSTRAP_ADMIN_PASSWORD"))) return false;
        }
        foreach (var key in Keys) existing[key] = values[key];
        if (!existing.ContainsKey("HTTP_PORT")) existing["HTTP_PORT"] = "8088";
        if (string.IsNullOrWhiteSpace(existing.GetValueOrDefault("BOOTSTRAP_ADMIN_EMAIL")))
            existing["BOOTSTRAP_ADMIN_EMAIL"] = NonEmpty(values.GetValueOrDefault("BootstrapAdmin__Email")) ?? "admin@gmail.com";
        if (string.IsNullOrWhiteSpace(existing.GetValueOrDefault("BOOTSTRAP_ADMIN_PASSWORD")))
            existing["BOOTSTRAP_ADMIN_PASSWORD"] = NonEmpty(values.GetValueOrDefault("BootstrapAdmin__Password"))
                ?? Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var content = "# Private Compose credentials copied from backend .env; do not commit.\n"
            + string.Join("\n", existing.Select(pair => pair.Key + "=" + Quote(pair.Value))) + "\n";
        await AtomicFile.WriteAsync(target, content, privateFile: true, ct);
        return true;
    }

    private static string? NonEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
