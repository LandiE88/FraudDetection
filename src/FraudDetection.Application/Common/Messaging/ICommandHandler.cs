namespace FraudDetection.Application.Common.Messaging;

/// <summary>
/// Handles a single command (a request that changes state) and returns its result.
/// Implementations are looked up by the controller directly via DI — there is no bus.
/// </summary>
public interface ICommandHandler<in TCommand, TResponse>
{
    Task<TResponse> Handle(TCommand command, CancellationToken cancellationToken = default);
}
