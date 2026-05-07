using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FinancialAPI.IntegrationTests.Auth;

public class AuthEndpointsTests :
    IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(
        WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var request = new
        {
            username = "admin",
            password = "Admin@123"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();

        body.Should().Contain("accessToken");
        body.Should().Contain("refreshToken");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        var request = new
        {
            username = "admin",
            password = "WrongPassword"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Payments_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync(
            "/api/v1/payments?pageNumber=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Payments_WithValidToken_ReturnsSuccess()
    {
        // Login first
        var loginRequest = new
        {
            username = "admin",
            password = "Admin@123"
        };

        var loginResponse = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            loginRequest);

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginBody =
            await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        loginBody.Should().NotBeNull();

        // Attach JWT
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                loginBody!.AccessToken);

        // Call protected endpoint
        var response = await _client.GetAsync(
            "/api/v1/payments?pageNumber=1&pageSize=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private sealed class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}