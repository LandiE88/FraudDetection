using FraudDetection.Api.Contracts;
using FraudDetection.Application.AccountHolders.Commands.CreateAccountHolder;
using FraudDetection.Application.AccountHolders.Commands.UpdateAccountHolder;
using FraudDetection.Application.AccountHolders.Dtos;
using FraudDetection.Application.AccountHolders.Queries.GetAccountHolderById;
using FraudDetection.Application.AccountHolders.Queries.SearchAccountHolders;
using FraudDetection.Application.Common.Messaging;
using FraudDetection.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace FraudDetection.Api.Controllers;

[ApiController]
[Route("api/account-holders")]
[Produces("application/json")]
public sealed class AccountHoldersController : ControllerBase
{
    private readonly IQueryHandler<SearchAccountHoldersQuery, PagedResult<AccountHolderResponse>> _searchHandler;
    private readonly IQueryHandler<GetAccountHolderByIdQuery, AccountHolderResponse> _getByIdHandler;
    private readonly ICommandHandler<CreateAccountHolderCommand, AccountHolderResponse> _createHandler;
    private readonly ICommandHandler<UpdateAccountHolderCommand, AccountHolderResponse> _updateHandler;

    public AccountHoldersController(
        IQueryHandler<SearchAccountHoldersQuery, PagedResult<AccountHolderResponse>> searchHandler,
        IQueryHandler<GetAccountHolderByIdQuery, AccountHolderResponse> getByIdHandler,
        ICommandHandler<CreateAccountHolderCommand, AccountHolderResponse> createHandler,
        ICommandHandler<UpdateAccountHolderCommand, AccountHolderResponse> updateHandler)
    {
        _searchHandler = searchHandler;
        _getByIdHandler = getByIdHandler;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
    }

    /// <summary>Registers a person against an account.</summary>
    /// <response code="201">The account holder was created.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="422">The request violates a domain invariant.</response>
    [HttpPost]
    [ProducesResponseType(typeof(AccountHolderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AccountHolderResponse>> Create(
        [FromBody] CreateAccountHolderRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateAccountHolderCommand(
            request.FirstName, request.LastName, request.IdPassport, request.Email, request.DateOfBirth);

        var result = await _createHandler.Handle(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Replaces an existing account holder's name, id/passport, email and date of
    /// birth. Does not change which account they're linked to — re-register a new
    /// holder for that instead.
    /// </summary>
    /// <response code="200">The account holder was updated.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="404">No account holder exists with that id.</response>
    /// <response code="422">The request violates a domain invariant.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AccountHolderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AccountHolderResponse>> Update(
        Guid id, [FromBody] UpdateAccountHolderRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateAccountHolderCommand(
            id, request.FirstName, request.LastName, request.IdPassport, request.Email, request.DateOfBirth);

        var result = await _updateHandler.Handle(command, cancellationToken);

        return Ok(result);
    }

    /// <summary>Retrieves a single account holder by id.</summary>
    /// <response code="200">The account holder was found.</response>
    /// <response code="404">No account holder exists with that id.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AccountHolderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccountHolderResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getByIdHandler.Handle(new GetAccountHolderByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Searches account holders by any combination of name, id/passport number,
    /// email, and birth year/month — at least one criterion is required. Name,
    /// id/passport and email match case-insensitively as a substring; birth year and
    /// birth month match exactly. Pair this with
    /// <c>GET /api/transactions?accountHolderId=...</c> to find all transactions for
    /// a given holder.
    /// </summary>
    /// <response code="200">A (possibly empty) page of matching account holders.</response>
    /// <response code="400">No search criterion was provided, or a parameter failed validation.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AccountHolderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AccountHolderResponse>>> Search(
        [FromQuery] string? firstName,
        [FromQuery] string? lastName,
        [FromQuery] string? idPassport,
        [FromQuery] string? email,
        [FromQuery] int? birthYear,
        [FromQuery] int? birthMonth,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new SearchAccountHoldersQuery(
            firstName, lastName, idPassport, email, birthYear, birthMonth, page, pageSize);

        var result = await _searchHandler.Handle(query, cancellationToken);
        return Ok(result);
    }
}
