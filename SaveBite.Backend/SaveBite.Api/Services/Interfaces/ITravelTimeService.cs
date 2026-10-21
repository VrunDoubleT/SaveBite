using SaveBite.Backend.Models.Requests;

namespace SaveBite.Backend.Services.Interfaces;

// Carry route distance, rounded travel time and optional encoded map geometry.
public sealed record TravelEstimate(decimal DistanceKm, int EtaMinutes, string? EncodedPolyline = null);
// Estimate travel from customer GPS coordinates to the selected shop.
public interface ITravelTimeService
{
    Task<TravelEstimate> EstimateAsync(CheckoutLocation origin, double shopLatitude, double shopLongitude, CancellationToken cancellationToken = default);
}
