using FluentAssertions;
using FluentValidation;
using FraudDetection.Application.Transactions.Commands.IngestTransaction;
using FraudDetection.Domain.Common;
using FraudDetection.Domain.Fraud;
using FraudDetection.Domain.Fraud.Rules;
using FraudDetection.Domain.Transactions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FraudDetection.Application.Tests.Transactions.Commands;

public class IngestTransactionCommandHandlerTests
{
    private readonly Mock<ITransactionEventRepository> _repository = new();
    private readonly Mock<IFraudRuleSettingsRepository> _fraudRuleSettingsRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IClock> _clock = new();
    private static readonly DateTime FixedNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public IngestTransactionCommandHandlerTests()
    {
        _clock.SetupGet(c => c.UtcNow).Returns(FixedNow);
        _repository
            .Setup(r => r.GetRecentByAccountAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TransactionEvent>());
        _fraudRuleSettingsRepository
            .Setup(r => r.GetLargeAmountThresholdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(LargeAmountRule.DefaultThresholds);
    }

    private IngestTransactionCommandHandler CreateHandler(FraudRuleEngine? engine = null) => new(
        _repository.Object,
        _fraudRuleSettingsRepository.Object,
        _unitOfWork.Object,
        engine ?? new FraudRuleEngine(new IFraudRule[] { new LargeAmountRule() }),
        new IngestTransactionCommandValidator(),
        _clock.Object,
        NullLogger<IngestTransactionCommandHandler>.Instance);

    [Fact]
    public async Task Handle_WithCleanTransaction_PersistsUnflaggedTransaction()
    {
        var handler = CreateHandler();
        var command = new IngestTransactionCommand(
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
        var command = new IngestTransactionCommand(
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
        var command = new IngestTransactionCommand(
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
        var command = new IngestTransactionCommand(
            Guid.NewGuid(), TransactionCategory.Purchase, 75m, "ZAR", "Corner Store", FixedNow);

        var response = await handler.Handle(command, CancellationToken.None);

        response.IsFlagged.Should().BeTrue();
        response.FraudFlags.Should().ContainSingle(f => f.RuleName == "LargeAmount");
    }

    [Fact]
    public async Task Handle_WithInvalidAmount_ThrowsValidationExceptionBeforeTouchingRepository()
    {
        // With no MediatR pipeline behavior to run this automatically, the handler
        // validates its own request first (see IngestTransactionCommandValidator) —
        // this proves that guard is actually in place and short-circuits before any
        // domain object is constructed or the repository is touched.
        var handler = CreateHandler();
        var command = new IngestTransactionCommand(
            Guid.NewGuid(), TransactionCategory.Purchase, -5m, "ZAR", "Corner Store", FixedNow);

        var act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        _repository.Verify(r => r.Add(It.IsAny<TransactionEvent>()), Times.Never);
    }
}
