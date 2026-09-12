using FluentAssertions;
using FraudDetection.Application.Common.Events;
using FraudDetection.Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FraudDetection.Application.Tests.Common.Events;

public class DomainEventDispatcherTests
{
    private sealed record TestEvent(string Payload) : IDomainEvent
    {
        public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
    }

    private sealed class RecordingHandler : IDomainEventHandler<TestEvent>
    {
        public List<string> Received { get; } = new();

        public Task Handle(TestEvent domainEvent, CancellationToken cancellationToken = default)
        {
            Received.Add(domainEvent.Payload);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task DispatchAsync_InvokesEveryRegisteredHandlerForTheEventType()
    {
        var handler = new RecordingHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IDomainEventHandler<TestEvent>>(handler);
        var provider = services.BuildServiceProvider();

        var dispatcher = new DomainEventDispatcher(provider);

        await dispatcher.DispatchAsync(new[] { new TestEvent("hello") });

        handler.Received.Should().ContainSingle().Which.Should().Be("hello");
    }

    [Fact]
    public async Task DispatchAsync_WithNoRegisteredHandler_DoesNotThrow()
    {
        var provider = new ServiceCollection().BuildServiceProvider();
        var dispatcher = new DomainEventDispatcher(provider);

        var act = async () => await dispatcher.DispatchAsync(new[] { new TestEvent("orphan") });

        await act.Should().NotThrowAsync();
    }
}
