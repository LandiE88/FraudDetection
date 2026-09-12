namespace FraudDetection.Domain.Fraud.Rules;

/// <summary>
/// Flags a transaction that looks like a duplicate/replay of one that just happened
/// on the same account: identical amount and merchant within a short window,
/// consistent with a double-charge or a replayed payment request.
/// </summary>
public sealed class DuplicateTransactionRule : IFraudRule
{
    public string Name => "DuplicateTransaction";

    private static readonly TimeSpan Window = TimeSpan.FromMinutes(2);

    public FraudRuleResult Evaluate(FraudRuleEvaluationContext context)
    {
        var transaction = context.Transaction;
        var windowStart = transaction.OccurredAtUtc - Window;

        var duplicate = context.RecentAccountHistory.FirstOrDefault(t =>
            t.OccurredAtUtc >= windowStart &&
            t.OccurredAtUtc <= transaction.OccurredAtUtc &&
            t.MerchantName.Equals(transaction.MerchantName, StringComparison.OrdinalIgnoreCase) &&
            t.Amount == transaction.Amount);

        if (duplicate is null)
        {
            return FraudRuleResult.Clear(Name);
        }

        return FraudRuleResult.Flag(
            Name,
            FraudSeverity.Medium,
            $"Matches a {transaction.Amount} transaction at '{transaction.MerchantName}' within the last {Window.TotalMinutes:0} minutes (event {duplicate.Id}).");
    }
}
