# Infrastructure

- Read the root and `src/AGENTS.md` instructions.
- Implement Application interfaces here and register implementations in [DependencyInjection.cs](DependencyInjection.cs). Keep SQL Server, Identity, RabbitMQ, Redis, and SignalR details out of Domain.
- Use [ApplicationDbContext](Data/ApplicationDbContext.cs), which extends IdentityDbContext and implements `IApplicationDbContext`. Preserve assembly-scanned `IEntityTypeConfiguration<T>` mappings and Identity's base model configuration.
- Put mappings in `Data/Configurations`; preserve schemas, relationships, deletion behavior, indexes, domain length limits, and identifier generation strategies.
- Generate EF migrations in `Data/Migrations` using the API startup project and `ApplicationDbContext`. Review both generated migration and model snapshot; do not rewrite already-applied migrations casually.
- Verify the target environment before applying a migration or running seeders. Local API startup in Development/Docker migrates automatically. Do not apply to shared/production databases without authorization for that environment.
- Preserve `AddEntityFrameworkOutbox<ApplicationDbContext>`, SQL Server bus outbox, endpoint consumer outbox configuration, and the inbox/outbox entities. Do not describe these as a guarantee of exactly-once SignalR delivery.
- Keep integration events in EventBus.Contracts; HTTP and SignalR payloads belong in Contracts. Keep notifier interfaces in Application and SignalR implementations here.
- Preserve correlation propagation across publish/consume filters and logs. Log identifiers and error codes rather than credentials or message bodies.
- Maintain JWT validation and server-side authorization. Test-only authentication shortcuts must stay in the test host.
- Keep production and development seeding separate. Existing demo/system-user credentials are not a company security standard and must not be copied into new projects.
- Use configuration providers for environment-specific values. Do not embed connection strings or secrets in new source or instruction files.

Verification: SQL Server integration tests for mapping, constraints, and query changes; inspect migrations separately because the current integration fixture uses `EnsureCreatedAsync`, not migrations.
