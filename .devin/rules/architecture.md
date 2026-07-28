# Rule: Architecture (Architecture Guild, automated)

## Layering

- Keep the layered structure: `Core` (entities, enums, interfaces, domain
  rules) → `Infrastructure` (EF Core, services, email, jobs) → `Web` (Blazor +
  host + endpoints). Dependencies point inward only; `Core` references nothing.
- Business logic lives in services (`Infrastructure/Services`), never in
  `Program.cs` or Razor components. Components and endpoints are thin.

## Endpoints

- `Program.cs` orchestrates bootstrap only. HTTP endpoints are grouped into
  endpoint modules via `MapGroup` + `IEndpointRouteBuilder` extension methods
  (target state — spec `R1`). Do not add new endpoints inline in `Program.cs`.

## Statelessness

- The web app is stateless: no server-held session state beyond the auth cookie.
  Anything that must persist goes to PostgreSQL. This keeps Cloud Run scaling and
  future horizontal scale safe.

## Decisions become ADRs

- Any structural decision (new dependency, pattern choice, tech swap, endpoint
  strategy, render-mode strategy) is recorded as an ADR in `docs/adr/` using the
  template there. Reference the ADR id from the relevant spec.
- Do not introduce speculative abstractions, patterns, or microservices. Prefer
  the simplest design that satisfies the spec (MVP-first).

## Data

- EF Core migrations, named descriptively. PostgreSQL identifiers are lowercase
  / snake_case; C# stays PascalCase (mapped in `AppDbContext`).
- Normalize the schema; avoid JSON columns unless justified (the
  `ProjectChangeRequest.PayloadJson` case is justified and documented).
