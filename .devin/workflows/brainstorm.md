---
description: Brainstorm and refine requirements with the PM
---

# Workflow: Brainstorm / Refine Requirements

Use this workflow when the user (PM) wants to discuss, clarify or expand product requirements before implementation.

## Steps

1. **Read context.** Before proposing ideas, read:
   - `docs/prd.md`
   - `docs/plan.md`
   - `docs/architecture.md`

2. **Ask clarifying questions.** Identify ambiguity in the current PRD/specs. Examples:
   - "Should the project start date be fixed to the creation date or can it be configured?"
   - "What should happen when the end date is reached?"
   - "Which fields are mandatory in the check-in form?"

3. **Summarize decisions.** When the PM answers, update the relevant doc or spec immediately with the agreed answer. Do not keep decisions only in chat.

4. **Keep scope tight.** For each new idea, classify it as:
   - `MVP` (must-have for the first release)
   - `Post-MVP` (nice to have)
   - `Out of scope`

5. **Output.** End the session with:
   - A short list of decisions made.
   - Files that were updated.
   - Next suggested action (e.g., create a new spec, start implementation sprint).

## Constraints

- Do not write production code during a brainstorm unless explicitly asked.
- Prefer updating `docs/prd.md` or `docs/plan.md` over explaining in chat.
- Use the same language as the PRD (Portuguese) for project docs.
