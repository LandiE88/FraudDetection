using FraudDetection.Domain.Common;
using FraudDetection.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

namespace FraudDetection.Infrastructure.Persistence.Repositories;

public sealed class TransactionEventRepository : ITransactionEventRepository
{
    private readonly FraudDetectionDbContext _dbContext;

    public TransactionEventRepository(FraudDetectionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TransactionEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _dbContext.TransactionEvents
            .Include(t => t.FraudFlags)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public async Task<IReadOnlyList<TransactionEvent>> GetRecentByAccountAsync(
        Guid accountId, DateTime sinceUtc, CancellationToken cancellationToken = default) =>
        await _dbContext.TransactionEvents
            .Include(t => t.FraudFlags)
            .Where(t => t.AccountId == accountId && t.OccurredAtUtc >= sinceUtc)
            .OrderByDescending(t => t.OccurredAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<PagedList<TransactionEvent>> SearchAsync(
        TransactionEventQuery query, CancellationToken cancellationToken = default)
    {
        var queryable = _dbContext.TransactionEvents
            .Include(t => t.FraudFlags)
            .AsQueryable();

        if (query.AccountId.HasValue)
        {
            queryable = queryable.Where(t => t.AccountId == query.AccountId.Value);
        }

        if (query.Category.HasValue)
        {
            queryable = queryable.Where(t => t.Category == query.Category.Value);
        }

        if (query.OnlyFlagged is true)
        {
            queryable = queryable.Where(t => t.FraudFlags.Any());
        }
        else if (query.OnlyFlagged is false)
        {
            queryable = queryable.Where(t => !t.FraudFlags.Any());
        }

        if (query.FromUtc.HasValue)
        {
            queryable = queryable.Where(t => t.OccurredAtUtc >= query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            queryable = queryable.Where(t => t.OccurredAtUtc <= query.ToUtc.Value);
        }

        queryable = queryable.OrderByDescending(t => t.OccurredAtUtc);

        var totalCount = await queryable.CountAsync(cancellationToken);

        var items = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<TransactionEvent>(items, query.Page, query.PageSize, totalCount);
    }

    public void Add(TransactionEvent transactionEvent) => _dbContext.TransactionEvents.Add(transactionEvent);
}
