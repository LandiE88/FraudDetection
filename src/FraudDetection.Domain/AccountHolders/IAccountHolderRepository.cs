using FraudDetection.Domain.Common;

namespace FraudDetection.Domain.AccountHolders;

public interface IAccountHolderRepository
{
    Task<AccountHolder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedList<AccountHolder>> SearchAsync(AccountHolderQuery query, CancellationToken cancellationToken = default);

    void Add(AccountHolder accountHolder);
}
