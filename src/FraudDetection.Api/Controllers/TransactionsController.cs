using FraudDetection.Api.Contracts;
using FraudDetection.Application.Common.Messaging;
using FraudDetection.Application.Common.Models;
using FraudDetection.Application.Transactions.Commands.IngestTransaction;
using FraudDetection.Application.Transactions.Dtos;
using FraudDetection.Application.Transactions.Queries.GetTransactionById;
using FraudDetection.Application.Transactions.Queries.GetTransactions;
using FraudDetection.Domain.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace FraudDetection.Api.Controllers;

[ApiController]
[Route("api/transactions")]
[Produces("application/json")]
public sealed class TransactionsController : ControllerBase
{
    private readonly ICommandHandler<IngestTransactionCommand, TransactionResponse> _ingestHandler;
    private readonly IQueryHandler<GetTransactionByIdQuery, TransactionResponse> _getByIdHandler;
    private readonly IQueryHandler<GetTransactionsQuery, PagedResult<TransactionResponse>> _searchHandler;

    public TransactionsController(
        ICommandHandler<IngestTransactionCommand, TransactionResponse> ingestHandler,
        IQueryHandler<GetTransactionByIdQuery, TransactionResponse> getByIdHandler,
        IQueryHandler<GetTransactionsQuery, PagedResult<TransactionResponse>> searchHandler)
    {
        _ingestHandler = ingestHandler;
        _getByIdHandler = getByIdHandler;
        _searchHandler = searchHandler;
    }

    /// <summary>
    /// Ingests a categorized transaction event and evaluates it against every
    /// configured fraud rule. <c>accountHolderId</c> is optional, but if supplied it
    /// must reference an existing account holder.
    /// </summary>
    /// <response code="201">The transaction was recorded (whether or not it was flagged).</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="404"><c>accountHolderId</c> was supplied but no such account holder exists.</response>
    /// <response code="422">The request violates a domain invariant.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TransactionResponse>> Ingest(
        [FromBody] IngestTransactionRequest request, CancellationToken cancellationToken)
    {
        var command = new IngestTransactionCommand(
            request.AccountId,
            request.Category,
            request.Amount,
            request.Currency,
            request.MerchantName,
            request.OccurredAtUtc,
            request.AccountHolderId);

        var result = await _ingestHandler.Handle(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Retrieves a single transaction event, including any fraud flags raised against it.</summary>
    /// <response code="200">The transaction was found.</response>
    /// <response code="404">No transaction exists with that id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getByIdHandler.Handle(new GetTransactionByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lists transaction events, optionally filtered by account, account holder,
    /// category, flagged status and date range. Use <c>accountHolderId</c> — the real
    /// foreign key to <c>account_holders.Id</c> — to find all transactions for a
    /// given account holder.
    /// </summary>
    /// <response code="200">A (possibly empty) page of matching transactions.</response>
    /// <response code="400">The query parameters failed validation.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TransactionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<TransactionResponse>>> Search(
        [FromQuery] Guid? accountId,
        [FromQuery] Guid? accountHolderId,
        [FromQuery] TransactionCategory? category,
        [FromQuery] bool? onlyFlagged,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTransactionsQuery(accountId, accountHolderId, category, onlyFlagged, fromUtc, toUtc, page, pageSize);
        var result = await _searchHandler.Handle(query, cancellationToken);
        return Ok(result);
    }
}
