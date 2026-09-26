using SaveBite.Backend.Messaging.Constants;

namespace SaveBite.Backend.Messaging.Configuration;

public class RabbitMqOptions
{
    public string HostName { get; set; } = string.Empty;
    public int Port { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string VirtualHost { get; set; } = "/";
    public string ExchangeName { get; set; } = RabbitMqExchanges.Main;
    public string DeadLetterExchangeName { get; set; } = RabbitMqExchanges.DeadLetter;
    public int MaxRetryCount { get; set; }
}
