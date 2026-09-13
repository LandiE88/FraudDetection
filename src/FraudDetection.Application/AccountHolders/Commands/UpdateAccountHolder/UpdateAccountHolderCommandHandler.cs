using FluentValidation;
using FraudDetection.Application.AccountHolders.Dtos;
using FraudDetection.Application.Common.Exceptions;
using FraudDetection.Application.Common.Messaging;
using FraudDetection.Domain.AccountHolders;
using FraudDetection.Domain.Common;

namespace FraudDetection.Application.AccountHolders.Commands.UpdateAccountHolder;

public sealed class UpdateAccountHolderCommandHandler : ICommandHandler<UpdateAccountHolderCommand, AccountHolderResponse>
{
    private readonly IAccountHolderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateAccountHolderCommand> _validator;
    private readonly IClock _clock;

    public UpdateAccountHolderCommandHandler(
        IAccountHolderRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<UpdateAccountHolderCommand> validator,
        IClock clock)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _clock = clock;
    }

    public async Task<AccountHolderResponse> Handle(UpdateAccountHolderCommand request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var accountHolder = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(AccountHolder), request.Id);

        var email = EmailAddress.Create(request.Email);
        var today = DateOnly.FromDateTime(_clock.UtcNow);

        accountHolder.Update(request.FirstName, request.LastName, request.IdPassport, email, request.DateOfBirth, today);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return accountHolder.ToResponse();
    }
}
