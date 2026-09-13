using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using FraudDetection.Api.Contracts;
using FraudDetection.Application.Common.Models;
using FraudDetection.Application.Transactions.Dtos;
using FraudDetection.Domain.AccountHolders;
using FraudDetection.Domain.Transactions;
using FraudDetection.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FraudDetection.IntegrationTests;

public class TransactionsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TransactionsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task Post_WithValidTransaction_Returns201AndPersistsIt()
    {
        var request = new CreateTransactionRequest(
            Guid.NewGuid(), TransactionCategory.Purchase, 75.50m, "ZAR", "Corner Store", DateTime.UtcNow);

        var response = await _client.PostAsJsonAsync("/api/transactions", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var body = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        body.Should().NotBeNull();
        body!.AccountId.Should().Be(request.AccountId);
        body.Amount.Should().Be(75.50m);
        body.IsFlagged.Should().BeFalse();

        var getResponse = await _client.GetAsync($"/api/transactions/{body.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Post_WithAmountOverCategoryThreshold_ReturnsFlaggedTransaction()
    {
        var request = new CreateTransactionRequest(
            Guid.NewGuid(), TransactionCategory.Purchase, 25_000m, "ZAR", "Electronics Megastore", DateTime.UtcNow);

        var response = await _client.PostAsJsonAsync("/api/transactions", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        body!.IsFlagged.Should().BeTrue();
        body.FraudFlags.Should().Contain(f => f.RuleName == "LargeAmount");
    }

    [Fact]
    public async Task Post_WithInvalidPayload_Returns400WithValidationErrors()
    {
        var request = new CreateTransactionRequest(
            Guid.Empty, TransactionCategory.Purchase, -10m, "US", "", DateTime.UtcNow);

        var response = await _client.PostAsJsonAsync("/api/transactions", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Errors.Should().ContainKey("AccountId");
        problem.Errors.Should().ContainKey("Amount");
        problem.Errors.Should().ContainKey("Currency");
        problem.Errors.Should().ContainKey("MerchantName");
    }

    [Fact]
    public async Task GetById_WithUnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/transactions/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_FilteredByOnlyFlagged_ReturnsOnlyFlaggedTransactionsForThatAccount()
    {
        var accountId = Guid.NewGuid();

        await _client.PostAsJsonAsync("/api/transactions", new CreateTransactionRequest(
            accountId, TransactionCategory.Purchase, 50m, "ZAR", "Clean Merchant", DateTime.UtcNow));

        await _client.PostAsJsonAsync("/api/transactions", new CreateTransactionRequest(
            accountId, TransactionCategory.Purchase, 25_000m, "ZAR", "Suspicious Merchant", DateTime.UtcNow));

        var response = await _client.GetAsync($"/api/transactions?accountId={accountId}&onlyFlagged=true");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await response.Content.ReadFromJsonAsync<PagedResult<TransactionResponse>>();
        page.Should().NotBeNull();
        page!.Items.Should().ContainSingle();
        page.Items.Single().MerchantName.Should().Be("Suspicious Merchant");
        page.Items.Single().IsFlagged.Should().BeTrue();
    }

    [Fact]
    public async Task Get_Health_ReturnsHealthy()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Post_WithKnownAccountHolderId_LinksTheTransactionToTheHolder()
    {
        var holder = await SeedAccountHolderAsync();

        var request = new CreateTransactionRequest(
            Guid.NewGuid(), TransactionCategory.Purchase, 100m, "ZAR", "Corner Store", DateTime.UtcNow, holder.Id);

        var response = await _client.PostAsJsonAsync("/api/transactions", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<TransactionResponse>();
        body!.AccountHolderId.Should().Be(holder.Id);
    }

    [Fact]
    public async Task Post_WithUnknownAccountHolderId_Returns404()
    {
        var request = new CreateTransactionRequest(
            Guid.NewGuid(), TransactionCategory.Purchase, 100m, "ZAR", "Corner Store", DateTime.UtcNow, Guid.NewGuid());

        var response = await _client.PostAsJsonAsync("/api/transactions", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_FilteredByAccountHolderId_ReturnsOnlyThatHoldersTransactions()
    {
        var holder = await SeedAccountHolderAsync();
        var otherHolder = await SeedAccountHolderAsync();

        await _client.PostAsJsonAsync("/api/transactions", new CreateTransactionRequest(
            Guid.NewGuid(), TransactionCategory.Purchase, 50m, "ZAR", "Holder's Merchant", DateTime.UtcNow, holder.Id));

        await _client.PostAsJsonAsync("/api/transactions", new CreateTransactionRequest(
            Guid.NewGuid(), TransactionCategory.Purchase, 60m, "ZAR", "Other Holder's Merchant", DateTime.UtcNow, otherHolder.Id));

        var response = await _client.GetAsync($"/api/transactions?accountHolderId={holder.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await response.Content.ReadFromJsonAsync<PagedResult<TransactionResponse>>();
        page.Should().NotBeNull();
        page!.Items.Should().ContainSingle();
        page.Items.Single().MerchantName.Should().Be("Holder's Merchant");
    }

    private async Task<AccountHolder> SeedAccountHolderAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FraudDetectionDbContext>();

        var holder = AccountHolder.Create(
            "Jane", "Doe", "A1234567", EmailAddress.Create($"{Guid.NewGuid():N}@example.com"),
            new DateOnly(1990, 5, 20), DateOnly.FromDateTime(DateTime.UtcNow));

        dbContext.AccountHolders.Add(holder);
        await dbContext.SaveChangesAsync();

        return holder;
    }
}
