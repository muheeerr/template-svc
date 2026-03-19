using DA.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Core.Events;

/// <summary>
/// In-process domain event dispatcher that resolves handlers from DI.
/// Dispatches events sequentially to maintain ordering guarantees.
/// </summary>
public class InProcessDomainEventDispatcher : DA.Events.IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InProcessDomainEventDispatcher> _logger;

    public InProcessDomainEventDispatcher(IServiceProvider serviceProvider, ILogger<InProcessDomainEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        foreach (var domainEvent in events)
        {
            var eventType = domainEvent.GetType();
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
            var handlers = _serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                try
                {
                    var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync));
                    await (Task)method!.Invoke(handler, [domainEvent, ct])!;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error dispatching domain event {EventType}", eventType.Name);
                }
            }
        }
    }
}
