using FluentAssertions;
using FraudDetection.Application.Auth;
using FraudDetection.Application.Auth.Commands.Login;
using FraudDetection.Application.Common.Exceptions;
using Moq;
using Xunit;

namespace FraudDetection.Application.Tests.Auth.Commands;

public class LoginCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identityService = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _handler = new LoginCommandHandler(_identityService.Object, _jwtTokenGenerator.Object, new LoginCommandValidator());
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsTokenFromGenerator()
    {
        var userId = Guid.NewGuid();
        var expiresAtUtc = new DateTime(2026, 1, 1, 13, 0, 0, DateTimeKind.Utc);

        _identityService
            .Setup(s => s.ValidateCredentialsAsync("jane.doe@example.com", "Str0ngPass", It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);
        _jwtTokenGenerator
            .Setup(g => g.GenerateToken(userId, "jane.doe@example.com"))
            .Returns(new GeneratedToken("signed-token", expiresAtUtc));

        var response = await _handler.Handle(new LoginCommand("jane.doe@example.com", "Str0ngPass"), CancellationToken.None);

        response.Token.Should().Be("signed-token");
        response.ExpiresAtUtc.Should().Be(expiresAtUtc);
    }

    [Fact]
    public async Task Handle_WithInvalidCredentials_ThrowsUnauthorizedExceptionWithoutGeneratingAToken()
    {
        _identityService
            .Setup(s => s.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        var act = async () => await _handler.Handle(new LoginCommand("jane.doe@example.com", "wrong"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
        _jwtTokenGenerator.Verify(g => g.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }
}
