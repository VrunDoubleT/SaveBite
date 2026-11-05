using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

// Persist requests before broker delivery. Ambient business transactions own their commit.
public class NotificationQueue : INotificationQueue
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _clock;

    public NotificationQueue(AppDbContext db, TimeProvider clock)
    {
        _db = db; _clock = clock;
    }

    // Persist in the ambient transaction; deliver after commit, or immediately for standalone requests.
    public async Task EnqueueAsync(NotificationTarget target, CreateNotificationCommand command, CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        var body = command.Body.Trim();
        // Match the notification column limit, including long moderation reasons.
        if (body.Length > 1000)
        {
            var limit = char.IsHighSurrogate(body[998]) ? 998 : 999;
            body = body[..limit] + "…";
        }
        command = command with
        {
            Body = body,
            DeduplicationKey = string.IsNullOrWhiteSpace(command.DeduplicationKey)
                ? $"notification-request:{id}"
                : command.DeduplicationKey.Trim()
        };

        var keyMaterial = JsonSerializer.Serialize(target) + ":" + command.DeduplicationKey;
        var key = "notification:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(keyMaterial)));
        var json = JsonSerializer.Serialize(new NotificationRequestedEvent(id, target, command));
        var now = _clock.GetUtcNow().UtcDateTime;
        // ON CONFLICT avoids aborting a caller's transaction during duplicate/concurrent requests.
        // Raw insert deliberately does not SaveChanges on unrelated tracked business entities.
        await _db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO event_outbox (id, order_id, event_key, routing_key, payload_json, created_at)
            VALUES ({id}, NULL, {key}, {RabbitMqRoutingKeys.NotificationRequested}, CAST({json} AS jsonb), {now})
            ON CONFLICT (event_key) DO NOTHING
            """, ct);
        var messageId = await _db.Set<SaveBite.Backend.Models.Entities.OrderEventOutbox>()
            .Where(e => e.EventKey == key && e.PublishedAt == null).Select(e => (Guid?)e.Id).SingleOrDefaultAsync(ct);
        if (messageId.HasValue) _db.TrackNotificationOutbox(messageId.Value);
        if (_db.Database.CurrentTransaction == null) await _db.FlushNotificationOutboxAsync();
    }
}
