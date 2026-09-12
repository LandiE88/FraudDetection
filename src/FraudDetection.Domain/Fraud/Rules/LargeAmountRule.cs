using FraudDetection.Domain.Transactions;

namespace FraudDetection.Domain.Fraud.Rules;

/// <summary>
/// Flags transactions whose amount exceeds a threshold defined per category. Different
/// categories carry different "normal" ranges, so each gets its own ceiling.
///
/// Thresholds are read from <see cref="FraudRuleEvaluationContext.Settings"/> — set by
/// an operator in the database (see <c>large_amount_thresholds</c> and
/// <see cref="IFraudRuleSettingsRepository"/>) — so they can be retuned without a code
/// change. <see cref="DefaultThresholds"/> is used for any category the configured
/// settings don't cover, so the rule never silently stops checking a category just
/// because it's missing a row.
/// </summary>
public sealed class LargeAmountRule : IFraudRule
{
    public string Name => "LargeAmount";

    public static readonly IReadOnlyDictionary<TransactionCategory, decimal> DefaultThresholds =
        new Dictionary<TransactionCategory, decimal>
        {
            [TransactionCategory.Purchase] = 5_000m,
            [TransactionCategory.Withdrawal] = 2_000m,
            [TransactionCategory.Transfer] = 10_000m,
            [TransactionCategory.Deposit] = 20_000m,
            [TransactionCategory.Refund] = 3_000m
        };

    public FraudRuleResult Evaluate(FraudRuleEvaluationContext context)
    {
        var transaction = context.Transaction;
        var threshold = ResolveThreshold(context.Settings, transaction.Category);

        if (transaction.Amount.Amount <= threshold)
        {
            return FraudRuleResult.Clear(Name);
        }

        var severity = transaction.Amount.Amount >= 50000  //this can also be set up in settings or db for more flexibility
            ? FraudSeverity.Critical
            : FraudSeverity.High;

        return FraudRuleResult.Flag(
            Name,
            severity,
            $"{transaction.Category} amount of {transaction.Amount} exceeds the {threshold:0.00} {transaction.Amount.Currency} threshold for this category.");
    }

    private static decimal ResolveThreshold(FraudRuleSettings settings, TransactionCategory category) =>
        settings.LargeAmountThresholds.TryGetValue(category, out var configured)
            ? configured
            : DefaultThresholds[category];
}
