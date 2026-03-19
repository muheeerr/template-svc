namespace DA.Events;

/// <summary>
/// Marker interface for domain events. Raised by entities, dispatched after SaveChanges.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}
