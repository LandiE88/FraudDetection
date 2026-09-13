using System.Net.Http.Json;
using FluentAssertions;
using FraudDetection.Api.Contracts;
using FraudDetection.Application.Transactions.Dtos;
using FraudDetection.Domain.Transactions;
using Xunit;

namespace FraudDetection.IntegrationTests;

/// <summary>
/// Covers fraud rules that depend on an account's history rather than a single
/// transaction in isolation, proving the ingest → persist → re-query round trip that
/// backs them works against a real database.
/// </summary>
public class FraudRuleIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public FraudRuleIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task Post_SameAmountAndMerchantTwiceInQuickSuccession_FlagsTheSecondAsDuplicate()
    {
        var accountId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var first = new CreateTransactionRequest(accountId, TransactionCategory.Purchase, 60m, "ZAR", "Streaming Service", now);
        var second = new CreateTransactionRequest(accountId, TransactionCategory.Purchase, 60m, "ZAR", "Streaming Service", now.AddSeconds(30));

        await _client.PostAsJsonAsync("/api/transactions", first);
        var secondResponse = await _client.PostAsJsonAsync("/api/transactions", second);

        var body = await secondResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        body!.FraudFlags.Should().Contain(f => f.RuleName == "DuplicateTransaction");
    }

    [Fact]
    public async Task Post_SixTransactionsInsideTenMinutes_FlagsTheSixthAsHighVelocity()
    {
        var accountId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow;

        for (var i = 0; i < 5; i++)
        {
            await _client.PostAsJsonAsync("/api/transactions", new CreateTransactionRequest(
                accountId, TransactionCategory.Purchase, 20m + i, "ZAR", $"Merchant {i}", baseTime.AddMinutes(i)));
        }

        var sixthResponse = await _client.PostAsJsonAsync("/api/transactions", new CreateTransactionRequest(
            accountId, TransactionCategory.Purchase, 30m, "ZAR", "Merchant 5", baseTime.AddMinutes(5)));

        var body = await sixthResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        body!.FraudFlags.Should().Contain(f => f.RuleName == "HighVelocity");
    }
}
