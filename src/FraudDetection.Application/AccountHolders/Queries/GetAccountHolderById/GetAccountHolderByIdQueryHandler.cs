using FraudDetection.Application.AccountHolders.Dtos;
using FraudDetection.Application.Common.Exceptions;
using FraudDetection.Application.Common.Messaging;
using FraudDetection.Domain.AccountHolders;

namespace FraudDetection.Application.AccountHolders.Queries.GetAccountHolderById;

public sealed class GetAccountHolderByIdQueryHandler : IQueryHandler<GetAccountHolderByIdQuery, AccountHolderResponse>
{
    private readonly IAccountHolderRepository _repository;

    public GetAccountHolderByIdQueryHandler(IAccountHolderRepository repository)
    {
        _repository = repository;
    }

    public async Task<AccountHolderResponse> Handle(GetAccountHolderByIdQuery request, CancellationToken cancellationToken = default)
    {
        var accountHolder = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(AccountHolder), request.Id);

        return accountHolder.ToResponse();
    }
}
