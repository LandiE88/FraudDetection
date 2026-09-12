namespace FraudDetection.Application.Transactions.Queries.GetTransactionById;

/// <summary>Fetches a single transaction by id. Handled by <see cref="GetTransactionByIdQueryHandler"/>.</summary>
public sealed record GetTransactionByIdQuery(Guid Id);
