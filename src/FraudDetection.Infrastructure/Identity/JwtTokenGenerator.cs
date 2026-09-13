using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FraudDetection.Application.Auth;
using FraudDetection.Domain.Common;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FraudDetection.Infrastructure.Identity;

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _settings;
    private readonly IClock _clock;

    public JwtTokenGenerator(IOptions<JwtSettings> settings, IClock clock)
    {
        _settings = settings.Value;
        _clock = clock;
    }

    public GeneratedToken GenerateToken(Guid userId, string email)
    {
        var expiresAtUtc = _clock.UtcNow.AddMinutes(_settings.ExpiryMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: signingCredentials);

        return new GeneratedToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}
