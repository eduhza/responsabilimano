---
name: e2e-playwright-tester
description: Use this skill whenever a spec involving backend-frontend interaction has just been implemented, when the user asks to run E2E tests, or when a feature needs end-to-end validation through the browser. This skill rebuilds the Docker containers, waits for the app to be ready, uses the MCP Playwright browser to navigate and exercise the spec's user flow, checks for console errors and captures screenshots, and reports whether the feature works. On failure, it preserves evidence and attempts to fix the underlying issue.
---

# E2E Playwright Tester

This skill validates a spec with backend-frontend interaction by exercising the described user flow in a real browser using the `devin/mcp-playwright` MCP server.

## When to use

- At the end of an `implement-spec` workflow, especially when the spec touches both backend and UI.
- When the user asks to "test this feature", "run E2E tests", "validate the UI flow", or similar.
- When the user mentions `docker compose`, `mcp-playwright`, or end-to-end verification.

## Inputs

Determine the inputs from context or by asking briefly:

- **Spec path**: the `.md` spec that was just implemented. Prefer the active IDE document or the spec referenced in the conversation.
- **App base URL**: default is `http://localhost:8080`. If the project uses a different port, infer it from `docker-compose.yml` or ask.
- **Credentials / seed data**: for auth flows, use seeded credentials from the project (`ana@email.com` or `bruno@email.com`, password `Password123`) unless the spec provides others.
- **Docker compose command**: `docker compose up -d --build` at the repository root.

## Workflow

1. **Read the spec** and extract the user flow. Identify the starting point, the actions the user performs, and the expected outcome.
2. **Plan the test**:
   - List the steps a real user would take.
   - Identify expected final URL and visible text.
   - Note any known project limitations (e.g. in ResponsabiliMano, `EditForm` pages do not work statically; prefer plain HTML `<form method="post">` flows such as login/logout).
3. **Rebuild and start the environment**:
   - Run `docker compose up -d --build` from the repository root.
   - Wait for the build to finish before proceeding.
4. **Wait for readiness**:
   - Ping the base URL for up to 120 seconds.
   - If it does not respond, run `docker compose logs --tail 50` and report the error. Do not proceed until the app is reachable.
5. **Run the browser flow**:
   - Use `browser_navigate` to open the starting URL.
   - Use `browser_snapshot` to understand the page state.
   - Use `browser_fill_form`, `browser_type`, `browser_click`, `browser_select_option`, and other MCP tools to reproduce the user actions.
   - Use `browser_wait_for` when text or navigation must appear.
6. **Validate**:
   - Check the final URL and visible text with `browser_find` or `browser_snapshot`.
   - Capture `browser_console_messages` with `level: "error"`.
   - If any error appears or the expected state is missing, the test has failed.
7. **Capture evidence**:
   - Take screenshots at the start, middle and end of the flow using `browser_take_screenshot`.
   - Save them under the default MCP output directory. If the test passes and the user has not asked to keep them, remove the artifacts at the end.
   - If the test fails, keep all artifacts for diagnosis.
8. **Report**:
   - Summarize what was tested.
   - List the steps executed.
   - State pass/fail.
   - Include any console errors found.
   - Reference screenshot file paths when they are retained.
9. **Attempt to fix on failure**:
   - Read relevant code, trace the failing path, and identify the root cause.
   - Prefer small, targeted fixes.
   - Do not modify the spec file.
   - Stop and ask the user if the cause is unclear or the fix is risky.
   - After fixing, rebuild with `docker compose up -d --build` and rerun the flow.

## Project-specific notes for ResponsabiliMano

- Render modes are **per page** (ADR-0003, enforced by `RenderModeTests`):
  - **Static SSR**: the landing page (`/`), `/login` and `/register`. These post native HTML
    forms because the auth cookie cannot be set over a SignalR circuit. Their field ids,
    names and form actions are a contract — do not assume they can be made interactive.
  - **Interactive Server**: every other page. `EditForm`-based flows (create project,
    invite partner, check-in, forgot/reset password) do work through the UI; spec X1
    applied `@rendermode InteractiveServer` to them.
- `/` is the public marketing landing. Signed-in visitors are redirected to `/projects`,
  which is the authenticated project list.
- The demo fixture is gated by the `SeedDemoData` config flag (on by default in
  `docker-compose.yml`), and `SeedData` skips entirely when any user already exists — an
  older volume will keep its old data. Truncate the tables to re-seed.
- Email normalization can be tested with a mixed-cased email such as `A@Example.COM`.

## Output format

Return a concise report with:

```
## Teste E2E: <spec name>
- Fluxo: <steps>
- Resultado: <pass / fail>
- URL final: <url>
- Erros no console: <none or list>
- Screenshots: <paths or removed>
- Correções tentadas: <none or summary>
```

Keep the report factual and easy to scan.
