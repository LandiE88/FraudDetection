using System.Collections;
using System.Reflection;
using FraudDetection.Domain.Common;

namespace FraudDetection.Application.Common.Events;

/// <summary>
/// Default <see cref="IDomainEventDispatcher"/>: for each event, resolves every
/// <see cref="IDomainEventHandler{TEvent}"/> registered for its concrete runtime type
/// and invokes them in turn. Uses reflection purely to close the generic handler
/// interface over a type only known at runtime (the event's own type) — no external
/// messaging library involved.
/// </summary>
public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public DomainEventDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var handlerInterface = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var enumerableOfHandlers = typeof(IEnumerable<>).MakeGenericType(handlerInterface);

            var handlers = (IEnumerable)(_serviceProvider.GetService(enumerableOfHandlers) ?? Array.Empty<object>());
            var handleMethod = handlerInterface.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.Handle))!;

            foreach (var handler in handlers)
            {
                var task = (Task)handleMethod.Invoke(handler, new object[] { domainEvent, cancellationToken })!;
                await task;
            }
        }
    }
}
