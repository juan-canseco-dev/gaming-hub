# PROJECT_NAME: coding-agent instructions

This file is a template, not GameHub's active root instructions. When adopting it, copy its contents to the new repository's root `AGENTS.md`, remove this paragraph, replace every placeholder, and verify all paths and commands.

## Purpose and sources

PROJECT_PURPOSE: one sentence describing users, business capabilities, and key terminology.

- Before code changes, read `docs/engineering/dotnet-standard.md` (shared standard version SHARED_STANDARD_VERSION) and `docs/engineering/project-reference.md` (this project's map, examples, and exceptions).
- Before editing, inspect the applicable `AGENTS.md`/`AGENTS.override.md` files along the target file's directory path. Explicitly read scoped files when working from the root.
- Check `git status --short`; preserve existing user changes. Inspect an analogous feature and its tests before designing a new pattern.
- Report conflicts between these instructions and the code. Resolve them within the requested task; avoid silently spreading existing defects.

## Stack and project map

TARGET_FRAMEWORK_AND_SDK_POLICY: fill from project files and any global.json.

PACKAGES_AND_SERVICES: identify the actual persistence provider, messaging transport, auth, UI, and testing stack. Do not add unused services just to match GameHub.

PROJECT_PATH_MAP: replace with existing paths for Domain, Application, Infrastructure, Contracts, Abstractions, optional EventBus.Contracts, API, optional UI, and tests.

SCOPED_INSTRUCTION_PATHS: list and link the actual layer-level AGENTS.md files copied/adapted for this repository.

## Shared architecture

- Domain owns business invariants and depends only on approved framework-independent primitives.
- Application owns feature-oriented CQRS, request validation, ports, and orchestration. Infrastructure implements external concerns. HTTP endpoints stay thin.
- Use the adopted GameHub-style nested partial feature classes, MediatR request/handler abstractions, Result/Error boundary, and EF context port. Document intentional deviations in the project reference.
- Queries filter/project before materializing. Validate requests through their real runtime path; preserve deterministic pagination.
- Obtain caller identity/time through abstractions and forward cancellation tokens.
- Enforce server-side resource authorization. Preserve public HTTP, DTO, event, and real-time contracts or migrate all affected consumers together.
- If messaging is present, preserve the configured transaction/outbox boundary and consider duplicate effects.
- Keep clients independent of server implementation projects. Browser-delivered configuration is public.
- Match adjacent code style and use the repository's formatting/analyzer configuration. Avoid incidental rewrites and dependency upgrades.

## Verified commands

Replace the following entries with commands executed from this repository's root:

- Restore: RESTORE_COMMAND
- Build: BUILD_COMMAND
- Unit tests: UNIT_TEST_COMMANDS
- Integration tests: INTEGRATION_TEST_COMMAND and required services
- Local run: RUN_COMMANDS and their startup/database side effects
- Migrations, if relevant: MIGRATION_COMMAND with context, startup project, output directory, and safe target requirements
- Formatting/analyzers, if configured: FORMAT_CHECK_COMMAND

Do not label commands verified until tested in the intended environment. State any package feed, runtime, Docker, migration, or cloud limitations.

## Completion and review

- Run checks appropriate to the change; documentation-only changes require link/path/content verification.
- Add relevant regression coverage for behavior changes. Mocked query tests do not establish database-provider behavior.
- Keep secrets and personal data out of instructions, source, and logs. Do not copy reference-project credentials.
- Finish with the result, verification evidence, and remaining blockers. Update scoped guidance when a durable convention changes.

## Code Review Rules

Prioritize authorization, correctness, data integrity, contract compatibility, validation wiring, query translation, and transaction/message behavior. Give actionable findings with triggering scenarios. Distinguish pre-existing problems from regressions.
