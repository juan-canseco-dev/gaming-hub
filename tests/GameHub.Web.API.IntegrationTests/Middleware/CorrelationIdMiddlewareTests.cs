using FluentAssertions;
using GameHub.Application.Abstractions.Observability;
using GameHub.Web.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameHub.Web.API.IntegrationTests.Middleware;

public sealed class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldPropagateValidIncomingCorrelationId()
    {
        const string incomingCorrelationId = "client-correlation_123";
        var context = new DefaultHttpContext();
        var accessor = new TestCorrelationIdAccessor { CorrelationId = "outer-operation" };
        string? correlationIdInsidePipeline = null;
        context.Request.Headers[CorrelationIdConstants.HeaderName] = incomingCorrelationId;
        var middleware = CreateMiddleware(_ =>
        {
            correlationIdInsidePipeline = accessor.CorrelationId;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context, accessor);

        correlationIdInsidePipeline.Should().Be(incomingCorrelationId);
        context.TraceIdentifier.Should().Be(incomingCorrelationId);
        context.Response.Headers[CorrelationIdConstants.HeaderName].ToString()
            .Should().Be(incomingCorrelationId);
        accessor.CorrelationId.Should().Be("outer-operation");
    }

    [Fact]
    public async Task InvokeAsync_ShouldGenerateCorrelationId_WhenIncomingValueIsInvalid()
    {
        var context = new DefaultHttpContext();
        var accessor = new TestCorrelationIdAccessor();
        context.Request.Headers[CorrelationIdConstants.HeaderName] = new string('x', 65);
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, accessor);

        context.TraceIdentifier.Should().NotBeNullOrWhiteSpace();
        context.TraceIdentifier.Should().NotBe(new string('x', 65));
        context.TraceIdentifier.Length.Should().BeLessThanOrEqualTo(CorrelationIdConstants.MaxLength);
        context.Response.Headers[CorrelationIdConstants.HeaderName].ToString()
            .Should().Be(context.TraceIdentifier);
    }

    private static CorrelationIdMiddleware CreateMiddleware(RequestDelegate next) =>
        new(next, NullLogger<CorrelationIdMiddleware>.Instance);

    private sealed class TestCorrelationIdAccessor : ICorrelationIdAccessor
    {
        public string? CorrelationId { get; set; }
    }
}
