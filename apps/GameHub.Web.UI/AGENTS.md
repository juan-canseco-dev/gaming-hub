# Blazor WebAssembly UI

- Read the root instructions and shared .NET standard.
- Preserve Blazor WebAssembly and MudBlazor. Organize pages, models, services, and state under the existing feature folders.
- Follow existing `.razor`/`.razor.cs` separation for complex components; do not move business behavior into markup or reorganize simple components unnecessarily.
- Use feature service interfaces and the configured HTTP client with authorization/unauthorized interceptors. Keep server access out of display-only components.
- UI project references remain Contracts and Abstractions. Do not add Domain, Application, Infrastructure, EF Core, or server secrets to browser code.
- Keep `ApiSettings` API/hub URLs aligned with the environment. Everything shipped through `wwwroot` is browser-visible.
- Preserve loading, empty, error, retry, and pagination states. Keep incoming SignalR updates consistent with local message/member state and read counts.
- Dispose subscriptions, timers, cancellation sources, and component-owned resources. Do not dispose a shared hub connection from a component that only subscribes to it.
- Treat authorization state as a UI concern; the server must enforce permissions independently.
- Inspect HTTP error decoding when changing endpoints. Current client services read `Error` while the working API uses ProblemDetails in several paths; do not copy the mismatch as a standard.
- Match existing contracts, cursor fields, nullable presence values, event names, and hub method names. Coordinate contract changes with API and notifier code.
- Keep MudBlazor accessibility semantics, keyboard interaction, responsive layouts, and recoverable failures in scope for affected screens.

Verification: build the UI project and exercise the affected flow against the API when available. No dedicated UI test project exists in the baseline; report manual checks and any unavailable runtime verification accurately.
