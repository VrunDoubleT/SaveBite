using Microsoft.Extensions.Options;
using SaveBite.Backend.Messaging.Configuration;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Messaging.Models;
using SaveBite.Backend.Services.Implementations;
namespace SaveBite.Backend.Messaging.Consumer;
public sealed class PlatformPayOsVerifiedConsumer(IOptions<RabbitMqOptions> options,
    ILogger<PlatformPayOsVerifiedConsumer> logger, IServiceScopeFactory scopes)
    : RabbitMqConsumer<PlatformPayOsVerifiedEvent>(options, logger)
{
    protected override string QueueName => RabbitMqQueues.PlatformPayOsVerifiedQueueName;
    protected override string? DeadLetterQueueName => RabbitMqQueues.PlatformPayOsVerifiedDeadLetterQueueName;
    protected override string RoutingKey => RabbitMqRoutingKeys.PlatformPayOsVerified;
    protected override async Task HandleMessageAsync(RabbitMessage<PlatformPayOsVerifiedEvent> message, CancellationToken ct)
    {
        if (message.Data == null || message.Data.Revision <= 0) throw new ArgumentException("Invalid payOS revision.");
        await using var scope = scopes.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<PlatformFeePaymentAvailability>().ResumeAsync(message.Data.Revision, ct);
    }
}
