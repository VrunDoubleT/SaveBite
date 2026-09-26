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

        await using var scope = _scopeFactory.CreateAsyncScope();

        var emailService =
            scope.ServiceProvider
                .GetRequiredService<IEmailService>();

        await emailService.SendAsync(
            to: email.To,
            subject: email.Subject,
            body: email.Body,
            isHtml: email.IsHtml,
            cancellationToken: cancellationToken);
    }
}