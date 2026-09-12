using FraudDetection.Domain.Fraud;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudDetection.Infrastructure.Persistence.Configurations;

public sealed class FraudFlagConfiguration : IEntityTypeConfiguration<FraudFlag>
{
    public void Configure(EntityTypeBuilder<FraudFlag> builder)
    {
        builder.ToTable("fraud_flags");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .HasColumnType("UUID")
            .ValueGeneratedNever();

        builder.Property(f => f.TransactionEventId)
            .HasColumnType("UUID")
            .IsRequired();

        builder.Property(f => f.RuleName)
            .HasMaxLength(100)
            .HasColumnType("VARCHAR(100)")
            .IsRequired();

        builder.Property(f => f.Severity)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnType("VARCHAR(20)")
            .IsRequired();

        builder.Property(f => f.Reason)
            .HasMaxLength(1000)
            .HasColumnType("VARCHAR(1000)")
            .IsRequired();

        builder.Property(f => f.FlaggedAtUtc)
            .HasColumnType("TIMESTAMP WITH TIME ZONE")
            .IsRequired();

        builder.HasIndex(f => f.TransactionEventId);
    }
}
