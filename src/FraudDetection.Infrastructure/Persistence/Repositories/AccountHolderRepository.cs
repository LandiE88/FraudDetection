using FraudDetection.Domain.AccountHolders;
using FraudDetection.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace FraudDetection.Infrastructure.Persistence.Repositories;

public sealed class AccountHolderRepository : IAccountHolderRepository
{
    private readonly FraudDetectionDbContext _dbContext;

    public AccountHolderRepository(FraudDetectionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AccountHolder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _dbContext.AccountHolders.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<PagedList<AccountHolder>> SearchAsync(
        AccountHolderQuery query, CancellationToken cancellationToken = default)
    {
        var queryable = _dbContext.AccountHolders.AsQueryable();

        // Name/id/email fields are matched case-insensitively and as a substring —
        // this is a "search", not an exact filter. Note that a value containing SQL
        // LIKE wildcards (% or _) is taken literally as a wildcard rather than escaped.
        if (!string.IsNullOrWhiteSpace(query.FirstName))
        {
            queryable = queryable.Where(a => EF.Functions.ILike(a.FirstName, $"%{query.FirstName}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.LastName))
        {
            queryable = queryable.Where(a => EF.Functions.ILike(a.LastName, $"%{query.LastName}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.IdPassport))
        {
            queryable = queryable.Where(a => EF.Functions.ILike(a.IdPassport, $"%{query.IdPassport}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.Email))
        {
            queryable = queryable.Where(a => EF.Functions.ILike(a.Email.Value, $"%{query.Email}%"));
        }

        if (query.BirthYear.HasValue)
        {
            queryable = queryable.Where(a => a.DateOfBirth.Year == query.BirthYear.Value);
        }

        if (query.BirthMonth.HasValue)
        {
            queryable = queryable.Where(a => a.DateOfBirth.Month == query.BirthMonth.Value);
        }

        queryable = queryable.OrderBy(a => a.LastName).ThenBy(a => a.FirstName);

        var totalCount = await queryable.CountAsync(cancellationToken);

        var items = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<AccountHolder>(items, query.Page, query.PageSize, totalCount);
    }

    public void Add(AccountHolder accountHolder) => _dbContext.AccountHolders.Add(accountHolder);
}
