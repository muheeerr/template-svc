namespace DA.Auditing;

public interface IAuditLogger
{
    Task LogAsync(
        string entityType,
        Guid entityId,
        string action,
        string? oldValues,
        string? newValues,
        CancellationToken ct = default);
}
