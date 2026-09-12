using FraudDetection.Domain.Common;

namespace FraudDetection.Application.Common.Events;

/// <summary>
/// Reacts to a single kind of domain event. Register one implementation per event type
/// you care about — <see cref="IDomainEventDispatcher"/> resolves every registered
/// handler for the concrete event type at dispatch time.
/// </summary>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task Handle(TEvent domainEvent, CancellationToken cancellationToken = default);
}
