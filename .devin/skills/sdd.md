# Skill: Spec Driven Development (SDD)

## What is SDD

Spec Driven Development means the repository is the source of truth for requirements. Every implementation task starts from an approved spec. The agent never writes code without a clear spec.

## Spec Format

A spec in this repo is a section inside `docs/plan.md` with:

- A unique ID (e.g., `S1.1`, `S2.3`).
- A one-sentence objective.
- A checklist of behaviors or endpoints.
- Acceptance criteria.

## How to Use Specs

1. **Before coding:** Read the spec. Confirm it is approved and dependencies are done.
2. **During coding:** Implement only what the spec asks. If the spec is ambiguous, pause and ask the PM.
3. **After coding:** Verify against the acceptance criteria. Update the spec status in `docs/plan.md`.

## When a Spec Is Missing

If the user asks for a feature not covered by an existing spec:

1. Do not implement immediately.
2. Create or propose a new spec in `docs/plan.md`.
3. Summarize the spec and ask for approval.
4. Only after approval, implement.

## Keeping Docs Updated

- When the PM makes a decision, update the PRD or architecture doc immediately.
- When a sprint is done, update the status checkboxes in `docs/plan.md`.
- The agent should treat `docs/prd.md`, `docs/plan.md` and `docs/architecture.md` as living documents.
