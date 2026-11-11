namespace SaveBite.Backend.Services.Implementations;

public sealed class EmailSendException(string message, bool mayHaveBeenSent, Exception inner) : Exception(message, inner)
{
    public bool MayHaveBeenSent { get; } = mayHaveBeenSent;
}
