namespace SaveBite.Backend.Messaging.Models;

public class RabbitMessage<T>
{
    public Guid MessageId { get; set; } = Guid.NewGuid();

    public string EventType { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public T Data { get; set; } = default!;
}