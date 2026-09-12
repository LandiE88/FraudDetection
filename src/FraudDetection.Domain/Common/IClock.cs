namespace FraudDetection.Domain.Common;

/// <summary>
/// Abstraction over the system clock so that time-dependent domain behaviour
/// (audit stamps, rule evaluation windows) stays deterministic and testable.
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
