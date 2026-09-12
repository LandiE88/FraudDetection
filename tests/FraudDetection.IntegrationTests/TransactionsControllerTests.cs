using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using FraudDetection.Api.Contracts;
using FraudDetection.Application.Common.Models;
using FraudDetection.Application.Transactions.Dtos;
using FraudDetection.Domain.Transactions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace FraudDetection.IntegrationTests;

public class TransactionsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TransactionsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_WithValidTransaction_Returns201AndPersistsIt()
    {
        var request = new IngestTransactionRequest(
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
        var request = new IngestTransactionRequest(
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
        var request = new IngestTransactionRequest(
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

        await _client.PostAsJsonAsync("/api/transactions", new IngestTransactionRequest(
            accountId, TransactionCategory.Purchase, 50m, "ZAR", "Clean Merchant", DateTime.UtcNow));

        await _client.PostAsJsonAsync("/api/transactions", new IngestTransactionRequest(
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
}
