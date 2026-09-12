using System.Reflection;
using FraudDetection.Application.Common.Events;
using FraudDetection.Domain.AccountHolders;
using FraudDetection.Domain.Common;
using FraudDetection.Domain.Transactions;
using FraudDetection.Infrastructure.Persistence.FraudRuleSettings;
using Microsoft.EntityFrameworkCore;

namespace FraudDetection.Infrastructure.Persistence;

public sealed class FraudDetectionDbContext : DbContext
{
    private readonly IDomainEventDispatcher? _domainEventDispatcher;

    public DbSet<TransactionEvent> TransactionEvents => Set<TransactionEvent>();
    public DbSet<LargeAmountThresholdRecord> LargeAmountThresholds => Set<LargeAmountThresholdRecord>();
    public DbSet<AccountHolder> AccountHolders => Set<AccountHolder>();

    public FraudDetectionDbContext(DbContextOptions<FraudDetectionDbContext> options)
        : base(options)
    {
    }

    // Optional dispatcher: null in contexts (e.g. design-time migrations) where the
    // application's DI container isn't set up, so domain-event dispatch is simply
    // skipped there.
    public FraudDetectionDbContext(DbContextOptions<FraudDetectionDbContext> options, IDomainEventDispatcher domainEventDispatcher)
        : base(options)
    {
        _domainEventDispatcher = domainEventDispatcher;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Snapshot aggregates with pending domain events before saving, since IDs
        // assigned during SaveChanges could otherwise change what ChangeTracker reports.
        var aggregatesWithEvents = ChangeTracker.Entries<IHasDomainEvents>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        if (_domainEventDispatcher is not null)
        {
            foreach (var aggregate in aggregatesWithEvents)
            {
                var events = aggregate.DomainEvents.ToList();
                aggregate.ClearDomainEvents();

                await _domainEventDispatcher.DispatchAsync(events, cancellationToken);
            }
        }

        return result;
    }
}
