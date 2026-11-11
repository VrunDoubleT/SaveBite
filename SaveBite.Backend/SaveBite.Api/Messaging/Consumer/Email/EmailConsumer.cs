using Microsoft.Extensions.Options;
using SaveBite.Backend.Messaging.Configuration;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Messaging.Models;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Messaging.Consumer.Email;

public class EmailConsumer
    : RabbitMqConsumer<EmailNotificationEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;

    protected override string? DeadLetterQueueName => "email_dead_letter_queue";
    protected override bool ParkFailures => true;
    protected override int RetryAttempts => 3;

    protected override string QueueName => RabbitMqQueues.EmailQueueName;

    protected override string RoutingKey => RabbitMqRoutingKeys.EmailRoutingKey;

    public EmailConsumer(
        IOptions<RabbitMqOptions> options,
        ILogger<EmailConsumer> logger,
        IServiceScopeFactory scopeFactory)
        : base(options, logger)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task HandleMessageAsync(
        RabbitMessage<EmailNotificationEvent> message,
        CancellationToken cancellationToken)
    {
        var email = message.Data;
        if (email == null || string.IsNullOrWhiteSpace(email.To) || string.IsNullOrWhiteSpace(email.Subject))
            throw new ArgumentException("Email recipient and subject are required.");
        if (email.ExpiresAt <= DateTime.UtcNow) return; // Never send an expired OTP on retry/replay.

        await using var scope = _scopeFactory.CreateAsyncScope();

        if (string.IsNullOrWhiteSpace(email.DeliveryKey))
            await scope.ServiceProvider.GetRequiredService<IEmailService>()
                .SendAsync(email.To, email.Subject, email.Body, email.IsHtml, cancellationToken);
        else
            await scope.ServiceProvider.GetRequiredService<SaveBite.Backend.Services.Implementations.EmailDeliveryService>()
                .SendAsync(email, cancellationToken);
    }
}
