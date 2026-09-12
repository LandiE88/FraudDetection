using FraudDetection.Domain.AccountHolders;

namespace FraudDetection.Application.AccountHolders.Dtos;

public static class AccountHolderMappingExtensions
{
    public static AccountHolderResponse ToResponse(this AccountHolder accountHolder) => new(
        accountHolder.Id,
        accountHolder.AccountId,
        accountHolder.FirstName,
        accountHolder.LastName,
        accountHolder.IdPassport,
        accountHolder.Email.Value,
        accountHolder.DateOfBirth);
}
