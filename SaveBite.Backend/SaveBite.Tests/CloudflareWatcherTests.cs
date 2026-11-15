using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using SaveBite.Deployment;
using Xunit;

namespace SaveBite.Tests;

public sealed class CloudflareWatcherTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "savebite-watcher-" + Guid.NewGuid().ToString("N"));
    private sealed class Clock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.FromUnixTimeSeconds(120);
    }

    [Theory]
    [InlineData("INF | https://bright-little-shop.trycloudflare.com |", "https://bright-little-shop.trycloudflare.com")]
    [InlineData("https://developers.cloudflare.com/tunnel/", null)]
    [InlineData("https://shop.trycloudflare.com.evil.test", null)]
    public void ExtractsOnlyQuickTunnelHostnames(string line, string? expected)
        => Assert.Equal(expected, EndpointSnapshot.ExtractUrl(line));

    [Theory]
    [InlineData(true, 100, true)]
    [InlineData(true, 75, false)]
    [InlineData(true, 140, false)]
    [InlineData(false, 100, false)]
    public void HealthRequiresReadinessAndRecentHeartbeat(bool ready, long updatedAt, bool expected)
        => Assert.Equal(expected, new EndpointSnapshot("https://bright-shop.trycloudflare.com", ready, "session", updatedAt).IsHealthy(new Clock()));

    [Fact]
    public async Task AtomicReplacementInvalidatesPreviousSessionAndPreservesApiContract()
    {
        var path = Path.Combine(_directory, "endpoint.json");
        var store = new EndpointStore(path, new Clock());
        Assert.Null(await store.ReadAsync());
        await store.WriteAsync("https://old-shop.trycloudflare.com", true, "old");
        await store.WriteAsync("", false, "new");
        var state = await store.ReadAsync();
        Assert.Equal("new", state!.Session);
        Assert.False(state.IsHealthy(new Clock()));
        using var json = JsonDocument.Parse(await File.ReadAllTextAsync(path));
        Assert.Equal(120, json.RootElement.GetProperty("UpdatedAtUnix").GetInt64());
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(6, 60)]
    [InlineData(100, 60)]
    public void BackoffGrowsAndRemainsBounded(int attempt, double seconds)
        => Assert.Equal(TimeSpan.FromSeconds(seconds + 0.5), TunnelSettings.RetryDelay(attempt, 0.5));

    [Fact]
    public async Task CancellationStopsChildProcessAndInvalidatesSnapshot()
    {
        if (!OperatingSystem.IsLinux()) return;
        Directory.CreateDirectory(_directory);
        var script = Path.Combine(_directory, "fake-cloudflared");
        var pidFile = Path.Combine(_directory, "pid");
        await File.WriteAllTextAsync(script, $"#!/bin/sh\necho $$ > '{pidFile}'\necho 'https://fake-shop.trycloudflare.com' >&2\nexec sleep 60\n");
        File.SetUnixFileMode(script, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        var settings = new TunnelSettings { Executable = script, StatePath = Path.Combine(_directory, "state.json"), PollInterval = TimeSpan.FromMilliseconds(20) };
        var store = new EndpointStore(settings.StatePath, TimeProvider.System);
        using var http = new HttpClient(new HealthyHandler());
        using var watcher = new CloudflareWatcher(settings, store, http, TimeProvider.System, NullLogger<CloudflareWatcher>.Instance);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await watcher.StartAsync(timeout.Token);
        try
        {
            while ((await store.ReadAsync(timeout.Token))?.Ready != true)
            {
                if (watcher.ExecuteTask!.IsCompleted) await watcher.ExecuteTask;
                await Task.Delay(20, timeout.Token);
            }
            Assert.Equal("https://fake-shop.trycloudflare.com", (await store.ReadAsync())!.Url);
        }
        finally { await watcher.StopAsync(CancellationToken.None); }
        Assert.False((await store.ReadAsync())!.Ready);
        var pid = int.Parse(await File.ReadAllTextAsync(pidFile));
        Assert.False(Directory.Exists($"/proc/{pid}"));
    }

    private sealed class HealthyHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("Healthy") });
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }
}
