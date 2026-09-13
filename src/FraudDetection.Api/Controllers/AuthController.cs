using FraudDetection.Api.Contracts;
using FraudDetection.Application.Auth.Commands.Login;
using FraudDetection.Application.Auth.Commands.Register;
using FraudDetection.Application.Auth.Dtos;
using FraudDetection.Application.Common.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FraudDetection.Api.Controllers;

/// <summary>
/// Issues the JWT access tokens every other endpoint requires. Marked
/// <see cref="AllowAnonymousAttribute"/> at the controller level so it's exempt from
/// the global "authenticated by default" policy configured in Program.cs.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    private readonly ICommandHandler<RegisterCommand, AuthResponse> _registerHandler;
    private readonly ICommandHandler<LoginCommand, AuthResponse> _loginHandler;

    public AuthController(
        ICommandHandler<RegisterCommand, AuthResponse> registerHandler,
        ICommandHandler<LoginCommand, AuthResponse> loginHandler)
    {
        _registerHandler = registerHandler;
        _loginHandler = loginHandler;
    }

    /// <summary>Registers a new user and returns an access token for immediate use — no separate login step required.</summary>
    /// <response code="201">The account was created; the response body carries a ready-to-use access token.</response>
    /// <response code="400">The request failed validation (weak password, malformed email, or email already registered).</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(request.Email, request.Password);
        var result = await _registerHandler.Handle(command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Exchanges an email/password pair for a fresh access token.</summary>
    /// <response code="200">The credentials were valid; the response body carries an access token.</response>
    /// <response code="400">The request failed validation.</response>
    /// <response code="401">The email/password pair is invalid.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await _loginHandler.Handle(command, cancellationToken);

        return Ok(result);
    }
}
