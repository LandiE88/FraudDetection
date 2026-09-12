using FluentAssertions;
using FraudDetection.Application.Common.Exceptions;
using FraudDetection.Application.Transactions.Queries.GetTransactionById;
using FraudDetection.Domain.Transactions;
using Moq;
using Xunit;

namespace FraudDetection.Application.Tests.Transactions.Queries;

public class GetTransactionByIdQueryHandlerTests
{
    private readonly Mock<ITransactionEventRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenTransactionExists_ReturnsMappedResponse()
    {
        var transaction = TransactionEvent.Create(
            Guid.NewGuid(), TransactionCategory.Purchase, Money.Create(42m, "ZAR"), "Merchant",
            DateTime.UtcNow, DateTime.UtcNow);

        _repository.Setup(r => r.GetByIdAsync(transaction.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        var handler = new GetTransactionByIdQueryHandler(_repository.Object);

        var response = await handler.Handle(new GetTransactionByIdQuery(transaction.Id), CancellationToken.None);

        response.Id.Should().Be(transaction.Id);
        response.Amount.Should().Be(42m);
    }

    [Fact]
    public async Task Handle_WhenTransactionDoesNotExist_ThrowsNotFoundException()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionEvent?)null);

        var handler = new GetTransactionByIdQueryHandler(_repository.Object);

        var act = async () => await handler.Handle(new GetTransactionByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
