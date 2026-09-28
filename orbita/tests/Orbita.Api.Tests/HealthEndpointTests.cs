using System.Net;
using System.Net.Http.Json;
namespace Orbita.Api.Tests;

public sealed class HealthEndpointTests : IClassFixture<OrbitaApiFactory>
{
    private readonly HttpClient client;

    public HealthEndpointTests(OrbitaApiFactory factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_endpoint_reports_a_healthy_backend()
    {
        using var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var health = await response.Content.ReadFromJsonAsync<HealthResponse>();

        Assert.NotNull(health);
        Assert.Equal("ok", health.Status);
        Assert.False(health.DatabaseConfigured);
    }
}

internal sealed record HealthResponse(string Status, bool DatabaseConfigured);
