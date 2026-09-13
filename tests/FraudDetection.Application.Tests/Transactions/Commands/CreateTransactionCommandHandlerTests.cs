using FluentAssertions;
using FluentValidation;
using FraudDetection.Application.Common.Exceptions;
using FraudDetection.Application.Transactions.Commands.CreateTransaction;
using FraudDetection.Domain.AccountHolders;
using FraudDetection.Domain.Common;
using FraudDetection.Domain.Fraud;
using FraudDetection.Domain.Fraud.Rules;
using FraudDetection.Domain.Transactions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FraudDetection.Application.Tests.Transactions.Commands;

public class CreateTransactionCommandHandlerTests
{
    private readonly Mock<ITransactionEventRepository> _repository = new();
    private readonly Mock<IAccountHolderRepository> _accountHolderRepository = new();
    private readonly Mock<IFraudRuleSettingsRepository> _fraudRuleSettingsRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IClock> _clock = new();
    private static readonly DateTime FixedNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public CreateTransactionCommandHandlerTests()
    {
        _clock.SetupGet(c => c.UtcNow).Returns(FixedNow);
        _repository
            .Setup(r => r.GetRecentByAccountAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TransactionEvent>());
        _fraudRuleSettingsRepository
            .Setup(r => r.GetLargeAmountThresholdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(LargeAmountRule.DefaultThresholds);
    }

    private CreateTransactionCommandHandler CreateHandler(FraudRuleEngine? engine = null) => new(
        _repository.Object,
        _accountHolderRepository.Object,
        _fraudRuleSettingsRepository.Object,
        _unitOfWork.Object,
        engine ?? new FraudRuleEngine(new IFraudRule[] { new LargeAmountRule() }),
        new CreateTransactionCommandValidator(),
        _clock.Object,
        NullLogger<CreateTransactionCommandHandler>.Instance);

    private static AccountHolder CreateAccountHolder() => AccountHolder.Create(
        "Jane", "Doe", "A1234567", EmailAddress.Create("jane.doe@example.com"),
        new DateOnly(1990, 5, 20), DateOnly.FromDateTime(FixedNow));

    [Fact]
    public async Task Handle_WithCleanTransaction_PersistsUnflaggedTransaction()
    {
        var handler = CreateHandler();
        var command = new CreateTransactionCommand(
            Guid.NewGuid(), TransactionCategory.Purchase, 100m, "ZAR", "Corner Store", FixedNow);

        var response = await handler.Handle(command, CancellationToken.None);

        response.IsFlagged.Should().BeFalse();
        response.FraudFlags.Should().BeEmpty();
        _repository.Verify(r => r.Add(It.Is<TransactionEvent>(t => t.Id == response.Id)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithAmountOverThreshold_ReturnsFlaggedTransaction()
    {
        var handler = CreateHandler();
        var command = new CreateTransactionCommand(
            Guid.NewGuid(), TransactionCategory.Purchase, 50_000m, "ZAR", "Corner Store", FixedNow);

        var response = await handler.Handle(command, CancellationToken.None);

        response.IsFlagged.Should().BeTrue();
        response.FraudFlags.Should().ContainSingle(f => f.RuleName == "LargeAmount");
        response.HighestSeverity.Should().Be(nameof(FraudSeverity.Critical));
    }

    [Fact]
    public async Task Handle_LoadsHistorySinceBeforeTheOccurrenceTime()
    {
        var handler = CreateHandler();
        var accountId = Guid.NewGuid();
        var command = new CreateTransactionCommand(
            accountId, TransactionCategory.Purchase, 10m, "ZAR", "Corner Store", FixedNow);

        await handler.Handle(command, CancellationToken.None);

        _repository.Verify(r => r.GetRecentByAccountAsync(
            accountId,
            It.Is<DateTime>(since => since < FixedNow),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithCustomThresholdFromSettingsRepository_FlagsAccordingToConfiguredValue()
    {
        // A R75 purchase is well under LargeAmountRule's built-in R5,000 default, but
        // should still get flagged once the settings repository (standing in for a
        // database row an operator edited) reports a R50 threshold for Purchase.
        _fraudRuleSettingsRepository
            .Setup(r => r.GetLargeAmountThresholdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<TransactionCategory, decimal> { [TransactionCategory.Purchase] = 50m });

        var handler = CreateHandler();
        var command = new CreateTransactionCommand(
            Guid.NewGuid(), TransactionCategory.Purchase, 75m, "ZAR", "Corner Store", FixedNow);

        var response = await handler.Handle(command, CancellationToken.None);

        response.IsFlagged.Should().BeTrue();
        response.FraudFlags.Should().ContainSingle(f => f.RuleName == "LargeAmount");
    }

    [Fact]
    public async Task Handle_WithoutAccountHolderId_LeavesItNullOnTheTransaction()
    {
        var handler = CreateHandler();
        var command = new CreateTransactionCommand(
            Guid.NewGuid(), TransactionCategory.Purchase, 100m, "ZAR", "Corner Store", FixedNow);

        var response = await handler.Handle(command, CancellationToken.None);

        response.AccountHolderId.Should().BeNull();
        _accountHolderRepository.Verify(
            r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithKnownAccountHolderId_SetsItOnTheTransaction()
    {
        var accountId = Guid.NewGuid();
        var accountHolder = CreateAccountHolder();

        _accountHolderRepository
            .Setup(r => r.GetByIdAsync(accountHolder.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountHolder);

        var handler = CreateHandler();
        var command = new CreateTransactionCommand(
            accountId, TransactionCategory.Purchase, 100m, "ZAR", "Corner Store", FixedNow, accountHolder.Id);

        var response = await handler.Handle(command, CancellationToken.None);

        response.AccountHolderId.Should().Be(accountHolder.Id);
    }

    [Fact]
    public async Task Handle_WithUnknownAccountHolderId_ThrowsNotFoundExceptionBeforeTouchingRepository()
    {
        var unknownAccountHolderId = Guid.NewGuid();

        _accountHolderRepository
            .Setup(r => r.GetByIdAsync(unknownAccountHolderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccountHolder?)null);

        var handler = CreateHandler();
        var command = new CreateTransactionCommand(
            Guid.NewGuid(), TransactionCategory.Purchase, 100m, "ZAR", "Corner Store", FixedNow, unknownAccountHolderId);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _repository.Verify(r => r.Add(It.IsAny<TransactionEvent>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInvalidAmount_ThrowsValidationExceptionBeforeTouchingRepository()
    {
        // With no MediatR pipeline behavior to run this automatically, the handler
        // validates its own request first (see CreateTransactionCommandValidator) —
        // this proves that guard is actually in place and short-circuits before any
        // domain object is constructed or the repository is touched.
        var handler = CreateHandler();
        var command = new CreateTransactionCommand(
            Guid.NewGuid(), TransactionCategory.Purchase, -5m, "ZAR", "Corner Store", FixedNow);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        _repository.Verify(r => r.Add(It.IsAny<TransactionEvent>()), Times.Never);
    }
}
