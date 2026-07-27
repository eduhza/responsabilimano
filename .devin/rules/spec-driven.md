# Rule: Spec-Driven (Tenet 1 — Specs are the source of truth)

The spec — not the chat, not the issue, not the code — is the authoritative
definition of what to build. Specs live in `specs/` as machine-readable files.

## Hard rules

- **Never generate implementation code without an APPROVED spec.** A spec is
  approved when its front-matter has `status: approved`.
- If a request has no matching spec, STOP and run the `write-spec` workflow to
  propose one in `specs/`. Wait for the human to approve (Gate 1) before coding.
- Each spec is atomic: **one spec = one iteration (2–3 days) = one deliverable.**
  If it can't fit, decompose into multiple specs instead of stretching it.
- The spec is the source of truth. If code and spec diverge, either fix the code
  or update the spec in the same PR — never leave them silently inconsistent.
- Every spec links to a tracking item (GitHub Issue) via the `tracking:` field.
  Requirements live in Git; status/ownership lives in the tracker.

## The spec must contain (see `specs/_template.md`)

Front-matter (`id`, `feature`, `priority`, `iteration`, `contract`, `tracking`,
`status`) plus: User Value, testable Acceptance Criteria, Data Model, Security
Constraints, API/Event Contract reference, Dependencies, Out of Scope.

## Definition of "clear enough to generate"

Before pointing the coding agent at a spec, confirm: acceptance criteria are
testable, the data model is defined, integration points reference a contract,
and security constraints are explicit. If any is missing, the spec is not ready.

> If the AI cannot generate good code from the spec, the spec is wrong — fix the
> spec, not the code.
