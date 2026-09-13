using FluentAssertions;
using FraudDetection.Domain.AccountHolders;
using FraudDetection.Domain.Exceptions;
using Xunit;

namespace FraudDetection.Domain.Tests.AccountHolders;

public class AccountHolderTests
{
    private static readonly DateOnly Today = new(2026, 1, 1);
    private static readonly EmailAddress ValidEmail = EmailAddress.Create("jane.doe@example.com");

    private static AccountHolder CreateValid(
        string firstName = "Jane",
        string lastName = "Doe",
        string idPassport = "A1234567",
        EmailAddress? email = null,
        DateOnly? dateOfBirth = null) =>
        AccountHolder.Create(
            firstName,
            lastName,
            idPassport,
            email ?? ValidEmail,
            dateOfBirth ?? new DateOnly(1990, 5, 20),
            Today);

    [Fact]
    public void Create_WithValidData_Succeeds()
    {
        var holder = CreateValid();

        holder.Id.Should().NotBe(Guid.Empty);
        holder.FirstName.Should().Be("Jane");
        holder.LastName.Should().Be("Doe");
        holder.IdPassport.Should().Be("A1234567");
        holder.Email.Should().Be(ValidEmail);
        holder.DateOfBirth.Should().Be(new DateOnly(1990, 5, 20));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankFirstName_Throws(string firstName)
    {
        var act = () => CreateValid(firstName: firstName);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankLastName_Throws(string lastName)
    {
        var act = () => CreateValid(lastName: lastName);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithNameOverMaxLength_Throws()
    {
        var tooLong = new string('a', AccountHolder.NameMaxLength + 1);

        var act = () => CreateValid(firstName: tooLong);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankIdPassport_Throws(string idPassport)
    {
        var act = () => CreateValid(idPassport: idPassport);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithIdPassportOverMaxLength_Throws()
    {
        var tooLong = new string('9', AccountHolder.IdPassportMaxLength + 1);

        var act = () => CreateValid(idPassport: tooLong);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithDateOfBirthInTheFuture_Throws()
    {
        var act = () => CreateValid(dateOfBirth: Today.AddDays(1));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithDateOfBirthEqualToToday_Succeeds()
    {
        var holder = CreateValid(dateOfBirth: Today);

        holder.DateOfBirth.Should().Be(Today);
    }

    [Fact]
    public void Create_TrimsNameAndIdPassport()
    {
        var holder = CreateValid(firstName: "  Jane  ", lastName: "  Doe  ", idPassport: "  A1234567  ");

        holder.FirstName.Should().Be("Jane");
        holder.LastName.Should().Be("Doe");
        holder.IdPassport.Should().Be("A1234567");
    }

    [Fact]
    public void Update_WithValidData_ReplacesEveryMutableFieldButNotId()
    {
        var holder = CreateValid();
        var originalId = holder.Id;
        var newEmail = EmailAddress.Create("john.smith@example.com");

        holder.Update("John", "Smith", "B9876543", newEmail, new DateOnly(1985, 2, 10), Today);

        holder.Id.Should().Be(originalId);
        holder.FirstName.Should().Be("John");
        holder.LastName.Should().Be("Smith");
        holder.IdPassport.Should().Be("B9876543");
        holder.Email.Should().Be(newEmail);
        holder.DateOfBirth.Should().Be(new DateOnly(1985, 2, 10));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithBlankFirstName_Throws(string firstName)
    {
        var holder = CreateValid();

        var act = () => holder.Update(firstName, "Doe", "A1234567", ValidEmail, new DateOnly(1990, 5, 20), Today);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Update_WithDateOfBirthInTheFuture_Throws()
    {
        var holder = CreateValid();

        var act = () => holder.Update("Jane", "Doe", "A1234567", ValidEmail, Today.AddDays(1), Today);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Update_WithIdPassportOverMaxLength_Throws()
    {
        var holder = CreateValid();
        var tooLong = new string('9', AccountHolder.IdPassportMaxLength + 1);

        var act = () => holder.Update("Jane", "Doe", tooLong, ValidEmail, new DateOnly(1990, 5, 20), Today);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Update_DoesNotMutateStateWhenValidationFails()
    {
        var holder = CreateValid();

        var act = () => holder.Update("", "Smith", "B9876543", ValidEmail, new DateOnly(1985, 2, 10), Today);

        act.Should().Throw<DomainException>();
        holder.FirstName.Should().Be("Jane");
        holder.LastName.Should().Be("Doe");
    }
}
