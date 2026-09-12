using FluentValidation;
using FraudDetection.Application.AccountHolders.Dtos;
using FraudDetection.Application.Common.Messaging;
using FraudDetection.Application.Common.Models;
using FraudDetection.Domain.AccountHolders;

namespace FraudDetection.Application.AccountHolders.Queries.SearchAccountHolders;

public sealed class SearchAccountHoldersQueryHandler
    : IQueryHandler<SearchAccountHoldersQuery, PagedResult<AccountHolderResponse>>
{
    private readonly IAccountHolderRepository _repository;
    private readonly IValidator<SearchAccountHoldersQuery> _validator;

    public SearchAccountHoldersQueryHandler(IAccountHolderRepository repository, IValidator<SearchAccountHoldersQuery> validator)
    {
        _repository = repository;
        _validator = validator;
    }

    public async Task<PagedResult<AccountHolderResponse>> Handle(
        SearchAccountHoldersQuery request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var query = new AccountHolderQuery
        {
            AccountId = request.AccountId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            IdPassport = request.IdPassport,
            Email = request.Email,
            BirthYear = request.BirthYear,
            BirthMonth = request.BirthMonth,
            Page = request.Page,
            PageSize = request.PageSize
        };

        var result = await _repository.SearchAsync(query, cancellationToken);

        return new PagedResult<AccountHolderResponse>
        {
            Items = result.Items.Select(a => a.ToResponse()).ToList(),
            Page = result.Page,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            TotalPages = result.TotalPages
        };
    }
}
