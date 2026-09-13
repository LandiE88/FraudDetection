using FluentAssertions;
using FluentValidation;
using FraudDetection.Application.Auth;
using FraudDetection.Application.Auth.Commands.Register;
using Moq;
using Xunit;

namespace FraudDetection.Application.Tests.Auth.Commands;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identityService = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();
    private readonly RegisterCommandHandler _handler;

    public RegisterCommandHandlerTests()
    {
        _handler = new RegisterCommandHandler(_identityService.Object, _jwtTokenGenerator.Object, new RegisterCommandValidator());
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsTokenFromGenerator()
    {
        var userId = Guid.NewGuid();
        var expiresAtUtc = new DateTime(2026, 1, 1, 13, 0, 0, DateTimeKind.Utc);

        _identityService
            .Setup(s => s.CreateUserAsync("jane.doe@example.com", "Str0ngPass", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUserResult.Success(userId));
        _jwtTokenGenerator
            .Setup(g => g.GenerateToken(userId, "jane.doe@example.com"))
            .Returns(new GeneratedToken("signed-token", expiresAtUtc));

        var response = await _handler.Handle(new RegisterCommand("jane.doe@example.com", "Str0ngPass"), CancellationToken.None);

        response.Email.Should().Be("jane.doe@example.com");
        response.Token.Should().Be("signed-token");
        response.ExpiresAtUtc.Should().Be(expiresAtUtc);
    }

    [Fact]
    public async Task Handle_WithInvalidCommand_ThrowsValidationExceptionWithoutTouchingIdentityService()
    {
        var act = async () => await _handler.Handle(new RegisterCommand("not-an-email", "weak"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        _identityService.Verify(
            s => s.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyRegistered_ThrowsValidationExceptionWithoutGeneratingAToken()
    {
        _identityService
            .Setup(s => s.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUserResult.Failure(new[] { new IdentityFieldError("Email", "Email 'jane.doe@example.com' is already taken.") }));

        var act = async () => await _handler.Handle(new RegisterCommand("jane.doe@example.com", "Str0ngPass"), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        _jwtTokenGenerator.Verify(g => g.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }
}
