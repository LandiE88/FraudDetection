using FraudDetection.Domain.Fraud;
using FraudDetection.Domain.Fraud.Rules;
using FraudDetection.Domain.Transactions;
using Microsoft.EntityFrameworkCore;

namespace FraudDetection.Infrastructure.Persistence.Repositories;

public sealed class FraudRuleSettingsRepository : IFraudRuleSettingsRepository
{
    private readonly FraudDetectionDbContext _dbContext;

    public FraudRuleSettingsRepository(FraudDetectionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyDictionary<TransactionCategory, decimal>> GetLargeAmountThresholdsAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.LargeAmountThresholds.AsNoTracking().ToListAsync(cancellationToken);

        // An empty table (e.g. every row was deleted) falls back to the built-in
        // defaults rather than leaving every category unchecked by this rule.
        return rows.Count == 0
            ? LargeAmountRule.DefaultThresholds
            : rows.ToDictionary(r => r.Category, r => r.ThresholdAmount);
    }
}
