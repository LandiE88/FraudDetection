using System.Reflection;
using FraudDetection.Application.Common.Events;
using FraudDetection.Domain.AccountHolders;
using FraudDetection.Domain.Common;
using FraudDetection.Domain.Transactions;
using FraudDetection.Infrastructure.Identity;
using FraudDetection.Infrastructure.Persistence.FraudRuleSettings;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FraudDetection.Infrastructure.Persistence;

/// <summary>
/// Inherits <see cref="IdentityDbContext{TUser, TRole, TKey}"/> rather than plain
/// <see cref="DbContext"/> so the Identity user/role tables live in the same database
/// and migration history as everything else — there's no separate "auth database" to
/// keep in sync. <see cref="ApplicationUser"/> is an infrastructure/framework concern
/// (see its own remarks), which is why it's the only entity here not configured
/// alongside the domain aggregates in Persistence/Configurations.
/// </summary>
public sealed class FraudDetectionDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
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
        base.OnModelCreating(modelBuilder);

        // Identity's default table names (AspNetUsers, AspNetRoles, ...) don't match
        // this project's lowercase-snake convention (account_holders,
        // transaction_events, ...) — rename them to fit rather than leaving Identity's
        // defaults as the one exception.
        modelBuilder.Entity<ApplicationUser>(b => b.ToTable("users"));
        modelBuilder.Entity<IdentityRole<Guid>>(b => b.ToTable("roles"));
        modelBuilder.Entity<IdentityUserRole<Guid>>(b => b.ToTable("user_roles"));
        modelBuilder.Entity<IdentityUserClaim<Guid>>(b => b.ToTable("user_claims"));
        modelBuilder.Entity<IdentityUserLogin<Guid>>(b => b.ToTable("user_logins"));
        modelBuilder.Entity<IdentityUserToken<Guid>>(b => b.ToTable("user_tokens"));
        modelBuilder.Entity<IdentityRoleClaim<Guid>>(b => b.ToTable("role_claims"));

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
