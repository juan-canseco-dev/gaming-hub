# Shared libraries

Apply the root instructions and [shared standard](../docs/engineering/dotnet-standard.md).

- Keep the project-reference direction documented in [the GameHub architecture map](../docs/engineering/gamehub-reference.md). Do not introduce circular references or dependencies from Domain into Application/Infrastructure.
- `GameHub.Abstractions`: small framework-independent primitives (`Result`, `Error`, pagination, enumeration, value objects). Do not turn it into a container for unrelated shared services.
- `GameHub.Contracts`: transport DTOs, notifications, and cursor contracts; it references Abstractions. Do not expose EF entities or add server-only dependencies.
- `GameHub.EventBus.Contracts`: integration-event payloads shared by producers and consumers. Keep messages independent of persistence and transport implementations.
- Treat property names, nullability, timestamps, event identity, and cursor encoding as compatibility decisions. Inspect API, UI, consumers, and tests before changing them.
- Keep domain terminology consistent: Channel categorizes chats; Chat holds messages and members; ChatMember is membership/read state; UserProfile is the domain-facing profile; UserPresence tracks presence. Identity's `ApplicationUser` belongs in Infrastructure.
- Use the deeper instructions in Domain, Application, and Infrastructure when editing those projects.
