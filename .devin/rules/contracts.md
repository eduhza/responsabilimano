# Rule: Contracts (Tenet 6 — contracts over coordination)

Integrations are defined by formal contracts before code is written. This is how
the app evolves safely (e.g., adding the check-in cron and, later, any external
integration) without silent breakage.

## Rules

- REST endpoints are described by **OpenAPI 3.1** in `contracts/*.yaml`.
- Asynchronous/scheduled integrations (the check-in cron, reminder jobs) are
  described by **AsyncAPI** event contracts in `contracts/*.yaml`.
- A spec that adds or changes an integration must reference its contract via the
  `contract:` front-matter field. No contract → resolve that first.
- **Brownfield baseline:** the current endpoints in `Program.cs` are reverse-
  engineered into OpenAPI as the baseline (spec `R9`). New work extends the
  contract; it does not diverge from it.
- **Contract tests run in CI** and must pass — they validate that the
  implementation honours the contract. This is one of the two stages AI-Native
  adds to the pipeline (the other is spec conformance).

## Where contracts live

`contracts/` at the repo root (shared surface). Even as a solo/monorepo, keeping
contracts separate from code makes them the coordination point and the input for
mocks and contract tests.
