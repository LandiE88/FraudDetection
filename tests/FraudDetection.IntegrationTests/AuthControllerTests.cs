using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using FraudDetection.Application.Auth.Dtos;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace FraudDetection.IntegrationTests;

/// <summary>
/// Covers the authentication endpoints themselves, plus the global policy that every
/// other controller requires a valid bearer token unless explicitly exempt.
/// </summary>
public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidRequest_Returns201WithAUsableToken()
    {
        var email = $"{Guid.NewGuid():N}@example.com";

        var response = await _client.PostAsJsonAsync("/api/auth/register", new { email, password = "Str0ngPassword!" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.Email.Should().Be(email);
        body.Token.Should().NotBeNullOrWhiteSpace();
        body.ExpiresAtUtc.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns400()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new { email, password = "Str0ngPassword!" });

        var response = await _client.PostAsJsonAsync("/api/auth/register", new { email, password = "AnotherStr0ngPassword!" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithWeakPassword_Returns400()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register", new { email = $"{Guid.NewGuid():N}@example.com", password = "weak" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Should().ContainKey("Password");
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_Returns200WithAToken()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        const string password = "Str0ngPassword!";
        await _client.PostAsJsonAsync("/api/auth/register", new { email, password });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        body!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new { email, password = "Str0ngPassword!" });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "TheWrongPassword!" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login", new { email = $"{Guid.NewGuid():N}@example.com", password = "Str0ngPassword!" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutAToken_Returns401()
    {
        var response = await _client.GetAsync("/api/transactions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithAValidToken_Succeeds()
    {
        var email = $"{Guid.NewGuid():N}@example.com";
        var registerResponse = await _client.PostAsJsonAsync(
            "/api/auth/register", new { email, password = "Str0ngPassword!" });
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/transactions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_DoesNotRequireAToken()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
