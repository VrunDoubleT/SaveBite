namespace SaveBite.Backend.Messaging.Configuration;
public sealed class RabbitMqTopologyState
{
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Task Ready => _ready.Task;
    public void MarkReady() => _ready.TrySetResult();
}
