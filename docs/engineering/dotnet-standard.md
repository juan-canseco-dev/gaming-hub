# Shared .NET engineering standard

Baseline version: 1.0, 2026-09-21. Reference implementation: GameHub.

This is the reusable coding and architecture baseline requested for company projects. It defines expectations for new work; it is not a claim that every existing GameHub path already satisfies them. Existing behavior, gaps, and adaptation notes are recorded in [the GameHub reference](gamehub-reference.md). Adopt this file in each repository through its root `AGENTS.md`, with an explicit instruction to read it before code changes.

## Architecture and dependency direction

Use a layered application with feature-oriented use cases. Keep domain behavior independent of delivery mechanisms, persistence, and external services so it can be tested without starting a host.

| Layer | Responsibility | Permitted direction within the solution |
| --- | --- | --- |
| Abstractions | Small shared primitives and result/pagination types | No application-specific outer layers |
| Domain | Business entities, invariants, value objects, domain errors | Abstractions |
| Contracts | Public DTOs, notifications, transport contracts | Abstractions when needed |
| EventBus.Contracts | Integration event schemas, when messaging exists | Independent of concrete infrastructure |
| Application | Commands, queries, handlers, validation, ports, consumers | Domain, Contracts, EventBus.Contracts and their primitives |
| Infrastructure | Persistence and external service implementations | Application and its dependencies |
| API/host | HTTP boundary and composition | Infrastructure/Application as needed for wiring |
| UI/client | Presentation and client orchestration | Contracts and Abstractions |

These are responsibility boundaries. A project that has no browser UI or message broker need not create empty projects or add unused dependencies. Preserve the same dependency principles when adapting the structure.

In the GameHub pattern, Application intentionally depends on EF Core interfaces/types and on MediatR/MassTransit. Do not claim this is a framework-free Application layer. Domain remains free of those frameworks. Do not add a generic repository layer over the existing `IApplicationDbContext` pattern without a demonstrated requirement and an explicit architecture decision.

## Use cases and C# conventions

Organize each use case in a feature folder. GameHub's reference pattern is a named static partial class with nested request, handler, and validator types separated into `Command.cs` or `Query.cs`, `Handler.cs`, and `Validator.cs`. Use `ICommand<T>`, `IQuery<T>`, and their handler interfaces over MediatR. Keep the nearest existing slice's naming consistent rather than introducing a second naming scheme.

Use records for immutable requests when consistent with the contract. Do not mechanically convert every existing DTO to a record. Keep nullable reference types enabled, model optional values explicitly, and avoid silencing null warnings without understanding the invariant. Prefer file-scoped namespaces for new ordinary C# files; match existing host/legacy files when editing them. Preserve constructor injection and existing guard conventions. Primary constructors are an option where already appropriate, not a requirement to rewrite code.

Keep asynchronous I/O asynchronous end to end. Forward `CancellationToken` through APIs that accept it. Avoid `.Result`, `.Wait()`, and unnecessary `Task.Run` around I/O. Obtain time and current user identity through Application abstractions, and pass time into Domain operations for deterministic tests.

Put invariants in Domain methods, orchestration in Application handlers, and HTTP mapping in endpoints. Request validation and domain validation serve different purposes: input shape can be invalid before a use case starts, while business invariants must also hold when the domain is called elsewhere.

## Results, validation, and HTTP

Use `Result`/`Result<T>` and named `Error` definitions for anticipated business failures. Keep machine-readable error codes stable. Use exceptions for unexpected faults and the established validation-exception boundary; a blanket ban on all exceptions would conflict with GameHub.

Use FluentValidation for request validation, and verify that the runtime pipeline actually executes each validator. Registration and direct validator tests alone are insufficient. Add a pipeline or HTTP test for validation wiring when it changes.

Expose use cases through thin Carter Minimal API endpoints. Keep routes, authentication requirements, tags, and documented responses aligned with actual behavior. Choose status codes per contract and preserve them unless deliberately migrating clients. New HTTP error contracts should use a consistent ProblemDetails shape with a machine-readable code and correlation identifier; coordinate this with clients. Do not expose domain entities, exception internals, or authentication secrets.

Authenticate callers and independently authorize access to the requested resource. A valid JWT does not prove membership or ownership. Derive caller identity on the server rather than trusting a user ID in a request body. Include denied-access cases in relevant tests.

## Persistence and queries

Application uses `IApplicationDbContext`; Infrastructure supplies the concrete EF context and provider. Keep mappings in separate `IEntityTypeConfiguration<T>` classes. Use domain constants for shared limits and database constraints where needed to protect invariants under concurrent requests.

Read queries should filter, order, and project before materialization and use no tracking when they do not mutate entities. Prefer explicit projections like GameHub's message queries. Preserve existing AutoMapper usage when touching it; adoption of this standard is not authorization to remove dependencies or rewrite mappings.

Use deterministic cursor pagination for feeds. The sort keys, tie-breakers, predicates, and encoded cursor must agree. Test equal timestamps, empty pages, malformed cursors, and page boundaries. A mocked IQueryable cannot prove provider translation or SQL Server ordering behavior.

Generate migrations with the correct context and startup project, review the schema diff and snapshot, and validate upgrades on a disposable database when schema changes. Apply shared/production migrations only under the project's deployment process. Never treat a test using `EnsureCreated` as migration coverage.

## Messaging and real-time behavior

When messaging is required, keep integration events separate from HTTP DTOs. Use the configured scoped EF bus outbox so database changes and outgoing events share the persistence boundary. In GameHub's command pattern, publish through `IPublishEndpoint` before saving the same scoped context. Verify registrations as well as handler code before relying on that guarantee.

Consumers orchestrate through Application ports; Infrastructure implements SignalR notifiers. Design externally observable effects for retries and duplicates. Broker delivery, database inbox deduplication, and browser notification delivery have different guarantees; do not promise exactly-once delivery to users merely because an inbox exists.

Preserve correlation IDs across HTTP, publication, consumption, and logs. Use structured logs with useful identifiers and error codes. Avoid logging raw credentials, tokens, message bodies, or whole sensitive request objects.

## Frontend

For Blazor projects, keep HTTP access in feature services and complex presentation behavior in component code-behind. Use shared transport contracts rather than server implementation references. Keep loading, empty, failure, retry, and disposal paths explicit. Preserve keyboard operation and accessible component semantics.

Treat browser configuration as public. Keep token handling within the established auth services, but do not treat an existing token-storage choice as universally appropriate for every application. Client-side visibility checks never replace server authorization.

## Verification and maintenance

Use Domain unit tests for invariants, Application unit tests for orchestration and failures, and API integration tests for the HTTP/persistence boundary. Follow the existing xUnit, FluentAssertions, Moq, MockQueryable, Testcontainers, and Respawn conventions where those components are present. Add regression coverage for behavior changes rather than tests that merely reproduce the implementation.

Report the exact checks performed and distinguish application failures from unavailable Docker, SDKs, package feeds, or permissions. Documentation-only work needs documentation validation, not an unrelated test run. A successful build cannot replace behavioral checks; mocked queries cannot replace database checks.

Keep framework and dependency versions in project files. Preserve versions during ordinary feature work. For a new project, select supported versions through the team's dependency/lifecycle review instead of freezing GameHub's .NET 9 versions into a permanent company rule.

Use `.editorconfig`, analyzers, formatting checks, architecture tests, and CI for rules that require mechanical enforcement. This baseline introduces Markdown guidance only; it does not install those controls. GameHub has no checked-in `.editorconfig`, `global.json`, or CI workflow at this baseline. Add them as explicit engineering work when the team decides their contents.

## Adopting changes and exceptions

Copy this versioned standard into each adopting repository, use the [root template](../codex/templates/project-AGENTS.template.md), and write a project-specific map and commands. Keep shared rules synchronized through reviewed updates, not a dependency on one developer's home directory.

Record intentional deviations in a short architecture decision: context, selected approach, alternatives, consequences, affected projects, and verification. Routine implementation choices within these boundaries do not need a new approval step. A task that explicitly requests an architecture change can make it concrete and update the decision record as part of that work.

When a production fix reveals a repeatable mistake, update the narrowest relevant instruction and an executable check where practical. Remove stale rules instead of appending contradictory ones indefinitely.
