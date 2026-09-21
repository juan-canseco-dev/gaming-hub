using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using GameHub.Abstractions.Primitives;
using GameHub.Application.Abstractions.Observability;

namespace GameHub.Web.API.IntegrationTests.Helpers;

internal static class ProblemDetailsAssertions
{
    internal static async Task ShouldBeProblemAsync(
        this HttpResponseMessage response,
        Error expectedError,
        int expectedStatus)
    {
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("type").GetString().Should().StartWith("https://");
        problem.GetProperty("title").GetString().Should().NotBeNullOrWhiteSpace();
        problem.GetProperty("status").GetInt32().Should().Be(expectedStatus);
        problem.GetProperty("detail").GetString().Should().Be(expectedError.Description);
        problem.GetProperty("instance").GetString().Should().NotBeNullOrWhiteSpace();
        problem.GetProperty("code").GetString().Should().Be(expectedError.Code);

        var correlationId = problem.GetProperty("correlationId").GetString();
        correlationId.Should().NotBeNullOrWhiteSpace();
        response.Headers.GetValues(CorrelationIdConstants.HeaderName)
            .Should().ContainSingle(correlationId);
    }
}
