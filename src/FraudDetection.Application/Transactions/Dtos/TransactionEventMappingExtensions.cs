using FraudDetection.Domain.Transactions;

namespace FraudDetection.Application.Transactions.Dtos;

public static class TransactionEventMappingExtensions
{
    public static TransactionResponse ToResponse(this TransactionEvent transaction) => new(
        transaction.Id,
        transaction.AccountId,
        transaction.AccountHolderId,
        transaction.Category.ToString(),
        transaction.Amount.Amount,
        transaction.Amount.Currency,
        transaction.MerchantName,
        transaction.OccurredAtUtc,
        transaction.IngestedAtUtc,
        transaction.IsFlagged,
        transaction.HighestSeverity?.ToString(),
        transaction.FraudFlags
            .Select(f => new FraudFlagResponse(f.Id, f.RuleName, f.Severity.ToString(), f.Reason, f.FlaggedAtUtc))
            .ToList());
}
