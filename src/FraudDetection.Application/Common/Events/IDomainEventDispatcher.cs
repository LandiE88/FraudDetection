using FraudDetection.Domain.Common;

namespace FraudDetection.Application.Common.Events;

/// <summary>
/// Delivers domain events raised by aggregates to their registered
/// <see cref="IDomainEventHandler{TEvent}"/> implementations. Called by the
/// infrastructure layer after a unit of work has committed successfully.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
