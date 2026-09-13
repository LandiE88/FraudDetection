namespace FraudDetection.Application.Common.Exceptions;

/// <summary>Thrown when a credential (e.g. an email/password pair) is invalid. Mapped to HTTP 401.</summary>
public sealed class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message)
    {
    }
}
