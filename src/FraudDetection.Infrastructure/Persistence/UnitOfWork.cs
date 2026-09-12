using FraudDetection.Domain.Common;

namespace FraudDetection.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly FraudDetectionDbContext _dbContext;

    public UnitOfWork(FraudDetectionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
