using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

public sealed class EmailDeliveryService(AppDbContext db, IEmailService smtp, TimeProvider clock,
    ILogger<EmailDeliveryService> logger, BusinessEmailQueue businessEmails)
{
    public async Task SendAsync(EmailNotificationEvent email, CancellationToken ct)
    {
        if (!await businessEmails.StillRelevantAsync(email, ct)) return;
        if (string.IsNullOrWhiteSpace(email.DeliveryKey))
        {
            await smtp.SendAsync(email.To, email.Subject, email.Body, email.IsHtml, ct);
            return;
        }
        var key = email.DeliveryKey;
        await using (var transaction = await db.Database.BeginTransactionAsync(ct))
        {
            var now = clock.GetUtcNow().UtcDateTime;
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO email_deliveries (delivery_key, status, updated_at) VALUES ({key}, 'Pending', {now}) ON CONFLICT DO NOTHING", ct);
            var row = await db.Set<EmailDelivery>().FromSqlInterpolated($"SELECT * FROM email_deliveries WHERE delivery_key = {key} FOR UPDATE").SingleAsync(ct);
            if (row.Status == "Sent") return;
            if (row.Status is "Sending" or "Uncertain")
            {
                logger.LogError("Email {DeliveryKey} has an ambiguous delivery outcome. Verify SMTP logs before allowing another send.", key);
                throw new InvalidOperationException("SMTP outcome requires administrator verification.");
            }
            row.Status = "Sending"; row.UpdatedAt = now;
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        try
        {
            await smtp.SendAsync(email.To, email.Subject, email.Body, email.IsHtml, ct);
        }
        catch (Exception error)
        {
            // A provider rejection before acceptance is safe to retry. Unknown transport failures are not.
            var status = error is EmailSendException { MayHaveBeenSent: false } ? "Pending" : "Uncertain";
            await db.Set<EmailDelivery>().Where(d => d.DeliveryKey == key).ExecuteUpdateAsync(
                s => s.SetProperty(d => d.Status, status).SetProperty(d => d.UpdatedAt, clock.GetUtcNow().UtcDateTime), CancellationToken.None);
            throw;
        }
        // Failure/crash here leaves Sending, so redelivery cannot send a duplicate email.
        await using var confirmed = await db.Database.BeginTransactionAsync(ct);
        await db.Set<EmailDelivery>().Where(d => d.DeliveryKey == key).ExecuteUpdateAsync(
            s => s.SetProperty(d => d.Status, "Sent").SetProperty(d => d.SentAt, (DateTime?)clock.GetUtcNow().UtcDateTime)
                .SetProperty(d => d.UpdatedAt, clock.GetUtcNow().UtcDateTime), ct);
        var orderId = await db.Set<OrderEventOutbox>().Where(e => e.EventKey == key).Select(e => e.OrderId).SingleOrDefaultAsync(ct);
        if (orderId.HasValue)
            await db.Orders.Where(o => o.Id == orderId).ExecuteUpdateAsync(s => s
                .SetProperty(o => o.PickupReminderEmailSentAt, (DateTime?)clock.GetUtcNow().UtcDateTime), ct);
        await confirmed.CommitAsync(ct);
    }
}
