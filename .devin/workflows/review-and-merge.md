---
description: Gate 2 — review the AI-generated PR, approve, merge, deploy behind a flag
---

# Workflow: Review & Merge (loop steps 4–5)

Use this after `implement-spec` has opened a PR and CI has run. This is **Gate 2**:
the point where human judgment is worth the most.

## Preconditions

- The PR links to an approved spec (`status: approved`).
- CI is **100% green**: build, test (+coverage threshold), SAST/DAST/SCA, spec
  conformance, contract test. If red, the agent fixes and re-pushes — do not
  review a red pipeline.

## Gate 2 checklist (human)

1. **Spec conformance** — does the diff implement exactly the spec, no scope creep?
2. **Tests are meaningful** — each acceptance criterion has a test that exercises
   the real edge case, not just the happy path. Reject "green but empty" tests.
3. **Security became code** — the spec's Security Constraints are actually
   implemented (auth/ownership checks, input validation, no PII in logs).
4. **Conventions** — matches `.devin/rules/dotnet-conventions.md` and
   `architecture.md` (thin endpoints/components, logic in services).
5. **Migrations** — present and descriptive if the data model changed.

If anything fails, return to generation with **specific** feedback (cite the
criterion/file), not a vague "try again".

## Merge & deploy

6. Approve → merge (PR into `develop`; `develop → main` releases via CI).
7. Deploy is automatic on merge to `main` and lands **behind a feature flag**
   (off). Deploy ≠ release.
8. Validate in production with the flag off. Turn the flag on now (single-spec)
   or defer to Gate 3 (`accept-feature`) for multi-spec features.
9. Set the spec `status: done`; close the tracking item.

## Output

- Merge/deploy confirmation, flag name and state, and the spec status change.
