---
description: Turn an idea/feature into an atomic, approvable spec (Gate 1 prep)
---

# Workflow: Write a Spec

Use this when a request has no matching approved spec. It produces one file in
`specs/` that a human can approve at Gate 1. Corresponds to step 4 (decompose →
spec) of the AI-Native 12-step process.

## Steps

1. **Read context.** `docs/prd.md`, `docs/plan.md`, `docs/architecture.md`,
   relevant ADRs in `docs/adr/`, and `.devin/rules/spec-driven.md`.

2. **Check scope.** One spec = one iteration (2–3 days) = one deliverable. If the
   request is bigger, propose **multiple** specs (and, if they compose a visible
   capability, a short Feature brief) instead of one large spec.

3. **Draft from the template.** Copy `specs/_template.md` to
   `specs/<id>-<slug>.md`. Fill every section:
   - Testable acceptance criteria (each one should map to at least one test).
   - Data model, security constraints, and the contract reference (`contracts/…`).
   - Dependencies and explicit Out of Scope.
   - Front-matter with `status: draft` and a `tracking:` GitHub Issue id.

4. **Self-check (is it generatable?).** Confirm the AI could produce correct code
   from this spec alone. If not, tighten it — ambiguity here becomes bugs later.

5. **Present for approval (Gate 1).** Summarize the spec and ask the PM to
   approve. On approval, set `status: approved`. Only then may implementation start.

## Output

- Path to the new spec file.
- The tracking item id.
- Open questions blocking approval, if any.
