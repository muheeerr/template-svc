namespace DA.Events;

/// <summary>
/// Dispatches domain events. Implemented in Core layer, resolved via DI in AppDbContext.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default);
}
