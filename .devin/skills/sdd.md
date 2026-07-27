# Skill: Spec Driven Development (SDD)

## What is SDD

Spec Driven Development means the repository is the source of truth for requirements. Every implementation task starts from an approved spec. The agent never writes code without a clear spec.

## Spec Format

Each spec is its **own machine-readable file** in `specs/`, following
`specs/_template.md`. It has YAML front-matter (`id`, `feature`, `priority`,
`iteration`, `contract`, `tracking`, `status`) and sections: User Value,
Acceptance Criteria (testable), Data Model, Security Constraints, API/Event
Contract, Dependencies, Out of Scope.

`docs/plan.md` is the **roadmap/index** that links to these spec files — it is no
longer where the spec text lives. (Git = requirements; the tracker = status.)

## How to Use Specs

1. **Before coding:** Read the spec. Confirm `status: approved` (Gate 1) and that
   dependencies are done. If it is `draft`, do not code — get it approved first.
2. **During coding:** Implement only what the spec asks. Generate tests from the
   acceptance criteria. If the spec is ambiguous, pause and fix the spec.
3. **After coding:** Verify against the acceptance criteria; set the spec
   `status: done` and update the linked tracking item.

## When a Spec Is Missing

If the user asks for a feature not covered by an existing spec:

1. Do not implement immediately.
2. Run the `write-spec` workflow to create `specs/<id>-<slug>.md` from the template.
3. Summarize the spec and ask for approval (Gate 1).
4. Only after `status: approved`, implement.

## Keeping Docs Updated

- When the PM makes a decision, record it as an ADR in `docs/adr/` and update
  `docs/architecture.md` if needed.
- Keep the spec's `status` field current; keep `docs/plan.md` roadmap in sync.
- Treat `docs/prd.md`, `docs/plan.md`, `docs/architecture.md` and `specs/` as
  living documents.
