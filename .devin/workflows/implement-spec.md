---
description: Implement a single approved spec end-to-end
---

# Workflow: Implement a Spec

Use this workflow when the user asks to implement a feature that matches a spec in `docs/plan.md`.

## Steps

1. **Identify the spec.** Ask the user which spec to implement, or infer from the request. Confirm the spec ID (e.g., `S1.1`).

2. **Read required context.** Before writing code, read:
   - `docs/prd.md`
   - `docs/plan.md` (the target spec and any dependent specs)
   - `docs/architecture.md`
   - `.devin/rules/core.md`
   - `.devin/skills/sdd.md`

3. **Check dependencies.** If the spec depends on another incomplete spec, warn the user and propose to implement dependencies first.

4. **Design the minimal change.** Write a short 2-3 sentence implementation plan and present it to the user if the change is non-trivial. Otherwise proceed.

5. **Implement.** Follow the rules in `.devin/rules/core.md`:
   - Prefer small, focused edits.
   - Add only the code needed for the spec.
   - Add migrations if the database model changes.
   - Add tests only if requested or obviously required for correctness.

6. **Verify.**
   - Run the minimal command needed to verify the change (build, test, or start app).
   - If automated verification is not available, describe manual steps.

7. **Update status.** Mark the spec as completed in `docs/plan.md` when done.

8. **Finalize.** Execute `.devin/workflows/finalize-spec.ps1` para garantir que as alterações sejam commitadas e enviadas:
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
