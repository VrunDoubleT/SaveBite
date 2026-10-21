using System.Text.Json;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

// Obtain motorcycle travel estimates and route geometry for pickup planning.
public sealed class VietMapTravelTimeService : ITravelTimeService
{
    private readonly IHttpClientFactory _clients;
    private readonly IConfiguration _configuration;

    public VietMapTravelTimeService(
        IHttpClientFactory clients,
        IConfiguration configuration)
    {
        _clients = clients;
        _configuration = configuration;
    }

    // Request a motorcycle route and validate its travel time, distance and optional map polyline.
    public async Task<TravelEstimate> EstimateAsync(CheckoutLocation origin, double shopLatitude, double shopLongitude, CancellationToken cancellationToken = default)
    {
        var key = _configuration["Routing:VietMapApiKey"];
        if (string.IsNullOrWhiteSpace(key)) throw AppException.ServiceUnavailable("Travel-time estimation is not configured. Please try again later.");
        var source = FormattableString.Invariant($"{origin.Latitude},{origin.Longitude}");
        var destination = FormattableString.Invariant($"{shopLatitude},{shopLongitude}");
        var path = $"api/route/v4?apikey={Uri.EscapeDataString(key)}&point={Uri.EscapeDataString(source)}&point={Uri.EscapeDataString(destination)}&vehicle=motorcycle&points_encoded=true";
        try
        {
            using var response = await _clients.CreateClient("VietMapRouting").GetAsync(path, cancellationToken);
            if (!response.IsSuccessStatusCode) throw AppException.ServiceUnavailable("Could not obtain a route to this shop. Please try again.");
            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            var root = json.RootElement;
            if (!root.TryGetProperty("paths", out var paths) || paths.GetArrayLength() == 0)
                throw AppException.BadRequest("No route from your GPS location to this shop was found.");
            var route = paths[0];
            var milliseconds = route.GetProperty("time").GetDouble();
            var meters = route.GetProperty("distance").GetDouble();
            if (!double.IsFinite(milliseconds) || !double.IsFinite(meters) || milliseconds < 0 || meters < 0 || milliseconds > 86400000)
                throw AppException.ServiceUnavailable("The route provider returned an invalid travel estimate.");
            var polyline = route.TryGetProperty("points", out var points) && points.ValueKind == JsonValueKind.String
                ? points.GetString() : null;
            return new(decimal.Round((decimal)meters / 1000, 3), Math.Max(1, (int)Math.Ceiling(milliseconds / 60000)), polyline);
        }
        catch (HttpRequestException) { throw AppException.ServiceUnavailable("The route provider is temporarily unavailable. Please retry GPS estimation."); }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { throw AppException.ServiceUnavailable("Travel-time estimation timed out. Please try again."); }
        catch (KeyNotFoundException) { throw AppException.ServiceUnavailable("The route provider returned incomplete travel data."); }
        catch (InvalidOperationException) { throw AppException.ServiceUnavailable("The route provider returned invalid travel data."); }
        catch (JsonException) { throw AppException.ServiceUnavailable("The route provider returned an invalid response."); }
    }
}
