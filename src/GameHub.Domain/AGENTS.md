# Domain

- Read the root and `src/AGENTS.md` instructions.
- Keep Domain dependent only on the existing `GameHub.Abstractions` project. No EF Core, MediatR, MassTransit, ASP.NET Core, or UI references here.
- Put business invariants in domain methods and factories. Preserve encapsulated mutation, private constructors/setters where used, and read-only collections.
- Return existing `Result`/`Result<T>` and domain error definitions for expected failures. Do not use exceptions for ordinary business rejection.
- Accept time as an argument for time-dependent behavior; application handlers obtain it from `IDateTimeProvider`.
- Match existing identifier ownership. `Chat.AddMessage` and `Chat.Join` allocate message IDs using `Guid.CreateVersion7()`; persistence generates some other entity IDs. Do not globally replace one strategy with the other.
- Reuse domain constants in validators and EF configurations. `Chat.MaxMessageLength` and `Chat.MaxPreviewLength` are sources of truth for their limits.
- Preserve aggregate state transitions, including preview, last-message ID, and timestamp updates.
- Do not add dependency injection registration, HTTP status codes, persistence mapping, or event publication to entities.
- Add focused Domain unit tests for changed invariants, boundary values, returned errors, and state after success/failure.

Reference: [Chat.cs](Chats/Chat.cs), [MessagePreviewService.cs](Chats/MessagePreviewService.cs), and [domain tests](../../tests/GameHub.Domain.UnitTests).
