using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using FraudDetection.Api.Contracts;
using FraudDetection.Application.AccountHolders.Dtos;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace FraudDetection.IntegrationTests;

public class AccountHolderCrudTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AccountHolderCrudTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_WithValidRequest_Returns201AndCanBeFetchedById()
    {
        var request = new CreateAccountHolderRequest(
            "Jane", "Doe", "A1234567", "jane.doe@example.com", new DateOnly(1990, 5, 20));

        var response = await _client.PostAsJsonAsync("/api/account-holders", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var created = await response.Content.ReadFromJsonAsync<AccountHolderResponse>();
        created!.Email.Should().Be("jane.doe@example.com");

        var getResponse = await _client.GetAsync($"/api/account-holders/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<AccountHolderResponse>();
        fetched!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task Post_WithInvalidEmail_Returns400()
    {
        var request = new CreateAccountHolderRequest(
            "Jane", "Doe", "A1234567", "not-an-email", new DateOnly(1990, 5, 20));

        var response = await _client.PostAsJsonAsync("/api/account-holders", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Should().ContainKey("Email");
    }

    [Fact]
    public async Task Put_WithValidRequest_UpdatesTheHolder()
    {
        var createRequest = new CreateAccountHolderRequest(
            "Jane", "Doe", "A1234567", "jane.doe@example.com", new DateOnly(1990, 5, 20));
        var createResponse = await _client.PostAsJsonAsync("/api/account-holders", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<AccountHolderResponse>();

        var updateRequest = new UpdateAccountHolderRequest(
            "Janet", "Doe-Smith", "A1234567", "janet.doe-smith@example.com", new DateOnly(1990, 5, 20));

        var updateResponse = await _client.PutAsJsonAsync($"/api/account-holders/{created!.Id}", updateRequest);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<AccountHolderResponse>();
        updated!.FirstName.Should().Be("Janet");
        updated.LastName.Should().Be("Doe-Smith");
        updated.Email.Should().Be("janet.doe-smith@example.com");

        var getResponse = await _client.GetAsync($"/api/account-holders/{created.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<AccountHolderResponse>();
        fetched!.FirstName.Should().Be("Janet");
    }

    [Fact]
    public async Task Put_WithUnknownId_Returns404()
    {
        var updateRequest = new UpdateAccountHolderRequest(
            "Janet", "Doe", "A1234567", "janet.doe@example.com", new DateOnly(1990, 5, 20));

        var response = await _client.PutAsJsonAsync($"/api/account-holders/{Guid.NewGuid()}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_WithUnknownId_Returns404()
    {
        var response = await _client.GetAsync($"/api/account-holders/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
