using FluentValidation;
using FluentValidation.Results;
using FraudDetection.Application.Auth.Dtos;
using FraudDetection.Application.Common.Messaging;

namespace FraudDetection.Application.Auth.Commands.Register;

public sealed class RegisterCommandHandler : ICommandHandler<RegisterCommand, AuthResponse>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IValidator<RegisterCommand> _validator;

    public RegisterCommandHandler(
        IIdentityService identityService, IJwtTokenGenerator jwtTokenGenerator, IValidator<RegisterCommand> validator)
    {
        _identityService = identityService;
        _jwtTokenGenerator = jwtTokenGenerator;
        _validator = validator;
    }

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var result = await _identityService.CreateUserAsync(request.Email, request.Password, cancellationToken);
        if (!result.Succeeded)
        {
            throw new ValidationException(result.Errors.Select(e => new ValidationFailure(e.Field, e.Message)));
        }

        var token = _jwtTokenGenerator.GenerateToken(result.UserId, request.Email);

        return new AuthResponse(request.Email, token.Value, token.ExpiresAtUtc);
    }
}
