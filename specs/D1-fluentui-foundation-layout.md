---
id: D1
feature: Redesign UI — FluentUI Blazor + Design System
pod: ResponsabiliMano (solo)
priority: P1
iteration: 1 (2-3 dias)
contract: none
tracking: gh-issue-#TBD  <!-- criar manualmente: token do git credential manager não tem escopo repo para criar issues via API -->
status: draft
depends_on: []
adr: [0003, 0006]
---

# D1 — Fundação: FluentUI + Tema + Layout

## User Value

Como usuário da aplicação, quero uma interface visual atrativa, responsiva (mobile-first) e com suporte a dark mode, para que a experiência de usar o ResponsabiliMano seja agradável tanto no celular quanto no computador.

## Acceptance Criteria

### AC1 — Pacotes FluentUI instalados e registrados

1. O pacote `Microsoft.FluentUI.AspNetCore.Components` (v4) está referenciado em `ResponsabiliMano.Web.csproj` e em `Directory.Packages.props` como `PackageVersion`.
2. O pacote `Microsoft.FluentUI.AspNetCore.Components.Icons` está referenciado igualmente.
3. `Program.cs` chama `builder.Services.AddFluentUIComponents()` (com `ServiceLifetime.Scoped`, padrão para Blazor Server).
4. `_Imports.razor` contém `@using Microsoft.FluentUI.AspNetCore.Components` e `@using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons`.
5. A aplicação compila sem erros e sem warnings de nullable após a adição.

### AC2 — Bootstrap removido, FluentUI como único framework CSS

1. A tag `<link rel="stylesheet" href="@Assets["lib/bootstrap/dist/css/bootstrap.min.css"]" />` é removida de `App.razor`.
2. A pasta `wwwroot/lib/bootstrap/` é removida.
3. Nenhuma referência a `bootstrap` permanece em `.razor`, `.css` ou `.csproj` do projeto Web.
4. O `<link>` para `app.css` permanece (conterá os design tokens customizados).
5. O `<link>` para `ResponsabiliMano.Web.styles.css` permanece (CSS isolado por componente).

### AC3 — Providers FluentUI no MainLayout

1. `MainLayout.razor` contém os 5 providers obrigatórios: `<FluentToastProvider />`, `<FluentDialogProvider />`, `<FluentMessageBarProvider />`, `<FluentTooltipProvider />`, `<FluentKeyCodeProvider />`.
2. `MainLayout.razor` contém `<FluentDesignTheme>` com `Mode="DesignThemeModes.System"` e `StorageName="rm-theme"`.

### AC4 — Design tokens customizados (paleta "Energia")

1. `app.css` define as custom properties CSS da paleta "Energia" (ver `docs/design-brief.md` §3):
   - `--rm-primary: #E85D4E`, `--rm-primary-dark: #C84A3D`, `--rm-secondary: #0D5C63`, `--rm-accent: #F4A261`, `--rm-bg: #FAF6F0`, `--rm-surface: #FFFFFF`, `--rm-text: #2D2D2D`, `--rm-text-muted: #7A7A7A`.
2. `app.css` define os tokens de dark mode via `@media (prefers-color-scheme: dark)` ou via classe `.dark` no `:root`:
   - `--rm-bg: #1A1A1A`, `--rm-surface: #2A2A2A`, `--rm-text: #E8E8E8`, `--rm-text-muted: #A0A0A0`.
3. `FluentDesignTheme` usa `CustomColor="#E85D4E"` para alinhar o accent do FluentUI com a paleta da app.
4. O `body` usa `background-color: var(--rm-bg)` e `color: var(--rm-text)`.

### AC5 — Google Fonts carregadas

1. `App.razor` inclui `<link>` para Google Fonts carregando `Outfit` (pesos 400, 600, 700) e `Inter` (pesos 400, 600).
2. `app.css` define `--rm-font-display: 'Outfit', sans-serif` e `--rm-font-body: 'Inter', sans-serif`.
3. Headings (`h1`–`h6`) usam `font-family: var(--rm-font-display)`.
4. `body` usa `font-family: var(--rm-font-body)`.

### AC6 — Layout responsivo: sidebar no desktop, bottom nav no mobile

1. `MainLayout.razor` usa `<FluentLayout>` como container raiz.
2. **Desktop (≥ 641px):** Uma sidebar (`FluentNavMenu` ou container customizado) com 240px de largura é exibida à esquerda, contendo logo/título + links de navegação. A sidebar é `position: sticky; top: 0; height: 100vh`.
3. **Mobile (< 641px):** A sidebar é ocultada (`display: none`). Uma bottom navigation bar fixa (`position: fixed; bottom: 0; width: 100%`) é exibida com 3 itens: Home, Novo Projeto, Sair.
4. O conteúdo principal (`FluentBodyContent` ou equivalente) tem `padding: 16px` no mobile e `padding: 24px` no desktop, com `max-width: 900px` centralizado no desktop.
5. A bottom navigation bar tem `z-index: 1000` e não sobrepõe conteúdo (área de conteúdo tem `padding-bottom` suficiente no mobile).

### AC7 — NavMenu refatorado com FluentNavMenu

1. `NavMenu.razor` usa `FluentNavMenu` com `FluentNavLink` para cada item de navegação.
2. Itens visíveis quando autenticado: Home (`Icons.Regular.Size20.Home`), Novo Projeto (`Icons.Regular.Size20.Add`), Sair.
3. Itens visíveis quando não autenticado: Login (`Icons.Regular.Size20.SignIn`), Cadastre-se (`Icons.Regular.Size20.PersonAdd`).
4. O link "Sair" mantém o comportamento de `<form method="post" action="api/auth/logout">` — pode ser um `FluentButton` com `type="submit"` dentro do form, estilizado como nav link.
5. O título da app ("ResponsabiliMano") aparece no topo da sidebar no desktop.

### AC8 — Bottom navigation bar (componente novo)

1. Um novo componente `BottomNav.razor` é criado em `Components/Layout/`.
2. Mostra 3 ícones com labels: Home, Novo Projeto, Sair (quando autenticado) ou Login, Cadastre-se (quando não autenticado).
3. O item ativo é destacado com a cor primária (`--rm-primary`).
4. Visível apenas no mobile via CSS `@media (max-width: 640.98px)`.
5. O item "Sair" mantém o `<form method="post" action="api/auth/logout">`.

### AC9 — Páginas existentes não quebram (compatibilidade)

1. Todas as 14 páginas existentes renderizam sem erro de runtime após a remoção do Bootstrap e adição do FluentUI.
2. As classes Bootstrap usadas nas páginas (`btn`, `btn-primary`, `form-control`, `form-label`, `alert`, `card`, `list-group`, `row`, `col-*`, `mb-3`, `d-flex`, `gap-2`, `text-muted`, `fs-*`) podem permanecer temporariamente — o FluentUI não entra em conflito com classes CSS residuais do Bootstrap que não têm definição. A refatoração tela-a-tela acontece nas specs D2 e D3.
3. `ReconnectModal.razor` continua funcionando (é um componente Blazor interno, não depende de Bootstrap).
4. O `blazor-error-ui` em `MainLayout.razor` mantém sua estrutura e CSS.

### AC10 — Render mode inalterado

1. Nenhuma página muda seu `@rendermode` ou falta dele. O ADR-0003 continua válido: páginas estáticas (Login, Register, Home) permanecem SSR; páginas interativas permanecem `InteractiveServer`.
2. O teste `RenderModeTests.cs` continua passando sem modificação.

### AC11 — E2E e testes existentes passam

1. Os testes E2E (`AuthFlowTests`, `CronAndEmailFlowTests`) passam sem modificação nos seletores críticos. Se um seletor depender de classe Bootstrap que foi removida, o teste é atualizado para usar texto/role.
2. Os testes de integração continuam passando.
3. O teste `RenderModeTests` continua passando.

### AC12 — ADR registrado

1. Um ADR `0006-fluentui-design-system.md` é criado em `docs/adr/` documentando:
   - A decisão de substituir Bootstrap por FluentUI Blazor.
   - A paleta "Energia" e tipografia (Outfit + Inter).
   - A estratégia de layout responsivo (sidebar desktop + bottom nav mobile).
   - A compatibilidade com ADR-0003 (render mode por página, não global).

## Data Model

Nenhum — esta spec não altera o modelo de dados. Apenas UI/layout.

## Security Constraints

- O fluxo de logout (`POST /api/auth/logout` via form) deve continuar funcionando — não pode ser convertido para um link GET.
- O antiforgery continua aplicado às rotas não-`/api` (ver ADR-0004). Forms HTML dentro da sidebar/bottom-nav devem respeitar o antiforgery.
- `FluentDesignTheme` persiste a preferência em `localStorage` — nenhum dado sensível é armazenado.
- Nenhum PII é exposto em logs.

## API / Event Contract

none — esta spec não altera endpoints ou contratos.

## Dependencies

- `Microsoft.FluentUI.AspNetCore.Components` v4 (NuGet) — a versão compatível com .NET 10 deve ser usada (última estável v4.x).
- `Microsoft.FluentUI.AspNetCore.Components.Icons` v4 (NuGet) — mesma versão do core.
- Google Fonts (CDN) — `Outfit` e `Inter`. Fallback para sans-serif genérico se o CDN estiver indisponível.
- Chart.js continua sendo carregado via CDN em `App.razor` (sem mudança).

## Out of Scope

- **Refatoração de telas individuais** (Login, Register, Home, CreateProject, etc.) — specs D2 e D3.
- **Streak de check-ins** — spec D3.
- **Faces de sentimento redesenhadas** — spec D3.
- **Skeleton loading** — spec D2.
- **Empty states ilustrados** — spec D2.
- **Toast notifications em operações** — spec D2.
- **Substituição de classes Bootstrap nas páginas** — specs D2 e D3. Esta spec apenas remove o CSS do Bootstrap; as classes residuais nas páginas não causam erro (são classes sem estilo).
- **PWA / instalação mobile** — fora do escopo do MVP (PRD §9).
- **Animações complexas** — fora do escopo.

## Verification

### Build

```powershell
dotnet build ResponsabiliMano.slnx
```

Deve compilar sem erros.

### Testes unitários (bUnit)

```powershell
dotnet test tests/ResponsabiliMano.Web.Tests
```

`RenderModeTests` deve continuar passando — nenhuma página muda de render mode.

### Testes de integração

```powershell
dotnet test tests/ResponsabiliMano.Web.IntegrationTests
```

### Testes E2E (Playwright)

```powershell
dotnet test tests/ResponsabiliMano.Web.E2ETests
```

Se algum seletor quebrar (ex: `btn-primary` não existe mais como estilo), atualizar o seletor para `role=button` ou texto.

### Verificação manual

1. Rodar a app localmente (`dotnet run --project src/ResponsabiliMano.Web`).
2. Abrir no navegador em viewport mobile (375px) — bottom nav visível, sidebar oculta.
3. Abrir em viewport desktop (1280px) — sidebar visível, bottom nav oculta.
4. Alternar dark mode no OS — `FluentDesignTheme` deve mudar a paleta automaticamente.
5. Navegar entre páginas — nenhuma deve dar erro de runtime.
6. Fazer login e logout — o fluxo deve funcionar via bottom nav (mobile) e sidebar (desktop).
7. Verificar que Google Fonts (Outfit, Inter) estão carregadas (DevTools > Network).
8. Verificar que nenhum arquivo Bootstrap é carregado (DevTools > Network — buscar "bootstrap").
