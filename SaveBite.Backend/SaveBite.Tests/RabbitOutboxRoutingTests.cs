using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SaveBite.Backend.Messaging.Configuration;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Messaging.Publisher;
using Xunit;

namespace SaveBite.Tests;

public sealed class RabbitAuditFactAttribute : FactAttribute
{
    public RabbitAuditFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("SAVEBITE_AUDIT_RABBIT") != "1")
            Skip = "Opt in with SAVEBITE_AUDIT_RABBIT=1 for the dedicated localhost:5679 audit broker.";
    }
}

public class RabbitOutboxRoutingTests
{
    [RabbitAuditFact]
    public async Task TrustReconcileMustBeRoutedBeforePublisherConfirmsSuccess()
    {
        var factory = new ConnectionFactory { HostName = "127.0.0.1", Port = 5679, UserName = "audit", Password = "audit-only-password" };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        var exchange = "savebite.audit." + Guid.NewGuid().ToString("N");
        var queue = exchange + ".trust";
        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true);
        try
        {
            var publisher = new RabbitMqPublisher(connection, Options.Create(new RabbitMqOptions { ExchangeName = exchange }));
            // This used to return success because mandatory was missing for trust.level.reconcile.
            await Assert.ThrowsAnyAsync<Exception>(() => publisher.PublishAsync(RabbitMqRoutingKeys.TrustLevelReconcile,
                new { PolicyVersion = 1 }));
            await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueBindAsync(queue, exchange, RabbitMqRoutingKeys.TrustLevelReconcile);
            await publisher.PublishAsync(RabbitMqRoutingKeys.TrustLevelReconcile, new { PolicyVersion = 1 });
            var delivered = await channel.BasicGetAsync(queue, autoAck: true);
            Assert.NotNull(delivered);
            Assert.True(delivered.BasicProperties.Persistent);
            Assert.Equal(RabbitMqRoutingKeys.TrustLevelReconcile, delivered.RoutingKey);
        }
        finally
        {
            await channel.QueueDeleteAsync(queue);
            await channel.ExchangeDeleteAsync(exchange);
        }
    }
}
