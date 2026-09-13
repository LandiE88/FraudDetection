using FluentValidation;
using FraudDetection.Application.Common.Messaging;
using FraudDetection.Application.Common.Models;
using FraudDetection.Application.Transactions.Dtos;
using FraudDetection.Domain.Transactions;

namespace FraudDetection.Application.Transactions.Queries.GetTransactions;

public sealed class GetTransactionsQueryHandler
    : IQueryHandler<GetTransactionsQuery, PagedResult<TransactionResponse>>
{
    private readonly ITransactionEventRepository _repository;
    private readonly IValidator<GetTransactionsQuery> _validator;

    public GetTransactionsQueryHandler(ITransactionEventRepository repository, IValidator<GetTransactionsQuery> validator)
    {
        _repository = repository;
        _validator = validator;
    }

    public async Task<PagedResult<TransactionResponse>> Handle(GetTransactionsQuery request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var query = new TransactionEventQuery
        {
            AccountId = request.AccountId,
            AccountHolderId = request.AccountHolderId,
            Category = request.Category,
            OnlyFlagged = request.OnlyFlagged,
            FromUtc = request.FromUtc,
            ToUtc = request.ToUtc,
            Page = request.Page,
            PageSize = request.PageSize
        };

        var result = await _repository.SearchAsync(query, cancellationToken);

        return new PagedResult<TransactionResponse>
        {
            Items = result.Items.Select(t => t.ToResponse()).ToList(),
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            TotalPages = result.TotalPages
        };
    }
}
