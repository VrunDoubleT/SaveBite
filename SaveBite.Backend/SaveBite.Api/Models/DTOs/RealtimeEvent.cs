namespace SaveBite.Backend.Models.DTOs;

// Version event contracts independently of the hub and include an ID for client-side deduplication.
public sealed record RealtimeEvent(Guid EventId, string Type, int Version, DateTime OccurredAt, object Data, string? CorrelationId = null);
// Tell cart subscribers why their authoritative cart state needs refreshing.
public sealed record CartChangedEvent(string Reason);
