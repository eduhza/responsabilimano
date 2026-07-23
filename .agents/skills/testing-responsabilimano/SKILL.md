---
name: testing-responsabilimano
description: Test ResponsabiliMano end-to-end through its local Blazor UI and PostgreSQL-backed web app. Use when verifying auth/project/email flows or backend refactors.
---

# Testing ResponsabiliMano

ASP.NET Core (.NET 10) + Blazor + EF Core + PostgreSQL app for accountability partners.

## Setup
1. `dotnet` may not be preinstalled. Install .NET 10: `curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0 --install-dir "$HOME/.dotnet"` then `export PATH="$HOME/.dotnet:$PATH"`. (A blueprint suggestion to install it automatically has been submitted.)
2. Start DB: `docker compose up -d db` (Postgres, port 5432, db/user/pass = responsabilimano/postgres/postgres).
3. Run: `ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/ResponsabiliMano.Web --launch-profile http` → serves `http://localhost:5055`. Dev startup auto-runs EF migrations + `SeedData.SeedAsync`.
4. Seed users (only created if Users table empty): `a@example.com` / `b@example.com`, password `Password123`. Seed demo project "Projeto Demo" owned by user A.

## CRITICAL: the app is static-SSR only
- No component wires up `@rendermode InteractiveServer` (it's registered in Program.cs but never applied to `<Routes>` or any page). So `EditForm`-based pages — **Register, CreateProject, InvitePartner, ForgotPassword, ResetPassword** — do NOT bind through the browser: fields fill in the DOM but submit empty and show "required" validation. This may be a bug/limitation; do not assume these UI forms work.
- The **Login** page and the **Logout**/logout buttons are plain HTML `<form method="post" action="/api/auth/...">`, so they DO work statically. Login is the reliable UI entry point.
- Waiting longer for a Blazor circuit will NOT fix the EditForm pages — the circuit is never started because no render mode is applied.

## Recommended UI test (works statically)
- Login with a MIXED-CASE version of a seeded email (e.g. `A@Example.COM` for `a@example.com`) → should redirect to `/` and render "Olá, Usuário A!" with the demo project. This exercises email normalization end-to-end. Wrong casing failing to log in would indicate broken normalization.
- After login, home lists the user's projects; nav shows Início / Novo Projeto / Sair. Click Sair to test logout (returns to /login).

## Testing backend/service/endpoint logic via the JSON API (shell)
Since the UI forms are static-SSR-blocked, validate services + minimal-API endpoints directly:
- `POST /api/auth/forgot-password {"email":"not-an-email"}` → 400 with `{"errors":{"email":["A valid email is required."]}}` (email validation).
- `POST /api/auth/forgot-password {"email":"A@Example.com"}` → 200; the dev email is LOGGED to stdout (LoggingEmailService prints `=== EMAIL (Dev) ===`). Grep the app log for the reset/invite token to verify token generation (should be URL-safe base64: only `[A-Za-z0-9_-]`).
- `GET /api/projects/{guid}` with no auth cookie → 401 (auth check). NOTE: POST endpoints return 400 for missing content-type/antiforgery *before* the auth check when hit via curl, so use a GET endpoint to cleanly observe the 401 auth branch.
- The Blazor UI calls services directly and does NOT hit `/api/projects/*`, so endpoint-only helpers (exception→HTTP mapping etc.) are only reachable via authenticated JSON calls with cookies — hard to reach without scripting cookie auth; note as not-directly-tested if you can't.

## Devin Secrets Needed
None. Everything runs locally with the hardcoded dev Postgres credentials in `docker-compose.yml` / `appsettings.Development.json`.
