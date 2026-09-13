using FraudDetection.Domain.AccountHolders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FraudDetection.Infrastructure.Persistence.Configurations;

public sealed class AccountHolderConfiguration : IEntityTypeConfiguration<AccountHolder>
{
    public void Configure(EntityTypeBuilder<AccountHolder> builder)
    {
        builder.ToTable("account_holders", table =>
        {
            // Defense in depth: even a write that bypasses the API's EmailAddress
            // validation (a manual INSERT/UPDATE, a future service sharing this
            // database) still can't leave a malformed email in this column.
            table.HasCheckConstraint(
                "CK_account_holders_email_format",
                "\"email\" ~* '^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$'");
        });

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnType("UUID")
            .ValueGeneratedNever();

        builder.Property(a => a.FirstName)
            .HasMaxLength(AccountHolder.NameMaxLength)
            .HasColumnType($"VARCHAR({AccountHolder.NameMaxLength})")
            .IsRequired();

        builder.Property(a => a.LastName)
            .HasMaxLength(AccountHolder.NameMaxLength)
            .HasColumnType($"VARCHAR({AccountHolder.NameMaxLength})")
            .IsRequired();

        builder.Property(a => a.IdPassport)
            .HasColumnName("id_passport")
            .HasMaxLength(AccountHolder.IdPassportMaxLength)
            .HasColumnType($"VARCHAR({AccountHolder.IdPassportMaxLength})")
            .IsRequired();

        // Modeled as an owned type (like Money on TransactionEvent), not a
        // HasConversion scalar: a converted property's wrapped object can't have its
        // members (e.g. .Value) translated in LINQ queries, which would break the
        // ILIKE search on email in AccountHolderRepository. An owned type's
        // properties translate to SQL normally.
        builder.OwnsOne(a => a.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("email")
                .HasMaxLength(EmailAddress.MaxLength)
                .HasColumnType($"VARCHAR({EmailAddress.MaxLength})")
                .IsRequired();

            email.HasIndex(e => e.Value);
        });

        builder.Navigation(a => a.Email).IsRequired();

        builder.Property(a => a.DateOfBirth)
            .HasColumnName("date_of_birth")
            .HasColumnType("DATE")
            .IsRequired();

        builder.Ignore(a => a.DomainEvents);

        builder.HasIndex(a => a.IdPassport);
    }
}
