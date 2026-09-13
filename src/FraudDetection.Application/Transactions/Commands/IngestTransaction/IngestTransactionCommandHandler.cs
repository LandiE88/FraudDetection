using FluentValidation;
using FraudDetection.Application.Common.Exceptions;
using FraudDetection.Application.Common.Messaging;
using FraudDetection.Application.Transactions.Dtos;
using FraudDetection.Domain.AccountHolders;
using FraudDetection.Domain.Common;
using FraudDetection.Domain.Fraud;
using FraudDetection.Domain.Transactions;
using Microsoft.Extensions.Logging;

namespace FraudDetection.Application.Transactions.Commands.IngestTransaction;

public sealed class IngestTransactionCommandHandler : ICommandHandler<IngestTransactionCommand, TransactionResponse>
{
    // How far back to look for this account's history when evaluating rules such as
    // velocity and duplicate detection. Comfortably larger than the widest window any
    // current rule uses (10 minutes) so new rules have room without a code change here.
    private static readonly TimeSpan HistoryLookback = TimeSpan.FromHours(1);

    private readonly ITransactionEventRepository _transactionEventRepository;
    private readonly IAccountHolderRepository _accountHolderRepository;
    private readonly IFraudRuleSettingsRepository _fraudRuleSettingsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly FraudRuleEngine _fraudRuleEngine;
    private readonly IValidator<IngestTransactionCommand> _validator;
    private readonly IClock _clock;
    private readonly ILogger<IngestTransactionCommandHandler> _logger;

    public IngestTransactionCommandHandler(
        ITransactionEventRepository transactionEventRepository,
        IAccountHolderRepository accountHolderRepository,
        IFraudRuleSettingsRepository fraudRuleSettingsRepository,
        IUnitOfWork unitOfWork,
        FraudRuleEngine fraudRuleEngine,
        IValidator<IngestTransactionCommand> validator,
        IClock clock,
        ILogger<IngestTransactionCommandHandler> logger)
    {
        _transactionEventRepository = transactionEventRepository;
        _accountHolderRepository = accountHolderRepository;
        _fraudRuleSettingsRepository = fraudRuleSettingsRepository;
        _unitOfWork = unitOfWork;
        _fraudRuleEngine = fraudRuleEngine;
        _validator = validator;
        _clock = clock;
        _logger = logger;
    }

    public async Task<TransactionResponse> Handle(IngestTransactionCommand request, CancellationToken cancellationToken = default)
    {
        // With no pipeline to run this automatically, every handler validates its own
        // request up front. The API's global exception handler still turns the
        // FluentValidation.ValidationException this throws into a 400 with per-field
        // errors, exactly as it did when a MediatR behavior threw it.
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var amount = Money.Create(request.Amount, request.Currency);
        var ingestedAtUtc = _clock.UtcNow;

        // All three are independent reads, so fetch them concurrently rather than one
        // after the other.
        var historyTask = _transactionEventRepository.GetRecentByAccountAsync(
            request.AccountId, request.OccurredAtUtc - HistoryLookback, cancellationToken);
        var largeAmountThresholdsTask = _fraudRuleSettingsRepository.GetLargeAmountThresholdsAsync(cancellationToken);
        var accountHolderTask = request.AccountHolderId.HasValue
            ? _accountHolderRepository.GetByIdAsync(request.AccountHolderId.Value, cancellationToken)
            : Task.FromResult<AccountHolder?>(null);

        await Task.WhenAll(historyTask, largeAmountThresholdsTask, accountHolderTask);

        if (request.AccountHolderId.HasValue && accountHolderTask.Result is null)
        {
            throw new NotFoundException(nameof(AccountHolder), request.AccountHolderId.Value);
        }

        var transaction = TransactionEvent.Create(
            request.AccountId,
            request.Category,
            amount,
            request.MerchantName,
            request.OccurredAtUtc,
            ingestedAtUtc,
            request.AccountHolderId);

        var settings = new FraudRuleSettings { LargeAmountThresholds = largeAmountThresholdsTask.Result };
        var context = new FraudRuleEvaluationContext(transaction, historyTask.Result, settings);
        var ruleResults = _fraudRuleEngine.Evaluate(context);

        transaction.ApplyFraudAssessment(ruleResults);

        _transactionEventRepository.Add(transaction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (transaction.IsFlagged)
        {
            _logger.LogWarning(
                "Transaction {TransactionId} for account {AccountId} flagged by rules: {Rules}",
                transaction.Id,
                transaction.AccountId,
                string.Join(", ", transaction.FraudFlags.Select(f => f.RuleName)));
        }

        return transaction.ToResponse();
    }
}
