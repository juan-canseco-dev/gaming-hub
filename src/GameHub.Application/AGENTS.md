# Application

- Read the root and `src/AGENTS.md` instructions.
- Organize use cases under `Features/<Feature>/Commands/<Action>` or `Queries/<Action>`, following the nearest existing slice. Some existing features use a shorter layout; do not reorganize them as incidental cleanup.
- Follow [ChatSendMessage](Features/Chats/Commands/SendMessage/Handler.cs): a named static partial feature class with nested sealed `Command`, `Handler`, and `Validator` types in separate files. Use the existing `ICommand`, `IQuery`, and handler interfaces; these use MediatR and `Result`.
- Access data through `IApplicationDbContext`. EF Core is deliberately referenced in this layer. Do not introduce repositories or use the concrete Infrastructure context here.
- Commands coordinate domain behavior, publish integration events when required, and save changes. Keep business invariants in Domain.
- Queries should be read-only: use `AsNoTracking`, filter and project in the database, then materialize. Preserve deterministic ordering and the tie-breakers represented in each cursor.
- Follow [GetMessagesByChat](Features/Chats/Queries/GetMessages/Handler.cs) and its explicit projection for query organization. Preserve `Limit + 1`, next-cursor construction, and malformed-cursor handling when changing pagination.
- Use `IAuthenticatedUserService` for caller identity and `IDateTimeProvider` for current UTC time. Do not accept a client-supplied user ID as proof of identity or authorization.
- Forward cancellation tokens to EF, MediatR, event publication, and notifier calls. Avoid sync-over-async.
- Expected business failures return `Result.Failure`. Input validation uses FluentValidation and the existing application `ValidationException` boundary.
- `ValidationBehavior<TRequest,TResponse>` currently constrains requests to `IBaseCommand`. Query validators are not automatically covered. For a query change, trace the execution path and test invalid inputs through the real pipeline; do not infer coverage from validator unit tests.
- Preserve behavior registration order (Logging, then Validation) unless changing it is intentional and tested. Do not invent a transaction pipeline behavior.
- When using the configured EF bus outbox, publish via the scoped `IPublishEndpoint` before `SaveChangesAsync` on the same context. Do not use `IBus` to bypass that scope.
- Consumers belong beside their feature. Use notifier abstractions from `Abstractions/Realtime` rather than concrete SignalR hub types. Consider retries and duplicate observable effects.
- Keep mapping changes focused: explicit projections are the demonstrated query pattern; AutoMapper references/helpers also exist.

Verification: relevant Application unit tests; API integration tests for changed HTTP behavior, query translation, authorization, or runtime validation.
