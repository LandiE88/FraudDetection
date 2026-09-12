namespace FraudDetection.Domain.Transactions;

/// <summary>
/// Filter and paging criteria for listing transaction events. Deliberately made of
/// primitives/enums only so it stays a plain query spec rather than pulling
/// application-layer concerns into the domain.
/// </summary>
public sealed class TransactionEventQuery
{
    public Guid? AccountId { get; init; }
    public TransactionCategory? Category { get; init; }
    public bool? OnlyFlagged { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
