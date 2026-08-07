# ResponsabiliMano agent notes

## Verification
- Build: `dotnet build ResponsabiliMano.slnx`
- Unit/Integration: `dotnet test ResponsabiliMano.slnx`
- E2E: start Postgres with `docker compose up -d db`, then `dotnet run --project src/ResponsabiliMano.Web --launch-profile http`.

## Local dev test accounts
The dev seed fixture uses these demo accounts (password is **case-sensitive**):
- `ana@email.com` / `Password123`
- `bruno@email.com` / `Password123`

Note: e-mail login is **not** case-insensitive at the time of writing; login with `A@Example.COM` fails.

## Goal negotiation E2E flow
1. Create a project with one or more goals.
2. Invite a partner; the project stays in `Pending`.
3. Partner accepts invitation; each goal has two `GoalTarget` rows (creator / partner) in `PendingAcceptance`.
4. Either participant can:
   - Click **Ajustar** to propose a new target/comment for their own row.
   - Click **Aceitar** or **Aceitar todos** to accept pending targets they did not propose.
5. When the last pending target is accepted by the other side, the project becomes `Active`.

## Workflow reminder
When implementing a spec from `specs/`:
1. Create a feature branch **before** committing (`git checkout -b feature/<spec-id>-...`).
2. Never commit directly to `develop`.
3. After finishing, push the branch and open a PR to `develop` with `gh pr create --base develop ...`.
4. Mark the spec as `status: in-progress` in its markdown file.
5. Wait for the user's `/review-and-merge` approval before merging.
