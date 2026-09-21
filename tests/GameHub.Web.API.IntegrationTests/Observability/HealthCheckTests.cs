using System.Net;
using FluentAssertions;
using GameHub.Web.API.IntegrationTests.Abstractions;

namespace GameHub.Web.API.IntegrationTests.Observability;

[Collection(SharedTestCollection.FixtureName)]
public sealed class HealthCheckTests(CustomWebApplicationFactory factory)
{
    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpoint_WhenDependenciesAreAvailable_ShouldReturnHealthy(string path)
    {
        using var response = await factory.HttpClient.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("Healthy");
    }
}
