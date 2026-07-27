# Core Rules for AI — ResponsabiliMano

## Project Context

- This is a .NET 10 + Blazor + PostgreSQL web application.
- It runs on Google Cloud Platform (GCP).
- Development follows the **AI-Native SDLC** (Dell framework): the AI is the primary author of code and tests; the human curates. See `docs/plano-ai-native-sdlc.md`.
- Development follows Spec Driven Development (SDD): every code change traces back to an **approved spec in `specs/`**. `docs/plan.md` is the roadmap/index; `docs/architecture.md` + `docs/adr/` hold decisions.

## The 3 human gates (this is where the PM intervenes)

The loop runs autonomously except at three points. Do not proceed past a gate without human approval:

- **Gate 1 — Approve the spec** (before generating). Enforced by `.devin/rules/spec-driven.md`.
- **Gate 2 — Approve the MR** (after CI is 100% green). Enforced by `.devin/rules/quality-gates.md` and the `review-and-merge` workflow.
- **Gate 3 — Accept the feature** (E2E, only for multi-spec features). Flip the feature flag to release.

Everything between gates — generation, PR, CI, security auto-remediation — is autonomous.

## Rule index

Read the focused rule for the concern at hand: `spec-driven.md`, `quality-gates.md`, `security.md`, `architecture.md`, `contracts.md`, `dotnet-conventions.md`. This file holds the cross-cutting basics below.

## AI Working Modes (Agent vs Editor)

- **Agent mode:** appropriate for broad, exploratory, or setup tasks that touch many files (e.g., repository scaffolding, PRD review, initial architecture). When in Agent mode, still respect SDD and keep changes aligned with `docs/plan.md` and `docs/architecture.md`.
- **Editor mode:** appropriate for implementing a single approved spec at a time. When the PM asks to implement a spec (e.g., "Implemente a spec S1.1"), read the spec, the PRD, the architecture doc, and the relevant rules before making focused changes. Avoid scope creep.
- The PM will explicitly choose the mode for each conversation. If the request is spec-specific, assume Editor mode and stay inside the spec scope.

## Code Style

- Use C# 13/.NET 10 features where appropriate.
- Follow standard C# naming: PascalCase for types/methods/properties, camelCase for local variables.
- Keep classes and methods small and focused.
- Prefer explicit types over `var` when the type is not obvious.
- Do not over-engineer. Avoid unnecessary abstractions, design patterns, or microservices.

## Frontend (Blazor)

- Use built-in Blazor components and MudBlazor/Fluent UI only if already referenced.
- Keep Razor components readable; extract markup to child components when it grows.
- Use `@code` blocks for simple logic; move complex logic to partial classes or services.
- Validate user input on both client and server.

## Backend

- Use ASP.NET Core Minimal APIs or Controllers consistently (decide in architecture and stick to it).
- Use Entity Framework Core for database access.
- Use async/await for I/O-bound operations.
- Hash passwords with a secure algorithm (BCrypt/Argon2/Identity default). Never store plain text.
- Never commit secrets, connection strings, or credentials.

## Database

- Use EF Core migrations. Name migrations descriptively.
- Map all PostgreSQL table and column names to lowercase (snake_case for compound names). C# entities and properties can remain PascalCase; EF Core must map to lowercase database identifiers.
- Keep schema normalized for MVP. Avoid JSON columns unless justified.
- Foreign keys must have proper indexes for query performance.

## Security

- Validate and sanitize all inputs.
- Protect admin/cron endpoints with API keys or secret headers.
- Use HTTPS in production.
- Follow OWASP top 10 basics: XSS, CSRF, SQL injection (EF Core helps), insecure auth.

## Testing

- Add unit tests for pure domain logic.
- Add integration tests for API endpoints when they involve business rules.
- Do not write tests for trivial getters/setters or UI markup alone.

## Git / Commits

- Write commit messages in English in the imperative: `Add user registration endpoint`.
- One logical change per commit.
- Do not include generated build artifacts in commits.

## Communication

- Prefer Portuguese when updating project docs (`docs/`) because the PM works in Portuguese.
- Explain trade-offs briefly when asked to choose between technologies.
