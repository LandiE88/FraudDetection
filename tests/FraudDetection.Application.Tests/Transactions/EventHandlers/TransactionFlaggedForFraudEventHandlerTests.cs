using FluentAssertions;
using FraudDetection.Application.Transactions.EventHandlers;
using FraudDetection.Domain.Transactions.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FraudDetection.Application.Tests.Transactions.EventHandlers;

public class TransactionFlaggedForFraudEventHandlerTests
{
    [Fact]
    public async Task Handle_WithFlaggedEvent_CompletesWithoutThrowing()
    {
        // Now that the handler implements our own IDomainEventHandler<T> directly
        // (rather than MediatR's INotificationHandler<DomainEventNotification<T>>),
        // it can be exercised with the raw domain event — no wrapper needed.
        var handler = new TransactionFlaggedForFraudEventHandler(
            NullLogger<TransactionFlaggedForFraudEventHandler>.Instance);

        var domainEvent = new TransactionFlaggedForFraudEvent(
            Guid.NewGuid(), Guid.NewGuid(), new[] { "LargeAmount", "HighVelocity" }, DateTime.UtcNow);

        var act = async () => await handler.Handle(domainEvent, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
