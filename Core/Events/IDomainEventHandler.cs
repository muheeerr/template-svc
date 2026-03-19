using DA.Events;

namespace Core.Events;

/// <summary>
/// Handles a specific type of domain event.
/// Implement this interface and register in DI to receive events after SaveChanges.
/// </summary>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken ct = default);
}
