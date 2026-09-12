using FraudDetection.Domain.Common;

namespace FraudDetection.Domain.Transactions;

public interface ITransactionEventRepository
{
    Task<TransactionEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the given account's transaction history since <paramref name="sinceUtc"/>
    /// (by occurrence time), most-recent-first. Used to build the recent-history
    /// window that fraud rules such as velocity/duplicate checks evaluate against.
    /// </summary>
    Task<IReadOnlyList<TransactionEvent>> GetRecentByAccountAsync(
        Guid accountId, DateTime sinceUtc, CancellationToken cancellationToken = default);

    Task<PagedList<TransactionEvent>> SearchAsync(TransactionEventQuery query, CancellationToken cancellationToken = default);

    void Add(TransactionEvent transactionEvent);
}
