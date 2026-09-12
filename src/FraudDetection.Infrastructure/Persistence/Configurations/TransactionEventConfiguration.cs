using FraudDetection.Domain.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudDetection.Infrastructure.Persistence.Configurations;

public sealed class TransactionEventConfiguration : IEntityTypeConfiguration<TransactionEvent>
{
    public void Configure(EntityTypeBuilder<TransactionEvent> builder)
    {
        builder.ToTable("transaction_events");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnType("UUID")
            .ValueGeneratedNever();

        builder.Property(t => t.AccountId)
            .HasColumnType("UUID")
            .IsRequired();

        builder.Property(t => t.Category)
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasColumnType("VARCHAR(30)")
            .IsRequired();

        builder.Property(t => t.MerchantName)
            .HasMaxLength(200)
            .HasColumnType("VARCHAR(200)")
            .IsRequired();

        builder.Property(t => t.OccurredAtUtc)
            .HasColumnType("TIMESTAMP WITH TIME ZONE")
            .IsRequired();

        builder.Property(t => t.IngestedAtUtc)
            .HasColumnType("TIMESTAMP WITH TIME ZONE")
            .IsRequired();

        builder.OwnsOne(t => t.Amount, amount =>
        {
            amount.Property(m => m.Amount)
                .HasColumnName("amount")
                .HasPrecision(18, 2)
                .IsRequired();

            amount.Property(m => m.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .HasColumnType("VARCHAR(3)")
                .IsRequired();
        });

        builder.Navigation(t => t.Amount).IsRequired();

        builder.HasMany(t => t.FraudFlags)
            .WithOne()
            .HasForeignKey(f => f.TransactionEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(TransactionEvent.FraudFlags))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(t => t.DomainEvents);

        builder.HasIndex(t => t.AccountId);
        builder.HasIndex(t => t.OccurredAtUtc);
        builder.HasIndex(t => new { t.AccountId, t.OccurredAtUtc });
    }
}
