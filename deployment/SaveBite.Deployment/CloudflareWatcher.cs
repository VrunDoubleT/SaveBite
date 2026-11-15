using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SaveBite.Deployment;

public sealed class CloudflareWatcher(TunnelSettings settings, EndpointStore store, HttpClient http,
    TimeProvider clock, ILogger<CloudflareWatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var attempt = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            var session = Guid.NewGuid().ToString();
            var started = clock.GetTimestamp();
            using var process = new Process { StartInfo = CreateStartInfo() };
            using var reading = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            Task[] readers = [];
            var launched = false;
            try
            {
                await store.WriteAsync("", false, session, stoppingToken);
                launched = process.Start();
                if (!launched) throw new InvalidOperationException("Unable to launch tunnel process.");
                var urls = Channel.CreateBounded<string>(new BoundedChannelOptions(64)
                {
                    FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true, SingleWriter = false
                });
                readers = [ReadUrlsAsync(process.StandardOutput, urls.Writer, reading.Token),
                    ReadUrlsAsync(process.StandardError, urls.Writer, reading.Token)];
                await MonitorAsync(process, urls.Reader, session, stoppingToken);
                if (process.HasExited)
                    logger.LogWarning("Cloudflared exited with code {ExitCode}; session {Session}.", process.ExitCode, session);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) when (ex is IOException or Win32Exception or InvalidOperationException)
            {
                // Do not log process output, environment variables or provider response bodies.
                logger.LogError("Tunnel supervisor failed: {ErrorType}; session {Session}.", ex.GetType().Name, session);
            }
            finally
            {
                try { await store.WriteAsync("", false, session); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    logger.LogError("Unable to invalidate tunnel snapshot: {ErrorType}.", ex.GetType().Name);
                }
                await reading.CancelAsync();
                if (launched) await StopProcessAsync(process);
                try { await Task.WhenAll(readers).WaitAsync(TimeSpan.FromSeconds(2)); }
                catch (TimeoutException) { logger.LogWarning("Tunnel log readers did not stop promptly."); }
            }
            if (clock.GetElapsedTime(started) > TimeSpan.FromMinutes(5)) attempt = 0;
            var delay = TunnelSettings.RetryDelay(attempt, Random.Shared.NextDouble());
            attempt = Math.Min(attempt + 1, 6);
            logger.LogInformation("Restarting Quick Tunnel in {DelaySeconds:F1}s.", delay.TotalSeconds);
            try { await Task.Delay(delay, clock, stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }

    private ProcessStartInfo CreateStartInfo()
    {
        var info = new ProcessStartInfo(settings.Executable)
        {
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true
        };
        foreach (var argument in new[] { "tunnel", "--no-autoupdate", "--url", settings.Origin,
            "--metrics", "127.0.0.1:2000", "--loglevel", "info", "--grace-period", "5s" })
            info.ArgumentList.Add(argument);
        return info;
    }

    private static async Task ReadUrlsAsync(StreamReader stream, ChannelWriter<string> urls, CancellationToken ct)
    {
        try
        {
            while (await stream.ReadLineAsync(ct) is { } line)
                if (EndpointSnapshot.ExtractUrl(line) is { } url) urls.TryWrite(url);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException) { }
    }

    private async Task MonitorAsync(Process process, ChannelReader<string> urls, string session, CancellationToken ct)
    {
        var url = "";
        bool? lastReady = null;
        var unhealthySince = clock.GetTimestamp();
        while (!ct.IsCancellationRequested && !process.HasExited)
        {
            while (urls.TryRead(out var candidate))
            {
                if (candidate == url) continue;
                url = candidate;
                await store.WriteAsync(url, false, session, ct);
                logger.LogInformation("Quick Tunnel URL discovered: {Url}; session {Session}.", url, session);
            }
            var ready = url.Length > 0 && await ProbeAsync("http://127.0.0.1:2000/ready", null, ct)
                && await ProbeAsync(url + "/api/health/ready", "Healthy", ct) && !process.HasExited;
            await store.WriteAsync(url, ready, session, ct);
            if (ready != lastReady)
            {
                logger.LogInformation("Quick Tunnel ready={Ready}; session {Session}.", ready, session);
                lastReady = ready;
            }
            if (ready) unhealthySince = clock.GetTimestamp();
            else if (clock.GetElapsedTime(unhealthySince) > settings.UnhealthyTimeout)
            {
                logger.LogWarning("Tunnel unavailable for {Seconds}s; requesting a new tunnel.", settings.UnhealthyTimeout.TotalSeconds);
                return;
            }
            await Task.Delay(settings.PollInterval, clock, ct);
        }
    }

    private async Task<bool> ProbeAsync(string url, string? expectedBody, CancellationToken ct)
    {
        try
        {
            using var response = await http.GetAsync(url, ct);
            return response.StatusCode == System.Net.HttpStatusCode.OK
                && (expectedBody is null || (await response.Content.ReadAsStringAsync(ct)).Trim() == expectedBody);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return false; }
        catch (Exception ex) when (ex is HttpRequestException or IOException) { return false; }
    }

    private async Task StopProcessAsync(Process process)
    {
        if (process.HasExited) return;
        try
        {
            if (OperatingSystem.IsLinux()) SendSignal(process.Id, 15);
            else process.Kill(entireProcessTree: true);
            using var timeout = new CancellationTokenSource(settings.ShutdownTimeout);
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException)
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or TimeoutException)
        {
            logger.LogWarning("Tunnel shutdown failed: {ErrorType}.", ex.GetType().Name);
        }
    }

    [DllImport("libc", EntryPoint = "kill", SetLastError = true)]
    private static extern int SendSignal(int processId, int signal);
}
