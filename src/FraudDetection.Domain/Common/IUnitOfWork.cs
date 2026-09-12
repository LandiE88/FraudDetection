namespace FraudDetection.Domain.Common;

/// <summary>
/// Commits changes made through repositories as a single transaction and, on success,
/// is responsible (via the infrastructure implementation) for dispatching any domain
/// events raised by the aggregates involved.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
