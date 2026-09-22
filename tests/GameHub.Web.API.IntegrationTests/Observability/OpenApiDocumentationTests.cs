using System.Net;
using System.Text.Json.Nodes;
using FluentAssertions;
using GameHub.Application.Abstractions.Observability;
using GameHub.Application.Features.Chats.Consumers;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GameHub.Web.API.IntegrationTests.Observability;

public sealed class OpenApiDocumentationTests
{
    [Fact]
    public async Task SwaggerUi_ShouldReferenceGeneratedOpenApiDocument()
    {
        using var factory = new OpenApiWebApplicationFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/swagger/index.html");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("GameHub API");

        using var configurationResponse = await client.GetAsync("/swagger/index.js");
        configurationResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await configurationResponse.Content.ReadAsStringAsync())
            .Should().Contain("../openapi/v1.json");
    }

    [Fact]
    public async Task OpenApiDocument_ShouldDescribeApiConventionsAndAuthentication()
    {
        using var factory = new OpenApiWebApplicationFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var document = JsonNode.Parse(await response.Content.ReadAsStringAsync());
        document.Should().NotBeNull();
        document!["info"]!["title"]!.GetValue<string>().Should().Be("GameHub API");
        document["info"]!["version"]!.GetValue<string>().Should().Be("v1");
        document["components"]!["securitySchemes"]!["Bearer"]!["scheme"]!
            .GetValue<string>().Should().Be("bearer");

        var protectedOperation = document["paths"]!["/api/channels"]!["get"]!;
        protectedOperation["summary"]!.GetValue<string>().Should().Be("List channels");
        protectedOperation["security"]![0]!["Bearer"].Should().NotBeNull();
        protectedOperation["responses"]!["401"].Should().NotBeNull();
        protectedOperation["responses"]!["403"].Should().NotBeNull();
        protectedOperation["responses"]!["200"]!["headers"]![CorrelationIdConstants.HeaderName]
            .Should().NotBeNull();
        protectedOperation["parameters"]!.AsArray()
            .Select(parameter => parameter!["name"]!.GetValue<string>())
            .Should().Contain(CorrelationIdConstants.HeaderName);

        var schemas = document["components"]!["schemas"]!.AsObject();
        var schemaNames = schemas.Select(schema => schema.Key).ToArray();
        schemaNames.Should().Contain(
        [
            "JoinChatRequest",
            "MessageAuthorDto",
            "SendMessageRequest",
            "UserDto"
        ]);
        schemaNames.Should().NotContain(["Command", "Command2", "UserDto2"]);
        schemas["MessageDto"]!["properties"]!["user"]!["$ref"]!
            .GetValue<string>().Should().Be("#/components/schemas/MessageAuthorDto");
        schemas["MessageAuthorDto"]!["properties"]!["presence"].Should().NotBeNull();

        var anonymousOperation = document["paths"]!["/api/identity/auth"]!["post"]!;
        anonymousOperation["security"].Should().BeNull();
        document["paths"]!["/"].Should().BeNull();
    }

    private sealed class OpenApiWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("IntegrationTesting");
            builder.UseSetting(
                "ConnectionStrings:ConnectionString",
                "Server=localhost;Database=GameHub;User Id=sa;Password=unused;Encrypt=False");
            builder.UseSetting("ConnectionStrings:Redis", "localhost:6379");
            builder.UseSetting("Jwt:SecretKey", "3bc3ecfd4d6d0855e7df9b43b452ebcfab80b1ae368873721e7b6e0fe70e1756");
            builder.UseSetting("Jwt:Issuer", "https://test-issuer");
            builder.UseSetting("Jwt:Audience", "https://test-audience");
            builder.UseSetting("Cors:PolicyName", "TestCorsPolicy");
            builder.UseSetting("Cors:AllowedOrigins:0", "https://localhost:5001");
            builder.UseSetting("EventBusSettings:Host", "amqp://localhost:5672");
            builder.UseSetting("EventBusSettings:Username", "guest");
            builder.UseSetting("EventBusSettings:Password", "guest");

            builder.ConfigureServices(services =>
            {
                services.AddMassTransitTestHarness(configuration =>
                {
                    configuration.AddConsumer<ChatMessageSentConsumer>();
                    configuration.AddConsumer<ChatMemberJoinedConsumer>();
                });
            });
        }
    }
}
