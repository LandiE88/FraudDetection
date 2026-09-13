namespace FraudDetection.Application.AccountHolders.Queries.GetAccountHolderById;

/// <summary>Fetches a single account holder by id. Handled by <see cref="GetAccountHolderByIdQueryHandler"/>.</summary>
public sealed record GetAccountHolderByIdQuery(Guid Id);
