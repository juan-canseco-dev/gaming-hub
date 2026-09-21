# GameHub: instructions for coding agents

## Start here

GameHub is a real-time, multi-channel chat application. Preserve its Clean Architecture boundaries and feature-oriented CQRS organization.

- Before code changes, read [the shared .NET standard](docs/engineering/dotnet-standard.md) and the relevant scoped instructions below. Read linked files explicitly; links are not imports.
- Before editing a file, check for additional `AGENTS.md` or `AGENTS.override.md` files on its directory path. Apply the instructions scoped to that file, even when the task starts at the repository root.
- Inspect `git status --short` and the relevant code and tests. Preserve existing user changes. Do not reset, overwrite, or reformat unrelated work.
- Treat `.csproj` files, registrations, and executable code as evidence of the current implementation. If these conflict with this guidance, explain the discrepancy and make only the change required by the task.

## Stack and boundaries

- Projects currently target `net9.0`, with nullable reference types and implicit usings enabled. Keep existing package versions unless an upgrade is part of the task.
- API: ASP.NET Core Minimal APIs through Carter. Application: MediatR 12, FluentValidation, EF Core 9 through `IApplicationDbContext`.
- Infrastructure: SQL Server, ASP.NET Core Identity/JWT, MassTransit/RabbitMQ with EF outbox/inbox, SignalR with Redis backplane.
- UI: Blazor WebAssembly and MudBlazor. Tests: xUnit, FluentAssertions, Moq/MockQueryable, WebApplicationFactory, Testcontainers, Respawn.
- Domain references Abstractions, not Application or Infrastructure. Infrastructure implements Application interfaces. The API is the composition root; the UI references only Contracts and Abstractions among the solution libraries.
- Keep `Result`/`Result<T>` and `Error` for expected failures; preserve the existing validation-exception boundary.
- Use the existing feature pattern: a named `public static partial class` containing `Command`/`Query`, `Handler`, and usually `Validator`, split across files.
- Prefer existing explicit LINQ projections for new queries. AutoMapper is already referenced and has helpers; do not remove it or add mapping infrastructure incidentally.
- Use `IAuthenticatedUserService` and `IDateTimeProvider` in handlers. Forward cancellation tokens through asynchronous I/O.

## Scoped instructions

| Work | Read before editing |
| --- | --- |
| Any shared library, DTO, or event contract | [src/AGENTS.md](src/AGENTS.md) |
| Domain behavior | [Domain](src/GameHub.Domain/AGENTS.md) |
| Use cases, validation, queries, consumers | [Application](src/GameHub.Application/AGENTS.md) |
| Persistence, Identity, messaging, hubs | [Infrastructure](src/GameHub.Infrastructure/AGENTS.md) |
| HTTP endpoints and startup | [API](apps/GameHub.Web.API/AGENTS.md) |
| Browser UI and client services | [UI](apps/GameHub.Web.UI/AGENTS.md) |
| Tests and fixtures | [Tests](tests/AGENTS.md) |

## Commands from the repository root

```powershell
dotnet restore GameHub.sln
dotnet build GameHub.sln --no-restore
dotnet test tests/GameHub.Domain.UnitTests/GameHub.Domain.UnitTests.csproj --no-restore
dotnet test tests/GameHub.Application.UnitTests/GameHub.Application.UnitTests.csproj --no-restore
```

Integration tests require a running Docker engine that supports the SQL Server and Redis container images:

```powershell
dotnet test tests/GameHub.Web.API.IntegrationTests/GameHub.Web.API.IntegrationTests.csproj --no-restore
```

Use [the developer guide](docs/codex/guide.md) for local services, runtime requirements, migrations, focused tests, and setup. Starting the API in Development or Docker applies migrations and seeds data; first verify that the configured database is the intended local database.

## Work and completion

- Keep changes focused. Follow neighboring C# style; do not impose repository-wide formatting or rename established feature types during unrelated work.
- Verify authentication and resource-level authorization separately. A route requiring a token does not establish chat membership or ownership.
- Preserve HTTP routes, error codes, DTOs, event names, cursor semantics, and SignalR contracts unless the requested change includes their consumers.
- Check validator execution: the current MediatR validation behavior only covers `IBaseCommand`; a query validator file alone does not establish runtime validation.
- Preserve the scoped outbox transaction: command handlers publish through `IPublishEndpoint` before saving their shared context. Do not replace this with direct broker or hub calls.
- For behavior changes, add or update relevant tests and run the affected suites. Use SQL Server integration tests for query translation and persistence behavior. For documentation-only changes, validate links, paths, and accuracy; application tests are unnecessary.
- Keep credentials, tokens, personal data, and local configuration values out of prompts, logs, and committed instructions. Do not copy development credentials into another project.
- Finish with the change, verification commands and results, and any checks not run. Never describe a skipped or blocked check as passed.

## Code Review Rules

Prioritize behavior regressions, authorization gaps, broken client/event contracts, EF translation, transaction/outbox ordering, duplicate side effects, and missing validation coverage. Explain actionable findings with the triggering scenario and affected file. Distinguish existing defects from defects introduced by the change.
