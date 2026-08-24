using FluentAssertions;
using GameHub.Application.Abstractions.Observability;
using GameHub.Web.API.IntegrationTests.Abstractions;

namespace GameHub.Web.API.IntegrationTests.Observability;

[Collection(SharedTestCollection.FixtureName)]
public sealed class CorrelationIdTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task Request_ShouldEchoIncomingCorrelationId()
    {
        const string correlationId = "integration-test-correlation";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add(CorrelationIdConstants.HeaderName, correlationId);

        using var response = await factory.HttpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();
        response.Headers.GetValues(CorrelationIdConstants.HeaderName)
            .Should().ContainSingle().Which.Should().Be(correlationId);
    }

    [Fact]
    public async Task Request_ShouldReturnGeneratedCorrelationId_WhenHeaderIsMissing()
    {
        using var response = await factory.HttpClient.GetAsync("/");

        response.EnsureSuccessStatusCode();
        response.Headers.GetValues(CorrelationIdConstants.HeaderName)
            .Should().ContainSingle()
            .Which.Should().NotBeNullOrWhiteSpace();
    }
}
