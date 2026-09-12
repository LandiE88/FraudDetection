namespace FraudDetection.Domain.Fraud;

/// <summary>
/// How serious a triggered fraud rule is judged to be. Ordered so the highest
/// severity among a transaction's flags can be taken with a simple Max().
/// </summary>
public enum FraudSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}
