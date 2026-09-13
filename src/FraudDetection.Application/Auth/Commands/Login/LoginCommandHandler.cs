using FluentValidation;
using FraudDetection.Application.Auth.Dtos;
using FraudDetection.Application.Common.Exceptions;
using FraudDetection.Application.Common.Messaging;

namespace FraudDetection.Application.Auth.Commands.Login;

public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, AuthResponse>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IValidator<LoginCommand> _validator;

    public LoginCommandHandler(
        IIdentityService identityService, IJwtTokenGenerator jwtTokenGenerator, IValidator<LoginCommand> validator)
    {
        _identityService = identityService;
        _jwtTokenGenerator = jwtTokenGenerator;
        _validator = validator;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var userId = await _identityService.ValidateCredentialsAsync(request.Email, request.Password, cancellationToken)
            // Deliberately the same message whether the email is unknown or the
            // password is wrong — telling them apart would let a caller enumerate
            // registered emails.
            ?? throw new UnauthorizedException("Invalid email or password.");

        var token = _jwtTokenGenerator.GenerateToken(userId, request.Email);

        return new AuthResponse(request.Email, token.Value, token.ExpiresAtUtc);
    }
}
