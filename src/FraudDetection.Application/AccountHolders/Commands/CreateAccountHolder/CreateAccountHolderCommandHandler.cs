using FluentValidation;
using FraudDetection.Application.AccountHolders.Dtos;
using FraudDetection.Application.Common.Messaging;
using FraudDetection.Domain.AccountHolders;
using FraudDetection.Domain.Common;

namespace FraudDetection.Application.AccountHolders.Commands.CreateAccountHolder;

public sealed class CreateAccountHolderCommandHandler : ICommandHandler<CreateAccountHolderCommand, AccountHolderResponse>
{
    private readonly IAccountHolderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateAccountHolderCommand> _validator;
    private readonly IClock _clock;

    public CreateAccountHolderCommandHandler(
        IAccountHolderRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<CreateAccountHolderCommand> validator,
        IClock clock)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _clock = clock;
    }

    public async Task<AccountHolderResponse> Handle(CreateAccountHolderCommand request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var email = EmailAddress.Create(request.Email);
        var today = DateOnly.FromDateTime(_clock.UtcNow);

        var accountHolder = AccountHolder.Create(
            request.AccountId,
            request.FirstName,
            request.LastName,
            request.IdPassport,
            email,
            request.DateOfBirth,
            today);

        _repository.Add(accountHolder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return accountHolder.ToResponse();
    }
}
