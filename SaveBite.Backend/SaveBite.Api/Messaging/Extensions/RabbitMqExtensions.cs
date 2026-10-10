using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SaveBite.Backend.Messaging.Configuration;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Messaging.Publisher;

namespace SaveBite.Backend.Messaging.Extensions;

public static class RabbitMqExtensions
{
    public static IServiceCollection AddRabbitMq(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind RabbitMQ configuration to IOptions<RabbitMqOptions>
        services.Configure<RabbitMqOptions>(
            configuration.GetSection("RabbitMQ"));

        // Register a singleton RabbitMQ connection.
        services.AddSingleton<IConnection>(sp =>
        {
            var options = sp
                .GetRequiredService<IOptions<RabbitMqOptions>>()
                .Value;

            var factory = new ConnectionFactory
            {
                HostName = options.HostName,
                Port = options.Port,
                UserName = options.UserName,
                Password = options.Password,
                VirtualHost = options.VirtualHost,

                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true
            };

            try
            {
                Console.WriteLine(
                    $"[RabbitMQ] Connecting to {options.HostName}:{options.Port}");

                var connection = factory
                    .CreateConnectionAsync()
                    .GetAwaiter()
                    .GetResult();

                Console.WriteLine(
                    "[RabbitMQ] Connection SUCCESS");

                return connection;
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "[RabbitMQ] Connection FAILED");

                Console.WriteLine(ex);

                throw;
            }
        });

        // Initialize exchange, queues and bindings.
        services.AddHostedService<RabbitMqTopologyInitializer>();

        // Register publisher.
        services.AddSingleton<
            IRabbitMqPublisher,
            RabbitMqPublisher>();

        return services;
    }
}
