using System.Net.Http.Json;
using FluentAssertions;
using FraudDetection.Api.Contracts;
using FraudDetection.Application.Transactions.Dtos;
using FraudDetection.Domain.Transactions;
using FraudDetection.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FraudDetection.IntegrationTests;

/// <summary>
/// Proves the LargeAmount threshold is genuinely read from the database at request
/// time — editing the `large_amount_thresholds` row directly (as an operator would,
/// with no code change or redeploy) changes what the API flags.
/// </summary>
public class LargeAmountThresholdConfigurationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public LargeAmountThresholdConfigurationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_AfterLoweringThresholdInDatabase_FlagsAnAmountThatWasPreviouslyFine()
    {
        // Well under the seeded R5,000 default for Purchase — should not be flagged yet.
        var beforeResponse = await _client.PostAsJsonAsync("/api/transactions", new IngestTransactionRequest(
            Guid.NewGuid(), TransactionCategory.Purchase, 150m, "ZAR", "Corner Store", DateTime.UtcNow));
        var before = await beforeResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        before!.IsFlagged.Should().BeFalse();

        // An operator lowers the Purchase threshold directly in the database.
        await SetPurchaseThresholdAsync(100m);

        // The same amount should now be flagged, with no code change or restart.
        var afterResponse = await _client.PostAsJsonAsync("/api/transactions", new IngestTransactionRequest(
            Guid.NewGuid(), TransactionCategory.Purchase, 150m, "ZAR", "Corner Store", DateTime.UtcNow));
        var after = await afterResponse.Content.ReadFromJsonAsync<TransactionResponse>();

        after!.IsFlagged.Should().BeTrue();
        after.FraudFlags.Should().Contain(f => f.RuleName == "LargeAmount");

        // Leave the table as this test found it so other tests in the suite that rely
        // on the seeded defaults aren't affected.
        await SetPurchaseThresholdAsync(5_000m);
    }

    private async Task SetPurchaseThresholdAsync(decimal amount)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FraudDetectionDbContext>();

        var record = await dbContext.LargeAmountThresholds
            .SingleAsync(t => t.Category == TransactionCategory.Purchase);
        record.ThresholdAmount = amount;

        await dbContext.SaveChangesAsync();
    }
}
