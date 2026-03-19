namespace DA.Entities;

/// <summary>
/// Interface for auditable entity properties.
/// Implemented by Entity base class to enable typed auditing without reflection.
/// </summary>
public interface IAuditable
{
    DateTimeOffset CreatedAt { get; set; }
    string CreatedBy { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
    string? UpdatedBy { get; set; }
}
