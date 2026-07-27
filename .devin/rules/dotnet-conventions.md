# Rule: .NET / C# Conventions

Baseline conventions for generated C#. When in doubt, run the
`dotnet-best-practices` skill.

## Language & style

- C# 13 / .NET 10. `Nullable` and `ImplicitUsings` are enabled solution-wide
  (see `Directory.Build.props`); do not re-declare per project.
- PascalCase for types/methods/properties; camelCase for locals. Prefer explicit
  types when the type is not obvious from the right-hand side.
- Small, focused classes and methods. No dead code, no leftover template files
  (the `Class1.cs` stubs are being removed — spec `R3`).

## Async & data

- `async`/`await` for all I/O (EF Core, email, HTTP). Pass and honour
  `CancellationToken` through service methods (the codebase already does this).
- Watch Npgsql UTC requirements: persist `DateTime` as UTC (`DateTimeKind.Utc`).
  This was a real bug (commit "Fix DateTime Kind for Npgsql UTC requirement").

## Packages

- Central Package Management: versions live in `Directory.Packages.props`. Add a
  `PackageVersion` there and reference without a version in the csproj.

## Tests

- xUnit. Service/domain tests use `TestDbContextFactory` (SQLite in-memory).
  Use `NullLogger<T>.Instance` and the `FakeEmailService` test double.

## Commits

- English, imperative mood: `Add check-in data model`. One logical change per
  commit. Never commit build artifacts (`bin/`, `obj/`) or secrets.
