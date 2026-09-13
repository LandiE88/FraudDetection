using FluentValidation.TestHelper;
using FraudDetection.Application.AccountHolders.Queries.SearchAccountHolders;
using Xunit;

namespace FraudDetection.Application.Tests.AccountHolders.Queries;

public class SearchAccountHoldersQueryValidatorTests
{
    private readonly SearchAccountHoldersQueryValidator _validator = new();

    [Fact]
    public void Validate_WithLastNameOnly_HasNoErrors()
    {
        var query = new SearchAccountHoldersQuery(null, "Doe", null, null, null, null);

        _validator.TestValidate(query).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithNoCriteriaAtAll_HasError()
    {
        var query = new SearchAccountHoldersQuery(null, null, null, null, null, null);

        _validator.TestValidate(query).ShouldHaveValidationErrorFor("SearchCriteria");
    }

    [Fact]
    public void Validate_WithOnlyPagingAndNoCriteria_HasError()
    {
        var query = new SearchAccountHoldersQuery(null, null, null, null, null, null, Page: 2, PageSize: 50);

        _validator.TestValidate(query).ShouldHaveValidationErrorFor("SearchCriteria");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public void Validate_WithOutOfRangeBirthMonth_HasError(int month)
    {
        var query = new SearchAccountHoldersQuery("Jane", null, null, null, null, month);

        _validator.TestValidate(query).ShouldHaveValidationErrorFor(x => x.BirthMonth);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    public void Validate_WithInRangeBirthMonth_HasNoErrorForBirthMonth(int month)
    {
        var query = new SearchAccountHoldersQuery("Jane", null, null, null, null, month);

        _validator.TestValidate(query).ShouldNotHaveValidationErrorFor(x => x.BirthMonth);
    }

    [Fact]
    public void Validate_WithBirthYearInTheFuture_HasError()
    {
        var query = new SearchAccountHoldersQuery("Jane", null, null, null, DateTime.UtcNow.Year + 1, null);

        _validator.TestValidate(query).ShouldHaveValidationErrorFor(x => x.BirthYear);
    }

    [Fact]
    public void Validate_WithBirthYearTooFarInThePast_HasError()
    {
        var query = new SearchAccountHoldersQuery("Jane", null, null, null, 1899, null);

        _validator.TestValidate(query).ShouldHaveValidationErrorFor(x => x.BirthYear);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithNonPositivePage_HasError(int page)
    {
        var query = new SearchAccountHoldersQuery("Jane", null, null, null, null, null, Page: page);

        _validator.TestValidate(query).ShouldHaveValidationErrorFor(x => x.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_WithPageSizeOutOfRange_HasError(int pageSize)
    {
        var query = new SearchAccountHoldersQuery("Jane", null, null, null, null, null, PageSize: pageSize);

        _validator.TestValidate(query).ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void Validate_WithPartialEmailSearchFragment_HasNoErrors()
    {
        // Deliberately not a full valid email address — this is a substring search,
        // so a fragment like a bare domain must be allowed.
        var query = new SearchAccountHoldersQuery(null, null, null, "@example.com", null, null);

        _validator.TestValidate(query).ShouldNotHaveAnyValidationErrors();
    }
}
