---
id: D4
feature: Redesign UI — camada quente sobre FluentUI
pod: ResponsabiliMano (solo)
priority: P1
iteration: 1 (1 dia)
contract: none
tracking: gh-issue-#TBD
status: approved
depends_on: [RT1]
adr: [0003, 0006]
supersedes: D2
---

# D4 — Redesign Auth + Home (camada quente)

Supersede **D2**. Entrega a primeira impressão (Login, Register, ForgotPassword,
ResetPassword, Home) com a paleta "Energia" e uma camada de design custom sobre o
FluentUI, mobile-first. Guiada pela skill `/frontend-design`.

## User Value

Como usuário, quero telas de entrada bonitas, quentes e coesas, para que minha
primeira impressão seja positiva e eu me sinta motivado a usar o sistema.

## Decisões de design

- **Assinatura:** wordmark "Responsabili**Mano**" (o sufixo "Mano" em coral, brincando
  com o nome) + tagline encorajadora, num card centralizado com faixa coral no topo.
- **Camada quente:** primitives próprios (`.rm-auth-card`, `.rm-input`, `.rm-btn`,
  `.status-pill`, `.project-card`) em `app.css`, derivados dos tokens Energia — em vez
  do visual cinza padrão do FluentUI. Dark mode automático (tokens já existentes).

## Acceptance Criteria

### AC1 — Login e Register (estáticos, POST puro — ADR-0003)

1. Mantêm `<form method="post">` nativo com `<input>`/`<button>` nativos. **Preservados**:
   `action` (`/api/auth/login`, `/api/auth/register-and-login`), `name=` dos campos
   (`email`, `password`, `Name`, `Email`, `Password`, `ConfirmPassword`), ids
   (`#email/#password/#name/#confirmPassword`), texto do `h3` ("Entrar"/"Cadastro") e
   texto/roles dos botões ("Entrar"/"Cadastrar"). (Guardados por `RegisterTests` bUnit e Playwright.)
2. Reestilizados com `.rm-auth`, `.rm-auth-card`, `.rm-input`, `.rm-btn`; erro (query
   `?Error=`) via `div.alert-danger` (classe preservada para o E2E).
3. **Não** ganham `@rendermode` — seguem estáticos (`RenderModeTests`).

### AC2 — ForgotPassword e ResetPassword (interativos)

1. Mantêm `@rendermode InteractiveServer`, `EditForm`, `InputText` com ids
   (`#email`, `#password`, `#confirmPassword`), texto dos botões e h3.
2. Sucesso via `div.alert-success` (classe + texto preservados para o E2E:
   "Se o e-mail existir" / "Senha redefinida"); erro via `div.alert-danger`.
3. Reestilizados com a mesma camada quente (card centralizado).

### AC3 — Home (interativa via RT1)

1. `<h1 class="home-greeting">` com `HomeWelcome` ("Olá, {nome}!") preservado (E2E
   testa `h1:has-text('Olá')` e o nome do usuário).
2. Projetos como `.project-card` (link para `/projects/{id}`) com `.status-pill` e
   **cor de status** na borda esquerda: Pending=âmbar, Active=teal, Finished=verde,
   Cancelled=coral. Grid 1 coluna no mobile, 2 no desktop.
3. **Skeleton** (`.skeleton .project-skeleton`) enquanto `_loading`; **empty state**
   (`.empty-state`) com ícone, texto e CTA "Criar projeto" para `/projects/new` quando
   não há projetos.
4. Erro de carregamento via `div.alert-danger`.

### AC4 — i18n

Novas chaves em `AppStrings.resx` (EN) e `AppStrings.pt-BR.resx`: `AuthTagline`,
`HomeSubtitle`, `HomeEmptyTitle`, `HomeEmptyDescription`, `HomeEmptyButton`.

### AC5 — Testes

`dotnet build` + `Web.Tests` (Register/RenderMode) verdes; E2E `AuthFlowTests`
preservado (seletores acima).

## Security Constraints

- Login/Register seguem POST HTTP estático (cookie não pode ser setado sobre circuito — ADR-0003).
- Antiforgery inalterado (ADR-0004). Sem PII em log.

## Out of Scope

- Telas de produto (ProjectDetail, InvitationAccept, CheckIn, Dashboard) → **D5**.
- Atualização ao vivo entre parceiros → **RT2**.

## Verification

- `dotnet build ResponsabiliMano.slnx`
- `dotnet test tests/ResponsabiliMano.Web.Tests`
- `dotnet test tests/ResponsabiliMano.Web.E2ETests --filter "AuthFlowTests"`
- Visual (`/run`): Login/Register/Home no mobile (375px) e desktop, dark mode do SO,
  empty state e skeleton na Home.
