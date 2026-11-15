using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using SaveBite.Backend.Extensions;
using Xunit;

namespace SaveBite.Tests;

public sealed class EnvironmentConfigurationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "savebite-config-" + Guid.NewGuid());

    private WebApplicationBuilder CreateBuilder(Dictionary<string, string?> settings)
    {
        var root = Path.Combine(_directory, "api");
        Directory.CreateDirectory(root);
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = root });
        builder.Configuration.AddInMemoryCollection(settings);
        return builder;
    }

    [Fact]
    public void JsonAndEnvironmentConfigurationDoNotRequireDotEnvFile()
    {
        var builder = CreateBuilder(new() { ["API_PORT"] = "", ["urls"] = "" });
        builder.AddEnvironmentConfiguration();
    }

    [Fact]
    public void DotEnvDoesNotOverwriteHostEnvironment()
    {
        var key = "SAVEBITE_CONFIG_TEST_" + Guid.NewGuid().ToString("N");
        var builder = CreateBuilder(new() { ["API_PORT"] = "" });
        File.WriteAllText(Path.Combine(_directory, ".env"), key + "='file-value'\n");
        Environment.SetEnvironmentVariable(key, "host-value");
        try
        {
            builder.AddEnvironmentConfiguration();
            Assert.Equal("host-value", builder.Configuration[key]);
        }
        finally { Environment.SetEnvironmentVariable(key, null); }
    }

    [Fact]
    public void ApiPortBindsAllContainerInterfaces()
    {
        var builder = CreateBuilder(new()
        {
            ["API_PORT"] = "8088", ["ASPNETCORE_URLS"] = "", ["DOTNET_URLS"] = "", ["urls"] = ""
        });
        builder.AddEnvironmentConfiguration();
        Assert.Equal("http://0.0.0.0:8088", builder.WebHost.GetSetting(WebHostDefaults.ServerUrlsKey));
    }

    [Fact]
    public void ExplicitUrlsTakePrecedenceOverApiPort()
    {
        var builder = CreateBuilder(new() { ["API_PORT"] = "8088", ["urls"] = "http://0.0.0.0:9099" });
        builder.AddEnvironmentConfiguration();
        Assert.Equal("http://0.0.0.0:9099", builder.WebHost.GetSetting(WebHostDefaults.ServerUrlsKey));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }
}
