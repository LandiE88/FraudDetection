using FluentAssertions;
using FluentValidation;
using FraudDetection.Application.Transactions.Queries.GetTransactions;
using FraudDetection.Domain.Common;
using FraudDetection.Domain.Transactions;
using Moq;
using Xunit;

namespace FraudDetection.Application.Tests.Transactions.Queries;

public class GetTransactionsQueryHandlerTests
{
    private readonly Mock<ITransactionEventRepository> _repository = new();

    [Fact]
    public async Task Handle_MapsRepositoryPageIntoPagedResult()
    {
        var transaction = TransactionEvent.Create(
            Guid.NewGuid(), TransactionCategory.Deposit, Money.Create(500m, "ZAR"), "Merchant",
            DateTime.UtcNow, DateTime.UtcNow);

        var pagedList = new PagedList<TransactionEvent>(new[] { transaction }, page: 2, pageSize: 10, totalCount: 15);

        _repository.Setup(r => r.SearchAsync(It.IsAny<TransactionEventQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedList);

        var handler = new GetTransactionsQueryHandler(_repository.Object, new GetTransactionsQueryValidator());

        var result = await handler.Handle(
            new GetTransactionsQuery(null, null, null, null, null, Page: 2, PageSize: 10),
            CancellationToken.None);

        result.Items.Should().ContainSingle(t => t.Id == transaction.Id);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(15);
        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task Handle_PassesFiltersThroughToTheRepositoryQuery()
    {
        var accountId = Guid.NewGuid();

        _repository.Setup(r => r.SearchAsync(It.IsAny<TransactionEventQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedList<TransactionEvent>(Array.Empty<TransactionEvent>(), 1, 20, 0));

        var handler = new GetTransactionsQueryHandler(_repository.Object, new GetTransactionsQueryValidator());

        await handler.Handle(
            new GetTransactionsQuery(accountId, TransactionCategory.Withdrawal, true, null, null),
            CancellationToken.None);

        _repository.Verify(r => r.SearchAsync(
            It.Is<TransactionEventQuery>(q =>
                q.AccountId == accountId &&
                q.Category == TransactionCategory.Withdrawal &&
                q.OnlyFlagged == true),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithPageSizeOverTheLimit_ThrowsValidationExceptionWithoutQueryingTheRepository()
    {
        var handler = new GetTransactionsQueryHandler(_repository.Object, new GetTransactionsQueryValidator());

        var act = async () => await handler.Handle(
            new GetTransactionsQuery(null, null, null, null, null, Page: 1, PageSize: 500),
            CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        _repository.Verify(r => r.SearchAsync(It.IsAny<TransactionEventQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
