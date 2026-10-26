using System.Net;
namespace Orbita.Api.Tests;

public sealed class SpaHostingTests : IClassFixture<OrbitaApiFactory>
{
    private readonly HttpClient client;

    public SpaHostingTests(OrbitaApiFactory factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Unknown_api_route_returns_not_found_instead_of_the_spa()
    {
        using var response = await client.GetAsync("/api/no-existe");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
