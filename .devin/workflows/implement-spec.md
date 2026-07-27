---
description: Implement a single approved spec end-to-end
---

# Workflow: Implement a Spec (loop steps 2–3)

Use this workflow when the user asks to implement a spec in `specs/`. This is the
autonomous part of the loop: it starts **only after Gate 1** (spec approved) and
ends by handing off to Gate 2 (`review-and-merge`).

## Steps

1. **Identify the spec.** Confirm the spec id and open `specs/<id>-<slug>.md`.
   **GATE 1 CHECK:** if front-matter `status` is not `approved`, STOP — run the
   `write-spec` workflow and get approval first. Never generate code from a draft.

2. **Read required context.** Before writing code, read:
   - The target spec in `specs/` (and any dependency specs)
   - The referenced contract in `contracts/` (if any)
   - `docs/prd.md`, `docs/architecture.md`, relevant `docs/adr/*`
   - `.devin/rules/core.md` and the focused rules (`spec-driven`, `quality-gates`,
     `security`, `architecture`, `contracts`, `dotnet-conventions`)

3. **Check dependencies.** If the spec depends on another incomplete spec, warn the user and propose to implement dependencies first.

4. **Design the minimal change.** Write a short 2-3 sentence implementation plan and present it to the user if the change is non-trivial. Otherwise proceed.

5. **Implement.** Follow the focused rules:
   - Prefer small, focused edits; add only the code the spec needs.
   - Business logic in services; keep endpoints/components thin (`architecture.md`).
   - Add EF Core migrations if the data model changes.
   - **Generate tests from the acceptance criteria** — required, not optional
     (`quality-gates.md`). Cover real edge cases.

6. **Verify.**
   - Run build + test (with coverage). CI must be able to reach 100% green.
   - If automated verification is not available locally, describe manual steps.

7. **Update status.** Set the spec `status: in-progress` while working; the switch
   to `done` happens at Gate 2 after merge.

8. **Hand off to Gate 2.** Open the PR (below) and stop for human review via the
   `review-and-merge` workflow. Do not self-merge without Gate 2 approval.

9. **Finalize.** Execute `.devin/workflows/finalize-spec.ps1` para garantir que as alterações sejam commitadas e enviadas:
   - Verifica alterações pendentes e cria commit automaticamente.
   - Faz push da branch atual para o origin.
   - Abre (ou reaproveita) PR da branch atual para `develop` e aprova.
   - Se solicitado, abre (ou reaproveita) PR de `develop` para `main` e aprova.

   Exemplo de comando:

   ```powershell
   powershell -ExecutionPolicy Bypass -File .devin/workflows/finalize-spec.ps1 -SpecId "S1.1"
   ```

   Para também gerar o PR de `develop` para `main`, adicione `-MainPR`:

   ```powershell
   powershell -ExecutionPolicy Bypass -File .devin/workflows/finalize-spec.ps1 -SpecId "S1.1" -MainPR
   ```

## Output

- Summary of files changed.
- Verification command(s) used.
- Any remaining TODOs or blockers.
