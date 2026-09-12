namespace FraudDetection.Domain.Common;

/// <summary>
/// Non-generic view over <see cref="AggregateRoot{TId}"/> so infrastructure code (e.g.
/// a DbContext's change tracker) can find every aggregate with pending domain events
/// regardless of its id type, without the domain layer depending on EF Core.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
