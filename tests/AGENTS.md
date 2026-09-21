# Tests

- Read the root instructions and the scoped guidance for the production layer being tested.
- Use the existing xUnit and FluentAssertions setup. Follow neighboring descriptive names such as `Handle_Should_ReturnFailure_WhenChatDoesNotExist` and Arrange/Act/Assert structure.
- Domain tests verify business invariants and state transitions without infrastructure.
- Application tests use Moq and MockQueryable.Moq where appropriate. Verify returned errors and meaningful side effects, including no save/publication after rejection. Use controllable time through `IDateTimeProvider`.
- MockQueryable verifies handler logic; it does not establish SQL Server translation, collation, constraints, or transaction behavior. Use integration tests for those concerns.
- Reuse [CustomWebApplicationFactory](GameHub.Web.API.IntegrationTests/Abstractions/CustomWebApplicationFactory.cs) and [SharedTestCollection](GameHub.Web.API.IntegrationTests/Abstractions/SharedTestCollection.cs). They start SQL Server and Redis containers and configure a MassTransit test harness.
- Integration tests need Docker even for a filtered HTTP test. The current factory does not start a RabbitMQ container; the harness does not prove production broker/outbox delivery behavior.
- Preserve the shared fixture lifecycle, database reset through Respawn, scope disposal, and test isolation. Do not parallelize tests that reset the same database or mutate shared authentication state.
- The fixture uses `EnsureCreatedAsync`. Passing these tests does not prove that migrations apply or upgrade existing data correctly.
- Test JWT validation is deliberately relaxed in the factory. These tests do not prove production issuer, audience, signature, or lifetime validation.
- Test validators directly for rules and through the request pipeline for wiring. Query validators currently fall outside the command-only validation behavior.
- Cover relevant boundaries: invalid IDs/cursors/limits, missing resources, membership/ownership, unauthenticated calls, repeated operations, and pagination ties.
- For error contract changes, verify status and payload fields using the current response format. Inspect existing working-tree ProblemDetails helpers before adding alternatives.

## Commands from repository root

```powershell
dotnet test tests/GameHub.Domain.UnitTests/GameHub.Domain.UnitTests.csproj
dotnet test tests/GameHub.Application.UnitTests/GameHub.Application.UnitTests.csproj
dotnet test tests/GameHub.Application.UnitTests/GameHub.Application.UnitTests.csproj --filter FullyQualifiedName~SendMessageHandlerTests
dotnet test tests/GameHub.Web.API.IntegrationTests/GameHub.Web.API.IntegrationTests.csproj --filter FullyQualifiedName~SendMessageTests
```

Use `--no-restore` after a successful restore. Use `--no-build` only when the same configuration has already built the current test code. Report passed, failed, and not-run checks separately; infrastructure failures are not assertions about application correctness.
