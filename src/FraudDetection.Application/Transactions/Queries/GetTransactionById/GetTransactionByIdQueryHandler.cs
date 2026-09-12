using FraudDetection.Application.Common.Exceptions;
using FraudDetection.Application.Common.Messaging;
using FraudDetection.Application.Transactions.Dtos;
using FraudDetection.Domain.Transactions;

namespace FraudDetection.Application.Transactions.Queries.GetTransactionById;

public sealed class GetTransactionByIdQueryHandler : IQueryHandler<GetTransactionByIdQuery, TransactionResponse>
{
    private readonly ITransactionEventRepository _repository;

    public GetTransactionByIdQueryHandler(ITransactionEventRepository repository)
    {
        _repository = repository;
    }

    public async Task<TransactionResponse> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken = default)
    {
        var transaction = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(TransactionEvent), request.Id);

        return transaction.ToResponse();
    }
}
