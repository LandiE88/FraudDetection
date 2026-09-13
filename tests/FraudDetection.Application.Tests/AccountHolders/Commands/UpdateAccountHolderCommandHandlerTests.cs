using FluentAssertions;
using FraudDetection.Application.AccountHolders.Commands.UpdateAccountHolder;
using FraudDetection.Application.Common.Exceptions;
using FraudDetection.Domain.AccountHolders;
using FraudDetection.Domain.Common;
using Moq;
using Xunit;

namespace FraudDetection.Application.Tests.AccountHolders.Commands;

public class UpdateAccountHolderCommandHandlerTests
{
    private readonly Mock<IAccountHolderRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IClock> _clock = new();
    private static readonly DateTime FixedNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private UpdateAccountHolderCommandHandler CreateHandler()
    {
        _clock.SetupGet(c => c.UtcNow).Returns(FixedNow);
        return new UpdateAccountHolderCommandHandler(
            _repository.Object, _unitOfWork.Object, new UpdateAccountHolderCommandValidator(), _clock.Object);
    }

    private static AccountHolder CreateExistingHolder() => AccountHolder.Create(
        Guid.NewGuid(), "Jane", "Doe", "A1234567", EmailAddress.Create("jane.doe@example.com"),
        new DateOnly(1990, 5, 20), DateOnly.FromDateTime(FixedNow));

    [Fact]
    public async Task Handle_WithExistingHolder_UpdatesAndPersistsIt()
    {
        var holder = CreateExistingHolder();
        _repository.Setup(r => r.GetByIdAsync(holder.Id, It.IsAny<CancellationToken>())).ReturnsAsync(holder);

        var handler = CreateHandler();
        var command = new UpdateAccountHolderCommand(
            holder.Id, "John", "Smith", "B9876543", "john.smith@example.com", new DateOnly(1985, 2, 10));

        var response = await handler.Handle(command, CancellationToken.None);

        response.FirstName.Should().Be("John");
        response.LastName.Should().Be("Smith");
        response.Email.Should().Be("john.smith@example.com");
        response.AccountId.Should().Be(holder.AccountId); // unchanged
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUnknownId_ThrowsNotFoundException()
    {
        _repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountHolder?)null);

        var handler = CreateHandler();
        var command = new UpdateAccountHolderCommand(
            Guid.NewGuid(), "John", "Smith", "B9876543", "john.smith@example.com", new DateOnly(1985, 2, 10));

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
