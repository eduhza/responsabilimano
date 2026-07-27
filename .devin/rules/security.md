# Rule: Security (Data Governance + Security Guild, automated)

Every spec carries a "Security Constraints" section. This rule is the baseline
the agent enforces even when the spec is silent.

## Always

- **Validate and sanitize all input**, server-side (client-side is a UX aid, not
  a control). Reuse shared validators; do not duplicate ad-hoc checks.
- **No PII in logs.** Never log passwords, tokens, reset/invite codes, or raw
  emails at info level. Structured logging must scrub sensitive fields.
- **Passwords** are hashed with BCrypt (already via `PasswordHasher`). Never
  store or log plaintext. Tokens (reset/invite) are URL-safe, random, expiring.
- **Auth on every stateful endpoint.** A user may only act on projects they
  participate in. Verify ownership in the service, not just the UI.
- **Secrets** never committed. Use GCP Secret Manager (prod) and `.env`
  (local, git-ignored). `.env.example` documents the keys without values.

## Antiforgery / CSRF

- Blazor static SSR form posts require antiforgery. `DisableAntiforgery()` is
  only acceptable for machine-to-machine endpoints (cron/webhooks) that are
  protected by a secret header — and each such case must be justified in an ADR.
- Revisit existing `DisableAntiforgery()` usages (auth, projects, change-requests)
  — they are a known debt (spec `R5`).

## Pipeline

- SAST (CodeQL) and dependency scanning (SCA) run in CI and must pass. The agent
  auto-remediates flagged findings before requesting review.
- Follow OWASP Top 10 basics: XSS, CSRF, SQL injection (EF Core parameterizes),
  broken auth, sensitive-data exposure.
