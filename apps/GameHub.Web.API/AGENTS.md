# Web API

- Read the root instructions and shared .NET standard.
- Implement HTTP routes as Carter `ICarterModule` classes in `Endpoints/<Feature>`. Bind transport inputs, dispatch through `IMediator`, and map results to HTTP. Keep business logic and database access in the appropriate inner layers.
- Follow [SendMessage](Endpoints/Chats/SendMessage.cs) for endpoint organization. Keep route paths, route names, tags, authorization, and response metadata consistent with behavior.
- Preserve existing paths, including the current singular/plural `chat`/`chats` differences, until an explicit contract change updates clients and tests together.
- Require authentication where the use case needs a caller. Verify resource-level authorization in the handler/hub separately from `.RequireAuthorization()`.
- Expected application/domain failures are `Error` values. The current working tree maps them through [ToProblem](Extensions/ErrorHttpResultExtensions.cs); preserve the agreed status, machine-readable code, detail, and correlation fields.
- Validation exceptions and unexpected exceptions are handled by [ExceptionHandlingMiddleware](Middleware/ExceptionHandlingMiddleware.cs). Do not expose stack traces or internal exception messages in public error responses.
- The ProblemDetails conversion is a working-tree change at the documentation baseline. Inspect current API tests and UI deserialization before extending it; older client paths still deserialize `Error` directly.
- Forward endpoint cancellation tokens to MediatR. Keep HTTP concerns out of Application and Domain.
- Preserve composition and middleware ordering in `Program.cs`, especially correlation before downstream logging/error handling and authentication before authorization.
- Read startup code before running the host: Development/Docker applies migrations; non-IntegrationTesting environments run seeders.
- OpenAPI is present; Scalar is not part of the inspected setup. Do not add documentation packages incidentally.

Verification: relevant API integration tests for success, validation, missing resource, unauthenticated/unauthorized callers, and error response shape. Match tests to the actual route contract rather than inventing universal status mappings.
