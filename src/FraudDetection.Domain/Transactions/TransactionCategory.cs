namespace FraudDetection.Domain.Transactions;

/// <summary>
/// The kind of financial movement a transaction event represents. Several fraud
/// rules (e.g. amount thresholds) key off this category.
/// </summary>
public enum TransactionCategory
{
    Purchase = 0,
    Withdrawal = 1,
    Deposit = 2,
    Transfer = 3,
    Refund = 4
}
