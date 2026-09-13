using FluentAssertions;
using FluentValidation;
using FraudDetection.Application.AccountHolders.Commands.CreateAccountHolder;
using FraudDetection.Domain.AccountHolders;
using FraudDetection.Domain.Common;
using Moq;
using Xunit;

namespace FraudDetection.Application.Tests.AccountHolders.Commands;

public class CreateAccountHolderCommandHandlerTests
{
    private readonly Mock<IAccountHolderRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IClock> _clock = new();
    private static readonly DateTime FixedNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private CreateAccountHolderCommandHandler CreateHandler()
    {
        _clock.SetupGet(c => c.UtcNow).Returns(FixedNow);
        return new CreateAccountHolderCommandHandler(
            _repository.Object, _unitOfWork.Object, new CreateAccountHolderCommandValidator(), _clock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_PersistsAndReturnsTheAccountHolder()
    {
        var handler = CreateHandler();
        var command = new CreateAccountHolderCommand(
            "Jane", "Doe", "A1234567", "jane.doe@example.com", new DateOnly(1990, 5, 20));

        var response = await handler.Handle(command, CancellationToken.None);

        response.FirstName.Should().Be("Jane");
        response.Email.Should().Be("jane.doe@example.com");
        _repository.Verify(r => r.Add(It.Is<AccountHolder>(a => a.Id == response.Id)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidEmail_ThrowsValidationExceptionWithoutTouchingRepository()
    {
        var handler = CreateHandler();
        var command = new CreateAccountHolderCommand(
            "Jane", "Doe", "A1234567", "not-an-email", new DateOnly(1990, 5, 20));

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        _repository.Verify(r => r.Add(It.IsAny<AccountHolder>()), Times.Never);
    }
}
