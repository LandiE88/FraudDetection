using FraudDetection.Domain.Fraud.Rules;
using FraudDetection.Infrastructure.Persistence.FraudRuleSettings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudDetection.Infrastructure.Persistence.Configurations;

public sealed class LargeAmountThresholdRecordConfiguration : IEntityTypeConfiguration<LargeAmountThresholdRecord>
{
    public void Configure(EntityTypeBuilder<LargeAmountThresholdRecord> builder)
    {
        builder.ToTable("large_amount_thresholds");

        builder.HasKey(t => t.Category);

        builder.Property(t => t.Category)
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasColumnType("VARCHAR(30)")
            .ValueGeneratedNever();

        builder.Property(t => t.ThresholdAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        // Seed a fresh database with exactly the values LargeAmountRule falls back to
        // when a category has no row, so the two never disagree out of the box. An
        // operator can then UPDATE these rows to retune the rule without a redeploy.
        builder.HasData(LargeAmountRule.DefaultThresholds.Select(kvp =>
            new LargeAmountThresholdRecord { Category = kvp.Key, ThresholdAmount = kvp.Value }));
    }
}
