using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SaveBite.Backend.Data;
using SaveBite.Backend.Messaging.Configuration;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Messaging.Models;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
namespace SaveBite.Backend.Messaging.Consumer;
public sealed class ModerationConsumer : RabbitMqConsumer<ModerationEvent>
{
    private readonly IServiceScopeFactory _scopes;
    public ModerationConsumer(IOptions<RabbitMqOptions> options, ILogger<ModerationConsumer> logger, IServiceScopeFactory scopes)
        : base(options, logger) => _scopes = scopes;
    protected override string QueueName => RabbitMqQueues.ModerationQueueName;
    protected override string? DeadLetterQueueName => RabbitMqQueues.ModerationDeadLetterQueueName;
    protected override string RoutingKey => RabbitMqRoutingKeys.ModerationAudit;
    protected override async Task HandleMessageAsync(RabbitMessage<ModerationEvent> message, CancellationToken ct)
    {
        var e = message.Data;
        if (e is null || e.Id == Guid.Empty || e.ActorUserId == Guid.Empty || e.TargetId == Guid.Empty)
            throw new ArgumentException("Invalid moderation audit event.");
        await using var scope = _scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (await db.AuditLogs.AnyAsync(x => x.Id == e.Id, ct)) return; // idempotent redelivery
        db.AuditLogs.Add(new AuditLog
        {
            Id = e.Id, ActorUserId = e.ActorUserId, ActorType = AuditActorType.User,
            Action = e.Action, TargetType = e.TargetType, TargetId = e.TargetId,
            Reason = e.Reason, OldValuesJson = e.OldValuesJson,
            NewValuesJson = e.NewValuesJson, CreatedAt = e.CreatedAt
        });
        await db.SaveChangesAsync(ct);
    }
}
