# Rule: Quality Gates (Tenet 5 — quality is automated, not staffed)

There is no human QA. Quality is a property of the process. The CI is the gate;
the human is the final judgment at Gate 2.

## The AI generates tests — never skipped

- For every spec, generate tests **from the acceptance criteria** (unit for
  domain logic, integration for endpoints/business rules).
- Do not write tests for trivial getters/setters or markup alone.
- Tests live in `tests/` (xUnit). Domain/service tests use the SQLite in-memory
  `TestDbContextFactory` (relational — honours constraints and cascades).

## "Green" is not enough

- Each acceptance criterion has at least one test that exercises the **real edge
  case**, not just the happy path.
- A test that passes but does not assert meaningful behaviour is a trap — remove
  or fix it. This judgment belongs to the human reviewer at Gate 2.

## CI must be 100% green before requesting human approval

The pipeline runs, in order and never skipped:
`build → test (+coverage) → SAST/DAST/SCA → spec conformance → contract test`.

- Coverage on `Core` + `Infrastructure` must not drop below the configured
  threshold (see `.github/workflows/ci-cd.yml`). New services must ship tests.
- If the CI is red, the agent auto-remediates lint/security findings and pushes
  fixes. It does **not** ask for human approval until the pipeline is green.

## Gate 2 human checklist (see `review-and-merge` workflow)

Do the tests cover the real edge cases? Did the spec's security constraints
become code? Does the code match the spec (spec conformance)? Only then approve.
