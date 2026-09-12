using FraudDetection.Application.AccountHolders.Dtos;
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

    public AccountHoldersController(
        IQueryHandler<SearchAccountHoldersQuery, PagedResult<AccountHolderResponse>> searchHandler)
    {
        _searchHandler = searchHandler;
    }

    /// <summary>
    /// Searches account holders by any combination of account id, name, id/passport
    /// number, email, and birth year/month — at least one criterion is required.
    /// Name, id/passport and email match case-insensitively as a substring; account
    /// id, birth year and birth month match exactly. Pair this with
    /// <c>GET /api/transactions?accountId=...</c> to find all transactions for a
    /// given holder's account(s).
    /// </summary>
    /// <response code="200">A (possibly empty) page of matching account holders.</response>
    /// <response code="400">No search criterion was provided, or a parameter failed validation.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AccountHolderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AccountHolderResponse>>> Search(
        [FromQuery] Guid? accountId,
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
            accountId, firstName, lastName, idPassport, email, birthYear, birthMonth, page, pageSize);

        var result = await _searchHandler.Handle(query, cancellationToken);
        return Ok(result);
    }
}
