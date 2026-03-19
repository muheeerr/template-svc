using DA.Entities;

namespace DA.Auditing;

public class AuditLog : Entity
{
    public string EntityType { get; set; } = null!;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = null!;  // CREATE, UPDATE, DELETE
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string IpAddress { get; set; } = null!;
    public string ChangedBy { get; set; } = null!;
}
