using Microsoft.Extensions.Options;
using SaveBite.Backend.Messaging.Configuration;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Messaging.Models;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Messaging.Consumer.Users;

public sealed class UserVerifiedConsumer : RabbitMqConsumer<UserVerifiedEvent>
{
    private readonly IServiceScopeFactory _scopes;

    public UserVerifiedConsumer(IOptions<RabbitMqOptions> options,
        ILogger<UserVerifiedConsumer> logger, IServiceScopeFactory scopes) : base(options, logger)
    {
        _scopes = scopes;
    }

    protected override string QueueName => RabbitMqQueues.UserTrustQueueName;
    protected override string? DeadLetterQueueName => RabbitMqQueues.UserTrustDeadLetterQueueName;
    protected override string RoutingKey => RabbitMqRoutingKeys.UserVerified;

    // Verify persisted account data and retry transient failures before sending to the dead-letter queue.
    protected override async Task HandleMessageAsync(RabbitMessage<UserVerifiedEvent> message, CancellationToken cancellationToken)
    {
        if (message.EventType != RoutingKey || message.Data == null || message.Data.UserId == Guid.Empty)
            throw new ArgumentException("A valid verified-account event is required.");

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await using var scope = _scopes.CreateAsyncScope();
                var user = await scope.ServiceProvider.GetRequiredService<IAuthRepository>()
                    .GetUserByIdAsync(message.Data.UserId, cancellationToken);
                if (user?.EmailVerifiedAt == null)
                    throw new InvalidOperationException("The verified account was not found.");

                await scope.ServiceProvider.GetRequiredService<ITrustScoreService>()
                    .InitializeAsync(user.Id, cancellationToken);
                return;
            }
            catch (Exception) when (attempt < 3 && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1 << attempt), cancellationToken);
            }
        }
    }
}
