using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Orbita.Api.Autenticacion;

namespace Orbita.Api.Tests;

public sealed class AuthEndpointTests : IClassFixture<OrbitaApiFactory>
{
    private readonly HttpClient client;

    public AuthEndpointTests(OrbitaApiFactory factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_returns_a_signed_token_for_configured_credentials()
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("test-user", "test-password"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(login);
        Assert.False(string.IsNullOrWhiteSpace(login.AccessToken));
        Assert.True(login.ExpiresAtUtc > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Login_rejects_credentials_that_do_not_match_configuration()
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("test-user", "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        using var missingUsernameResponse = await client.PostAsJsonAsync("/api/auth/login", new { username = (string?)null, password = "test-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, missingUsernameResponse.StatusCode);
    }

    [Fact]
    public async Task Protected_endpoint_requires_a_valid_token()
    {
        using var anonymousResponse = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        using var invalidTokenRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        invalidTokenRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-token");
        using var invalidTokenResponse = await client.SendAsync(invalidTokenRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, invalidTokenResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("test-user", "test-password"));
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login);

        using var authenticatedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        authenticatedRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", login.AccessToken);
        using var authenticatedResponse = await client.SendAsync(authenticatedRequest);

        Assert.Equal(HttpStatusCode.OK, authenticatedResponse.StatusCode);
        var user = await authenticatedResponse.Content.ReadFromJsonAsync<CurrentUserResponse>();
        Assert.Equal("test-user", user?.Username);
    }

}
