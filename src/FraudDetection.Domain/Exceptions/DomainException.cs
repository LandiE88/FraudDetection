namespace FraudDetection.Domain.Exceptions;

/// <summary>
/// Thrown when an operation would leave the domain in a state that violates one of
/// its invariants. The API layer maps this to HTTP 422 Unprocessable Entity.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
