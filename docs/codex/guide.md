# Using ChatGPT and Codex for .NET development with GameHub

Researched and checked against the repository on 2026-09-21.

Use GameHub's root [AGENTS.md](../../AGENTS.md) to guide coding work, the [shared .NET standard](../engineering/dotnet-standard.md) to keep projects consistent, and this guide to onboard developers. The files are ready for review in this repository; they do not change your personal Codex settings, install tools, or modify application code.

## 1. What we take from the Claude article

Mukesh Murugan's article makes a useful point: document the project's purpose, design constraints, and working commands instead of repeatedly explaining them in prompts. Its WHAT/WHY/HOW structure is a good onboarding device. This guide uses that idea, but its instructions and examples are derived from GameHub and verified OpenAI documentation. The article's product-specific memory, import, and configuration mechanisms should not be assumed to work in Codex. [Original article](https://codewithmukesh.com/blog/claude-md-mastery-dotnet/).

For GameHub, WHAT is a .NET real-time chat application, WHY is independently testable domain behavior with feature-oriented orchestration, and HOW is the actual solution layout, conventions, and commands below. These three questions make useful headings when onboarding another repository.

Do not copy the article's sample stack: GameHub targets .NET 9, uses SQL Server and MediatR, and includes Carter, MassTransit, and Blazor. AutoMapper is referenced, although the inspected feature queries use explicit LINQ projections. The detailed [reference map](../engineering/gamehub-reference.md) links to evidence and explains current gaps.

## 2. Choose the right working surface

Codex can inspect a repository, change files, execute development tools, and iterate on their output. The CLI works in a selected directory; the IDE extension uses its workspace. Local project access gives the agent concrete code and command results to work with. Its answer still needs engineering review: a plausible explanation is not evidence that a build or test ran. [Codex CLI](https://learn.chatgpt.com/docs/codex/cli), [project/workspace behavior](https://learn.chatgpt.com/docs/projects).

Use ordinary ChatGPT conversations for discussions, tradeoffs, and reviewing supplied material. A ChatGPT project can share uploaded sources and project instructions across chats, but it does not automatically expose your computer's repository. Upload or connect the relevant sources. A file attached to a chat is a snapshot and can become stale. [Projects and chats](https://learn.chatgpt.com/docs/projects).

| Need | Suggested workflow for this team |
| --- | --- |
| Explain a design choice or challenge an API contract | ChatGPT with the relevant standards and source excerpts |
| Implement or debug a feature in GameHub | Codex against the local repository and real tests |
| Review a proposed change | Codex or ChatGPT with the actual diff, acceptance criteria, and relevant contracts |
| Work in an isolated environment | A separately configured checkout/cloud environment; verify .NET, package access, and Docker capabilities there |

The repository is the durable team record. Conversation history is useful working context, but decisions worth preserving belong in reviewed files and executable checks.

## 3. How Codex reads project instructions

Codex builds its startup instruction chain from its home directory, then from the repository root down to the working directory. Home defaults to `~/.codex` unless `CODEX_HOME` changes it. At each level, `AGENTS.override.md` takes precedence over `AGENTS.md`; configured fallback names come afterward. At most one file per directory is selected. Deeper instructions take precedence over earlier ones. The default combined project-document budget is 32 KiB (`project_doc_max_bytes`). [Instruction discovery](https://learn.chatgpt.com/docs/agent-configuration/agents-md).

Our repository rule additionally tells Codex to read the scoped guidance for every file it edits. A root-started task should not assume startup discovery has loaded every descendant instruction file. Links are navigation, so the root explicitly asks the agent to open them. This is our workflow convention, not a claim that Markdown links or Claude-style `@file` lines automatically import content.

Use ordinary `AGENTS.md` files for shared rules. Avoid adding overrides casually: an override replaces the same-directory instruction selection. After editing instructions, start a fresh session and verify which files it loaded. [Instruction discovery](https://learn.chatgpt.com/docs/agent-configuration/agents-md).

These files guide the agent; they cannot change its sandbox permissions, organization controls, or higher-priority platform instructions.

### Claude-to-Codex adaptation

| Concept to migrate | Approach used here |
| --- | --- |
| Repository instructions | Root `AGENTS.md` plus scoped files |
| Detailed architectural rules | A versioned shared standard that the root explicitly tells Codex to read |
| Personal preferences | Optional home-level guidance, kept separate from mandatory team rules |
| Path-specific guidance | Scoped instruction files plus an explicit root routing table |
| Imported documentation | Explicit read instructions and normal links; no assumed import syntax |
| Automatic recollection | Helpful local memories when enabled; never the only copy of team policy |
| Repeatable task procedures | Optional skills; baseline instructions remain in AGENTS.md |
| Enforcement | Compiler, tests, analyzers, CI, and review; Markdown alone is insufficient |

## 4. Files supplied with this setup

```text
AGENTS.md
src/AGENTS.md
src/GameHub.Domain/AGENTS.md
src/GameHub.Application/AGENTS.md
src/GameHub.Infrastructure/AGENTS.md
apps/GameHub.Web.API/AGENTS.md
apps/GameHub.Web.UI/AGENTS.md
tests/AGENTS.md
docs/engineering/dotnet-standard.md
docs/engineering/gamehub-reference.md
docs/codex/guide.md
docs/codex/templates/project-AGENTS.template.md
docs/codex/templates/chatgpt-project-instructions.md
```

The root is a concise operational entry point. Scoped files hold layer-specific instructions. The shared standard carries cross-project requirements. The reference map records observed facts and known gaps. Templates are ordinary Markdown until deliberately copied or supplied to another surface; they do not install themselves.

For a first task, read the root and the instructions for the affected layer, then open an existing feature and its tests. The developer guide is human onboarding material, so agents do not need to reread every section for every small code change.

## 5. First-time setup for a developer

### Open the correct checkout

Open the GameHub folder in Codex desktop or your IDE workspace. If using the CLI, install it using the official instructions, run `codex` from the repository root, and use the supported sign-in flow. CLI commands such as `/status`, `/permissions`, `/model`, and `/review` expose session controls. `/init` can generate starter instructions, but this repository already has curated files; review any proposed replacement carefully. [CLI setup and commands](https://learn.chatgpt.com/docs/codex/cli).

Start with this prompt:

```text
Read AGENTS.md and the instructions relevant to an API + Application change.
List the instruction files you actually read, summarize the dependency rules,
identify the command/query validation distinction, and list the checks you
would run. Do not edit files or start services.
```

For a CLI installation, this explicit working-directory/read-only invocation is useful:

```powershell
codex --cd . --sandbox read-only --ask-for-approval on-request "Read AGENTS.md and its relevant scoped instructions. Summarize the rules for an API and Application change. Do not edit files or start services."
```

The read-only sandbox and approval policy are runtime controls; Markdown is not their replacement. Workspace-write permits work inside the configured workspace, with approval behavior controlled separately. Organization policy can constrain these settings. Use the account's approved controls rather than disabling them to get a restore or test to run. [Agent approvals and security](https://learn.chatgpt.com/docs/agent-approvals-security).

### Optional personal settings

Codex user settings live in `~/.codex/config.toml`; trusted projects can supply `.codex/config.toml`. Project trust and managed requirements affect which settings apply. This documentation deliberately creates neither file, avoiding changes to your chosen model, approvals, or account behavior. [Configuration basics](https://learn.chatgpt.com/docs/config-file/config-basic).

If your team uses a personal configuration, a minimal example to review with its managed settings is:

```toml
# Example only; not installed by this guide.
sandbox_mode = "workspace-write"
approval_policy = "on-request"
```

For personal response preferences, an optional home-level `AGENTS.md` might say:

```markdown
# Personal working preferences

- Explain the outcome and verification briefly.
- Preserve unrelated working-tree changes.
- Report checks that could not run and why.
- Use the current repository's architecture instructions and commands.
```

Keep mandatory company standards in the repository so a teammate or CI agent does not depend on your personal home directory. Do not overwrite existing home-level settings just to adopt this guide.

### Check the .NET environment

Run from the GameHub root:

```powershell
git status --short
dotnet --info
dotnet --list-sdks
dotnet --list-runtimes
dotnet restore GameHub.sln
dotnet build GameHub.sln --no-restore
```

All projects target `net9.0`. Use an SDK capable of building that target and the required .NET/ASP.NET Core 9 runtimes for local execution. Do not retarget the solution just because a newer SDK is installed. There is no `global.json` at this baseline. SDK pinning and framework upgrades should be explicit team decisions.

Restore needs access to the configured package sources. If access fails, retain the failure and distinguish it from a compiler error. Do not describe the project as broken simply because the agent environment cannot reach NuGet.

### Run the API and UI

Use the existing [README setup](../../README.md) to configure local SQL Server, Redis, RabbitMQ, JWT, and CORS values. Keep credentials in approved local configuration rather than copying them into this guide or prompts. Ensure API and hub URLs match the UI's `ApiSettings`. There is no `UserSecretsId` in the inspected API project; do not assume user-secrets are already initialized or that a particular provider wins over the explicit JSON configuration calls in `Program.cs`.

After checking the target database, start each host in its own terminal:

```powershell
dotnet run --project apps/GameHub.Web.API/GameHub.Web.API.csproj
```

```powershell
dotnet run --project apps/GameHub.Web.UI/GameHub.Web.UI.csproj
```

At the baseline, launch settings use `https://localhost:7058` for the API and `http://localhost:7238` for the UI. Use the actual launch output if configuration changes. A local HTTPS certificate must be usable by the client.

Alternatively, after populating the README's `.env` variables and checking available ports:

```powershell
docker compose -f docker-compose.yml -f docker-compose.override.yml up --build
```

This starts the application and backing services. API startup in Development/Docker applies EF migrations; non-IntegrationTesting startup runs seeders. These are real database writes. Docker-backed development and integration tests are different setups: the test factory creates its own SQL Server and Redis containers.

## 6. A repeatable coding workflow

Give Codex a concrete outcome, constraints, and observable acceptance criteria. Ask it to inspect an analogous use case, trace affected contracts, implement the smallest coherent change, and verify it. For broad work, request a short plan first; for a focused bug, provide the failing scenario and expected behavior.

Use this sequence as a team habit:

1. Check the working tree and identify existing changes that must be preserved.
2. Read relevant instructions, one comparable feature, and its tests.
3. Trace the change across Domain, Application, persistence, HTTP, events, and UI as applicable.
4. Resolve missing business requirements before implementing dependent behavior; continue independent investigation.
5. Implement with relevant regression coverage.
6. Run focused checks, then the affected broader suite if appropriate.
7. Inspect the diff and report the result, verification evidence, and remaining limitations.

### Example: diagnose a defect

```text
Investigate why invalid message-page limits can reach the query handler.
Read the query validator, MediatR registrations, endpoint, and relevant tests.
Determine whether the validator executes during an HTTP request. Implement a
focused fix with a regression test through the real request path. Preserve
existing route and response contracts, or explain any required contract change.
Run the relevant unit and integration tests; report unavailable prerequisites.
```

This prompt distinguishes a validator's rule definition from its runtime execution, a real concern in the inspected repository.

### Example: implement a new feature

```text
Add message editing using GameHub's existing architecture. A user may edit only
their own non-system message. Use the current message length limit. Define and
test the response for a missing message and a forbidden edit, following existing
contract conventions. Trace effects on the last-message preview and real-time UI.
First identify any missing product decisions, then implement the authorized
behavior across the affected layers. Preserve unrelated changes and report checks.
```

The feature is an example, not implemented by this documentation. A senior developer should still decide unresolved product details, such as edit windows or audit history, when they matter.

### Example: review

```text
Review this diff against AGENTS.md and the shared .NET standard. Focus on
authorization, validation execution, API/client compatibility, query translation,
pagination boundaries, and outbox ordering. List only actionable findings with
file locations and failure scenarios. Separate regressions from existing defects.
Do not change files.
```

### Example: continue long work

```text
Read the current diff and the task notes. Confirm the current objective, completed
changes, unresolved decisions, and remaining checks against the actual files.
Continue the unfinished work without repeating completed steps.
```

For a long task, keep a small reviewed task note with scope, decisions, verification, and next steps. Do not put temporary progress logs into permanent architecture instructions.

## 7. Verification commands and what they establish

The commands below build the test projects as needed. Add `--no-restore` after a successful restore. Add `--no-build` only after the current code was built in the same configuration.

```powershell
dotnet test tests/GameHub.Domain.UnitTests/GameHub.Domain.UnitTests.csproj
dotnet test tests/GameHub.Application.UnitTests/GameHub.Application.UnitTests.csproj
dotnet test tests/GameHub.Application.UnitTests/GameHub.Application.UnitTests.csproj --filter FullyQualifiedName~SendMessageHandlerTests
```

With Docker running and compatible SQL Server/Redis images available:

```powershell
docker info
dotnet test tests/GameHub.Web.API.IntegrationTests/GameHub.Web.API.IntegrationTests.csproj
dotnet test tests/GameHub.Web.API.IntegrationTests/GameHub.Web.API.IntegrationTests.csproj --filter FullyQualifiedName~SendMessageTests
```

To exercise all projects, after the prerequisites are satisfied:

```powershell
dotnet test GameHub.sln
```

| Check | Useful evidence | Does not establish |
| --- | --- | --- |
| Build | Compilation and reference compatibility | Correct business behavior |
| Domain unit tests | Invariants and state changes | HTTP/persistence correctness |
| Application unit tests | Handler decisions and meaningful effects | SQL Server translation or constraints |
| Existing API integration tests | HTTP behavior against test infrastructure | Production broker delivery, strict JWT validation, migration upgrades |
| UI build and manual flow | Compilation and observed browser behavior | Comprehensive automated UI coverage |

The integration factory uses SQL Server and Redis Testcontainers, Respawn, and a MassTransit test harness. It creates the database with `EnsureCreatedAsync`, relaxes JWT validation, and does not create a RabbitMQ container. Those are test design facts, not guarantees about production. See [test instructions](../../tests/AGENTS.md).

For an example schema change, with a compatible `dotnet-ef` tool available and the required startup configuration set, use the existing project paths:

```powershell
dotnet ef --version
dotnet ef migrations add AddMessageEditTimestamp --project src/GameHub.Infrastructure/GameHub.Infrastructure.csproj --startup-project apps/GameHub.Web.API/GameHub.Web.API.csproj --context ApplicationDbContext --output-dir Data/Migrations
```

`AddMessageEditTimestamp` is an example migration name, not a requested change. Review the generated migration and snapshot before applying it. For an explicitly selected disposable/local database:

```powershell
dotnet ef database update --project src/GameHub.Infrastructure/GameHub.Infrastructure.csproj --startup-project apps/GameHub.Web.API/GameHub.Web.API.csproj --context ApplicationDbContext
```

Do not substitute a production connection string or run update commands as part of a documentation task. The project has EF Design/Tools package references, but those references alone do not establish that the `dotnet-ef` CLI tool is installed.

## 8. Memory, skills, and context management

Codex local memory and ChatGPT web memory use separate stores and controls. When local memories are enabled, Codex can derive useful context from eligible prior tasks in the background; updates are not necessarily immediate. The current documentation describes `/memories` controls and generated files under the Codex home directory. Keep mandatory team rules in committed instructions regardless of whether a developer enables memory. Do not create a root `MEMORY.md` and assume Claude's loading rules apply. [Memories](https://learn.chatgpt.com/docs/customization/memories).

Skills package repeatable workflows. A skill has a `SKILL.md` with `name` and `description`, plus optional resources. Codex initially sees metadata and reads the full instructions when a skill is selected; selection can be explicit or based on the description. Repository skills are discovered under `.agents/skills` along the working-directory-to-root path. [Building skills](https://learn.chatgpt.com/docs/build-skills).

No custom skill is required for this baseline. Once a procedure is stable, a team could package a feature-implementation or migration-review workflow as a skill while leaving architecture constraints in `AGENTS.md`. Avoid duplicating the standard inside every skill. The official skill guide also describes plugin distribution for reusable workflows beyond one repository. [Building skills](https://learn.chatgpt.com/docs/build-skills).

For efficient daily use, keep root instructions short, provide precise feature paths, and request tests that resolve the actual risk. Store lengthy explanations in this guide rather than always-loaded instructions. Avoid repeated full-solution scans for a small isolated edit. These are team workflow recommendations; they do not promise a specific price, speedup, or token saving.

Choose the model available under your organization's plan and evaluate it on representative tasks. Measure accepted changes, review effort, failures, and verification quality. This setup deliberately hard-codes neither a model name nor pricing because the architecture standard should survive model changes.

## 9. Using the same standards in ChatGPT

Copy the [ChatGPT instruction card](templates/chatgpt-project-instructions.md) into a project's instructions, adapting the wording to your workflow. Supply the shared standard, GameHub reference, and the relevant code or diff as project sources. Include the source date/commit when available. A ChatGPT project needs uploaded or connected sources; merely mentioning a local file path does not supply its contents. [Projects and chats](https://learn.chatgpt.com/docs/projects).

For a task involving only a design, ask for alternatives and consequences. For a code review, supply the diff and its context. For an implementation suggestion without execution tools, require a clear distinction between suggested verification commands and tests actually executed.

Keep the canonical standards in Git. When they change, refresh any uploaded copies; otherwise ChatGPT and Codex may be working from different versions. Do not maintain independent architecture rules in a chat project's settings and a repository file.

## 10. Roll this out to other company projects

1. Copy [the shared standard](../engineering/dotnet-standard.md) to the adopting repository, retaining its version.
2. Copy [the root template](templates/project-AGENTS.template.md) to that repository's `AGENTS.md`; replace every placeholder and remove the template notice.
3. Create `docs/engineering/project-reference.md` with actual paths, reference directions, feature examples, runtime prerequisites, and known exceptions. Use GameHub's map as an example, not as a false inventory of another project.
4. Adapt the scoped instruction files to that project's directories. Add their paths to the root routing table. Omit UI/messaging rules where those capabilities do not exist.
5. Verify restore, build, tests, startup effects, and any migration commands in the intended environment. Do not label copied commands as validated.
6. Run an instruction-reading smoke prompt, then a small real change. Review the generated code against the shared standard.
7. Commit the guidance with normal code review. Track standard versions across repositories and distribute changes through reviewed updates.

Keep dependency direction, result boundaries, test expectations, and contract discipline common across projects. Keep names, paths, provider choices, framework versions, and deployment constraints explicit per project. Reusing architecture does not require installing RabbitMQ or adding Blazor to an application that has neither need.

Record deliberate exceptions in architecture decisions. Prefer enforcing stable, testable rules through analyzers, dependency tests, formatting checks, and CI. This change adds the guidance; it does not add those enforcement tools or claim that they already exist.

## 11. Troubleshooting and maintenance

| Symptom | What to check |
| --- | --- |
| Codex misses a layer rule | Ask which files it actually read; check the task's working directory and explicitly name the scoped file |
| Instructions seem stale or unexpected | Start a fresh session; check home/project overrides, fallback settings, and the instruction size budget |
| A documentation link was ignored | Turn the pointer into an explicit instruction to read that file before the relevant work |
| Codex proposes PostgreSQL or a different mediator | Point to the actual project files and reference map; remove conflicting template text |
| Validator tests pass but invalid HTTP input reaches a handler | Trace real pipeline execution, especially `IBaseCommand` versus `IQuery<T>` |
| Integration tests cannot start | Check Docker access, image availability, SQL Server compatibility, and resource constraints |
| A cloud task cannot run Testcontainers | Verify that environment's Docker capability; use a capable local/CI environment and report the gap |
| A client receives unusable errors | Compare its deserializer with the API response shape, including the current ProblemDetails transition |
| Rules are followed inconsistently | Remove contradictions, keep instructions specific, and add executable enforcement for important invariants |

For instruction discovery issues, the official troubleshooting reference covers working directories, overrides, fallback names, and the byte budget. [AGENTS.md troubleshooting](https://learn.chatgpt.com/docs/agent-configuration/agents-md).

Review guidance whenever architecture, dependencies, commands, or public contracts change. Update the narrowest applicable file and remove outdated baseline observations. A useful review question is: could a developer who has never opened this repository perform and verify the task using these instructions?

## 12. Validation of this documentation delivery

The setup was derived from project files, registrations, representative features, mappings, client services, and test fixtures. The working tree already contained application changes, including the ProblemDetails transition; those were preserved and explicitly identified in the reference map.

Validation for this documentation-only change checks Markdown links, referenced local paths, instruction sizes, template placeholders, and the changed-file scope. The restore, build, application tests, host startup, and EF commands above are operating instructions; they were not executed as application verification for this delivery. No machine configuration, database, or package version was changed.
