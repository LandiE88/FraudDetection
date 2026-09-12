namespace FraudDetection.Application.Common.Messaging;

/// <summary>
/// Handles a single query (a read-only request) and returns its result. Implementations
/// are looked up by the controller directly via DI — there is no bus.
/// </summary>
public interface IQueryHandler<in TQuery, TResponse>
{
    Task<TResponse> Handle(TQuery query, CancellationToken cancellationToken = default);
}
