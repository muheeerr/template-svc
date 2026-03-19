using DA.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DA.Auditing;

public class DbAuditLogger : IAuditLogger
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<DbAuditLogger> _logger;

    public DbAuditLogger(AppDbContext db, IHttpContextAccessor httpContextAccessor, ILogger<DbAuditLogger> logger)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogAsync(
        string entityType,
        Guid entityId,
        string action,
        string? oldValues,
        string? newValues,
        CancellationToken ct = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var changedBy = _db.GetUserName();

        var log = new AuditLog
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            OldValues = oldValues,
            NewValues = newValues,
            IpAddress = ipAddress,
            ChangedBy = changedBy
        };

        await _db.Set<AuditLog>().AddAsync(log, ct);
        _logger.LogInformation("Audit: {Action} on {EntityType} {EntityId} by {ChangedBy}", action, entityType, entityId, changedBy);
    }
}
