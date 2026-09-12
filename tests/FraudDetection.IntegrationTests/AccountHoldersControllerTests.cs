using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using FraudDetection.Application.AccountHolders.Dtos;
using FraudDetection.Application.Common.Models;
using FraudDetection.Domain.AccountHolders;
using FraudDetection.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FraudDetection.IntegrationTests;

public class AccountHoldersControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AccountHoldersControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_ByAccountId_ReturnsTheMatchingHolder()
    {
        var accountId = Guid.NewGuid();
        var holder = await SeedHolderAsync(accountId, "Jane", "Doe", "A1000001", "jane.doe@example.com", new DateOnly(1985, 3, 12));

        var response = await _client.GetAsync($"/api/account-holders?accountId={accountId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<AccountHolderResponse>>();
        page!.Items.Should().ContainSingle(a => a.Id == holder.Id);
    }

    [Fact]
    public async Task Get_ByPartialLastNameCaseInsensitive_ReturnsMatches()
    {
        var accountId = Guid.NewGuid();
        await SeedHolderAsync(accountId, "John", "Smithson", "B2000002", "john.smithson@example.com", new DateOnly(1978, 11, 2));

        var response = await _client.GetAsync("/api/account-holders?lastName=smith");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<AccountHolderResponse>>();
        page!.Items.Should().Contain(a => a.LastName == "Smithson");
    }

    [Fact]
    public async Task Get_ByBirthYearAndMonth_ReturnsOnlyMatchingHolders()
    {
        var matchingAccountId = Guid.NewGuid();
        var otherAccountId = Guid.NewGuid();

        await SeedHolderAsync(matchingAccountId, "Amara", "Okafor", "C3000003", "amara.okafor@example.com", new DateOnly(1992, 7, 4));
        await SeedHolderAsync(otherAccountId, "Amara", "Ngozi", "C3000004", "amara.ngozi@example.com", new DateOnly(1992, 9, 4));

        var response = await _client.GetAsync("/api/account-holders?firstName=Amara&birthYear=1992&birthMonth=7");

        var page = await response.Content.ReadFromJsonAsync<PagedResult<AccountHolderResponse>>();
        page!.Items.Should().ContainSingle(a => a.AccountId == matchingAccountId);
    }

    [Fact]
    public async Task Get_WithNoSearchCriteria_Returns400()
    {
        var response = await _client.GetAsync("/api/account-holders");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Should().ContainKey("SearchCriteria");
    }

    [Fact]
    public async Task Get_ByIdPassport_ReturnsTheMatchingHolder()
    {
        var accountId = Guid.NewGuid();
        await SeedHolderAsync(accountId, "Liam", "Nkosi", "D4000004-UNIQUE", "liam.nkosi@example.com", new DateOnly(2000, 1, 20));

        var response = await _client.GetAsync("/api/account-holders?idPassport=D4000004-UNIQUE");

        var page = await response.Content.ReadFromJsonAsync<PagedResult<AccountHolderResponse>>();
        page!.Items.Should().ContainSingle(a => a.AccountId == accountId);
    }

    [Fact]
    public async Task Get_ByEmailFragment_ReturnsTheMatchingHolder()
    {
        var accountId = Guid.NewGuid();
        await SeedHolderAsync(accountId, "Priya", "Naidoo", "E5000005", "priya.unique.tag@example.com", new DateOnly(1995, 4, 9));

        var response = await _client.GetAsync("/api/account-holders?email=unique.tag");

        var page = await response.Content.ReadFromJsonAsync<PagedResult<AccountHolderResponse>>();
        page!.Items.Should().ContainSingle(a => a.AccountId == accountId);
    }

    private async Task<AccountHolder> SeedHolderAsync(
        Guid accountId, string firstName, string lastName, string idPassport, string email, DateOnly dateOfBirth)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FraudDetectionDbContext>();

        var holder = AccountHolder.Create(
            accountId, firstName, lastName, idPassport, EmailAddress.Create(email), dateOfBirth,
            DateOnly.FromDateTime(DateTime.UtcNow));

        dbContext.AccountHolders.Add(holder);
        await dbContext.SaveChangesAsync();

        return holder;
    }
}
