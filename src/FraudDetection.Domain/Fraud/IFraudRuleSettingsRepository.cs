using FraudDetection.Domain.Transactions;

namespace FraudDetection.Domain.Fraud;

/// <summary>
/// Loads operator-configurable fraud rule settings from persistence. Implemented in
/// the infrastructure layer against whatever storage backs it (PostgreSQL, by
/// default). A missing or empty configuration should be treated as "use the built-in
/// defaults" — see <see cref="FraudRuleSettings.Default"/> — rather than a hard
/// failure, so the rule engine keeps working even before an operator has customized
/// anything.
/// </summary>
public interface IFraudRuleSettingsRepository
{
    Task<IReadOnlyDictionary<TransactionCategory, decimal>> GetLargeAmountThresholdsAsync(
        CancellationToken cancellationToken = default);
}
