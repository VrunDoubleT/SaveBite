namespace SaveBite.Backend.Audit;

public interface IAuditWriter
{
    void Record(string targetType, Guid targetId, string action,
                object? oldValues = null, object? newValues = null,
                string? reason = null);
}
