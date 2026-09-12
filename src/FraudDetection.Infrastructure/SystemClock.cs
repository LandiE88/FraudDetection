using FraudDetection.Domain.Common;

namespace FraudDetection.Infrastructure;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
