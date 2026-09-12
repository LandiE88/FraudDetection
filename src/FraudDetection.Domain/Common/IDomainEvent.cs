namespace FraudDetection.Domain.Common;

/// <summary>
/// Marker for something meaningful that happened inside the domain. Kept free of any
/// messaging-library dependency so the domain layer has zero infrastructure coupling;
/// the application layer's <c>IDomainEventDispatcher</c> is responsible for routing
/// these to whatever <c>IDomainEventHandler&lt;TEvent&gt;</c> implementations care about them.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}
