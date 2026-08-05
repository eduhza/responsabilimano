---
id: D2
feature: Redesign UI — FluentUI Blazor + Design System
pod: ResponsabiliMano (solo)
priority: P1
iteration: 1 (2-3 dias)
contract: none
tracking: gh-issue-#TBD
status: superseded
superseded_by: D4
depends_on: [D1]
adr: [0003, 0006]
---

> **SUPERSEDED por D4** (`specs/D4-auth-home-redesign.md`). Esta spec nunca foi
> implementada; a D4 entrega o mesmo escopo com uma camada de design quente sobre o
> FluentUI e, diferentemente da AC10 abaixo, torna a **Home interativa** (spec RT1)
> para reatividade. Mantida como referência histórica das ACs.

# D2 — Telas Auth + Home com FluentUI

## User Value

Como usuário, quero telas de login, cadastro, recuperação de senha e home com visual atrativo e profissional, para que minha primeira impressão da aplicação seja positiva e eu me sinta motivado a usar o sistema.

## Acceptance Criteria

### AC1 — Login refatorada com FluentUI

1. `Login.razor` substitui o `<form>` HTML puro por `EditForm` com `FluentTextField` para e-mail e senha, mantendo o `action="/api/auth/login"` via `FormName` e POST estático.
2. A página **não** ganha `@rendermode` — permanece SSR estático (ADR-0003). O form continua sendo um POST HTTP puro para `/api/auth/login` (não usa `OnValidSubmit` nem circuito SignalR).
3. O botão de submit usa `FluentButton` com `Type="ButtonType.Submit"` e `Appearance="Appearance.Accent"`.
4. O input de e-mail mantém `id="email"`; o input de senha mantém `id="password"`.
5. O erro de login (query param `?Error=...`) é exibido via `FluentMessageBar` com `Severity="MessageBarSeverity.Error"` em vez de `alert alert-danger`.
6. O link "Esqueci minha senha" usa `FluentAnchor` (ou `<a>` estilizado) apontando para `/forgot-password`.
7. O texto "Não tem uma conta?" com link para `/register` permanece.
8. O título `h3` com `@Localizer["LoginTitle"]` (pt-BR: "Entrar") é mantido.
9. O botão mantém o role e nome acessíveis.

### AC2 — Register refatorada com FluentUI

1. `Register.razor` substitui o `<form>` HTML puro por `EditForm` com `FluentTextField` para Nome, E-mail, Senha e Confirmação de Senha, mantendo o `action="/api/auth/register-and-login"` via POST estático.
2. A página **não** ganha `@rendermode` — permanece SSR estático (ADR-0003).
3. O botão de submit usa `FluentButton` com `Type="ButtonType.Submit"` e `Appearance="Appearance.Accent"`.
4. Os inputs mantêm seus IDs: `id="name"`, `id="email"`, `id="password"`, `id="confirmPassword"`.
5. O erro (query param `?Error=...`) é exibido via `FluentMessageBar` com `Severity="MessageBarSeverity.Error"`.
6. O texto "Já tem uma conta?" com link para `/login` permanece.
7. O título `h3` com `@Localizer["RegisterTitle"]` (pt-BR: "Cadastro") é mantido.
8. O botão mantém o nome acessível.

### AC3 — ForgotPassword refatorada com FluentUI

1. `ForgotPassword.razor` mantém `@rendermode InteractiveServer` (ADR-0003).
2. O `EditForm` usa `FluentTextField` para o campo de e-mail (em vez de `InputText`).
3. A validação usa `FluentValidationMessage` (em vez de `ValidationMessage`).
4. O `ValidationSummary` é substituído por `FluentValidationSummary`.
5. O botão de submit usa `FluentButton` com `Type="ButtonType.Submit"` e `Appearance="Appearance.Accent"`.
6. O estado de sucesso (`_submitted`) exibe a mensagem via `FluentMessageBar` com `Severity="MessageBarSeverity.Success"` em vez de `alert alert-success`.
7. O erro (`_error`) é exibido via `FluentMessageBar` com `Severity="MessageBarSeverity.Error"`.
8. O link "Voltar para o login" permanece apontando para `/login`.
9. O estado de loading no botão mostra `Localizer["Sending"]` (pt-BR: "Enviando...") com `disabled`.

### AC4 — ResetPassword refatorada com FluentUI

1. `ResetPassword.razor` mantém `@rendermode InteractiveServer` (ADR-0003).
2. O `EditForm` usa `FluentTextField` para Nova Senha e Confirmação de Senha (em vez de `InputText`).
3. A validação usa `FluentValidationMessage` e `FluentValidationSummary`.
4. O botão de submit usa `FluentButton` com `Type="ButtonType.Submit"` e `Appearance="Appearance.Accent"`.
5. O estado de sucesso (`_success`) exibe a mensagem via `FluentMessageBar` com `Severity="MessageBarSeverity.Success"`.
6. O erro (`_error`) é exibido via `FluentMessageBar` com `Severity="MessageBarSeverity.Error"`.
7. O link "Voltar para o login" permanece no estado de sucesso.
8. O estado de loading no botão mostra `Localizer["Resetting"]` (pt-BR: "Redefinindo...") com `disabled`.

### AC5 — Home refatorada com FluentUI

1. `Home.razor` **não** ganha `@rendermode` — permanece SSR estático (ADR-0003).
2. O título `h1` com `@Localizer["HomeWelcome", ...]` (pt-BR: "Olá, {0}!") é mantido.
3. A lista de projetos usa `FluentCard` para cada projeto em vez de `list-group-item`.
4. Cada card de projeto é um link clicável para `/projects/{id}` com o nome do projeto e um badge de status.
5. O badge de status usa cores da paleta "Energia":
   - Pending: `--rm-accent` (âmbar)
   - Active: `--rm-secondary` (teal)
   - Finished: verde (`#5B9279`)
   - Cancelled: `--rm-primary` (coral)
6. O erro de carregamento (`_error`) é exibido via `FluentMessageBar` com `Severity="MessageBarSeverity.Error"`.

### AC6 — Empty state na Home

1. Quando `_projects.Count == 0` (e não há erro), a Home exibe um empty state em vez de nada.
2. O empty state contém: um ícone (usando `FluentIcon` com `Icons.Regular.Size48.Add` ou similar), um texto convidativo e um `FluentButton` com link para `/projects/new`.
3. Novas chaves de i18n adicionadas em `AppStrings.resx` e `AppStrings.pt-BR.resx`:
   - `HomeEmptyTitle` — EN: "No projects yet" / PT-BR: "Nenhum projeto ainda"
   - `HomeEmptyDescription` — EN: "Create your first project and invite a partner to start your journey together." / PT-BR: "Crie seu primeiro projeto e convide um parceiro para começarem a jornada juntos."
   - `HomeEmptyButton` — EN: "Create project" / PT-BR: "Criar projeto"

### AC7 — Skeleton loading na Home

1. Durante o carregamento dos projetos (entre `OnInitializedAsync` iniciar e completar), a Home exibe um skeleton loading em vez de nada.
2. O skeleton consiste em 3 placeholders pulsantes (cards com `background: var(--rm-text-muted); opacity: 0.2; animation: pulse 1.5s infinite`).
3. Uma nova chave de i18n não é necessária — o skeleton é puramente visual.
4. A animação `pulse` é definida em `app.css` com `@keyframes pulse { 0%, 100% { opacity: 0.2 } 50% { opacity: 0.4 } }`.
5. O skeleton respeita `prefers-reduced-motion` (desativa a animação).

### AC8 — Toast notifications para feedback de auth

1. O `FluentToastProvider` já está no `MainLayout` (spec D1). Esta spec garante que as páginas de auth **não** usam toast (pois Login e Register são POST estáticos que recarregam a página — o feedback vem via query param ou redirect).
2. ForgotPassword e ResetPassword (interativas) podem usar `IToastService` para feedback de erro além do `FluentMessageBar` inline — mas o `FluentMessageBar` é o canal principal.
3. Nenhum toast é adicionado se o `FluentMessageBar` já é suficiente para o feedback.

### AC9 — Layout visual das telas de auth

1. As telas de Login e Register usam um layout centralizado: conteúdo em uma coluna com `max-width: 400px` e `margin: 0 auto`, verticalmente alinhada ao topo com padding superior.
2. As telas de ForgotPassword e ResetPassword seguem o mesmo padrão centralizado com `max-width: 400px`.
3. O título `h3` de cada tela usa `font-family: var(--rm-font-display)` (Outfit) e `color: var(--rm-secondary)` (Teal profundo).
4. Os `FluentTextField` têm `Appearance="Appearance.Outline"` e largura total (`width: 100%`).
5. Os links entre telas (Login ↔ Register, Login ↔ ForgotPassword, ResetPassword → Login) usam `color: var(--rm-primary)` (Coral).

### AC10 — Render mode inalterado

1. `RenderModeTests.cs` continua passando sem modificação.
2. Login, Register, Home permanecem na lista `StaticPages` (sem `@rendermode`).
3. ForgotPassword, ResetPassword permanecem na lista `InteractivePages` (com `@rendermode InteractiveServer`).

### AC11 — Testes existentes passam

1. Os testes de integração continuam passando.

### AC12 — Novas chaves de i18n

1. As novas chaves (`HomeEmptyTitle`, `HomeEmptyDescription`, `HomeEmptyButton`) são adicionadas em ambos os arquivos `.resx` (EN e pt-BR).
2. Todas as chaves existentes continuam funcionando — nenhuma chave é removida ou renomeada.

## Data Model

Nenhum — esta spec não altera o modelo de dados.

## Security Constraints

- Login e Register continuam como POST HTTP estático (sem circuito SignalR) — o cookie de auth não pode ser setado sobre circuito interativo (ADR-0003).
- O antiforgery continua aplicado às rotas não-`/api` (ADR-0004). Os `EditForm` com `FormName` mantêm a proteção antiforgery.
- Os campos `name="email"`, `name="password"`, `name="Name"`, `name="ConfirmPassword"` nos forms de Login e Register devem ser preservados — o backend lê esses nomes do form POST.
- Nenhum PII é exposto em logs.

## API / Event Contract

none — esta spec não altera endpoints ou contratos. Os forms continuam postando para os mesmos endpoints:
- `POST /api/auth/login`
- `POST /api/auth/register-and-login`

## Dependencies

- **D1 (Fundação)** — FluentUI deve estar instalado e configurado, providers no MainLayout, design tokens em `app.css`.
- `Microsoft.FluentUI.AspNetCore.Components` v4 (instalado em D1).
- `Microsoft.FluentUI.AspNetCore.Components.Icons` v4 (instalado em D1).

## Out of Scope

- **Telas de Projeto** (CreateProject, InvitePartner, ProjectDetail, InvitationAccept) — spec D3.
- **CheckIn** — spec D3.
- **Dashboard** — spec D3.
- **Streak de check-ins** — spec D3.
- **Faces de sentimento redesenhadas** — spec D3.
- **Dark mode em si** (implementado em D1) — esta spec apenas garante que as telas de auth funcionam em ambos os temas.
- **PWA** — fora do escopo do MVP.

## Verification

### Build

```powershell
dotnet build ResponsabiliMano.slnx
```

### Testes unitários (bUnit + RenderMode)

```powershell
dotnet test tests/ResponsabiliMano.Web.Tests
```

`RenderModeTests` deve continuar passando — Login, Register, Home na lista `StaticPages`; ForgotPassword, ResetPassword na lista `InteractivePages`.

### Testes de integração

```powershell
dotnet test tests/ResponsabiliMano.Web.IntegrationTests
```

### Verificação manual

1. Rodar a app localmente.
2. Abrir `/login` — layout centralizado, inputs FluentUI, botão coral.
3. Fazer login — redireciona para Home.
4. Verificar Home com projetos — cards FluentUI com badges coloridos.
5. Verificar Home sem projetos — empty state com ícone e CTA.
6. Abrir `/register` — formulário com 4 campos, layout centralizado.
7. Abrir `/forgot-password` — formulário interativo, feedback inline.
8. Abrir `/reset-password?token=...` — formulário interativo com senha e confirmação.
9. Alternar dark mode no OS — todas as telas de auth devem adaptar cores.
10. Abrir no mobile (375px) — conteúdo centralizado, inputs full-width, bottom nav visível.
