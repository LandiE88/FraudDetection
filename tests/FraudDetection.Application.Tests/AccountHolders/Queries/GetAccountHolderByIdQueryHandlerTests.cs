using FluentAssertions;
using FraudDetection.Application.AccountHolders.Queries.GetAccountHolderById;
using FraudDetection.Application.Common.Exceptions;
using FraudDetection.Domain.AccountHolders;
using Moq;
using Xunit;

namespace FraudDetection.Application.Tests.AccountHolders.Queries;

public class GetAccountHolderByIdQueryHandlerTests
{
    private readonly Mock<IAccountHolderRepository> _repository = new();

    [Fact]
    public async Task Handle_WhenAccountHolderExists_ReturnsMappedResponse()
    {
        var holder = AccountHolder.Create(
            "Jane", "Doe", "A1234567", EmailAddress.Create("jane.doe@example.com"),
            new DateOnly(1990, 5, 20), new DateOnly(2026, 1, 1));

        _repository.Setup(r => r.GetByIdAsync(holder.Id, It.IsAny<CancellationToken>())).ReturnsAsync(holder);

        var handler = new GetAccountHolderByIdQueryHandler(_repository.Object);

        var response = await handler.Handle(new GetAccountHolderByIdQuery(holder.Id), CancellationToken.None);

        response.Id.Should().Be(holder.Id);
        response.Email.Should().Be("jane.doe@example.com");
    }

    [Fact]
    public async Task Handle_WhenAccountHolderDoesNotExist_ThrowsNotFoundException()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountHolder?)null);

        var handler = new GetAccountHolderByIdQueryHandler(_repository.Object);

        var act = async () => await handler.Handle(new GetAccountHolderByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
