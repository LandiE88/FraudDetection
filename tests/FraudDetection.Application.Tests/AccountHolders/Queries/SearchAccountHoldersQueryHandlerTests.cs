using FluentAssertions;
using FluentValidation;
using FraudDetection.Application.AccountHolders.Queries.SearchAccountHolders;
using FraudDetection.Domain.AccountHolders;
using FraudDetection.Domain.Common;
using Moq;
using Xunit;

namespace FraudDetection.Application.Tests.AccountHolders.Queries;

public class SearchAccountHoldersQueryHandlerTests
{
    private readonly Mock<IAccountHolderRepository> _repository = new();
    private readonly SearchAccountHoldersQueryHandler _handler;

    public SearchAccountHoldersQueryHandlerTests()
    {
        _handler = new SearchAccountHoldersQueryHandler(_repository.Object, new SearchAccountHoldersQueryValidator());
    }

    private static AccountHolder CreateHolder(Guid accountId, string firstName, string lastName) =>
        AccountHolder.Create(
            accountId, firstName, lastName, "A1234567",
            EmailAddress.Create($"{firstName}.{lastName}@example.com".ToLowerInvariant()),
            new DateOnly(1990, 6, 15),
            new DateOnly(2026, 1, 1));

    [Fact]
    public async Task Handle_MapsRepositoryPageIntoPagedResult()
    {
        var accountId = Guid.NewGuid();
        var holder = CreateHolder(accountId, "Jane", "Doe");

        _repository.Setup(r => r.SearchAsync(It.IsAny<AccountHolderQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedList<AccountHolder>(new[] { holder }, page: 1, pageSize: 20, totalCount: 1));

        var result = await _handler.Handle(
            new SearchAccountHoldersQuery(accountId, null, null, null, null, null, null),
            CancellationToken.None);

        result.Items.Should().ContainSingle(a => a.Id == holder.Id && a.Email == "jane.doe@example.com");
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_PassesEveryFilterThroughToTheRepositoryQuery()
    {
        _repository.Setup(r => r.SearchAsync(It.IsAny<AccountHolderQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedList<AccountHolder>(Array.Empty<AccountHolder>(), 1, 20, 0));

        var accountId = Guid.NewGuid();

        await _handler.Handle(
            new SearchAccountHoldersQuery(accountId, "Jane", "Doe", "A123", "jane@", 1990, 6),
            CancellationToken.None);

        _repository.Verify(r => r.SearchAsync(
            It.Is<AccountHolderQuery>(q =>
                q.AccountId == accountId &&
                q.FirstName == "Jane" &&
                q.LastName == "Doe" &&
                q.IdPassport == "A123" &&
                q.Email == "jane@" &&
                q.BirthYear == 1990 &&
                q.BirthMonth == 6),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNoSearchCriteria_ThrowsValidationExceptionWithoutQueryingTheRepository()
    {
        var act = async () => await _handler.Handle(
            new SearchAccountHoldersQuery(null, null, null, null, null, null, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        _repository.Verify(r => r.SearchAsync(It.IsAny<AccountHolderQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
