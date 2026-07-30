---
id: D5
feature: Redesign UI — camada quente sobre FluentUI
pod: ResponsabiliMano (solo)
priority: P1
iteration: 1 (1 dia)
contract: none
tracking: gh-issue-#TBD
status: approved
depends_on: [D4]
adr: [0006]
---

# D5 — Elevação visual das telas de produto

Tira a cara "corporativa/planilha" das telas de produto (ProjectDetail,
InvitationAccept, CheckIn, Dashboard), conserta grids quebrados e deixa tudo
mobile-first e quente. Guiada por `/frontend-design`.

## User Value

Como usuário, quero as telas de projeto, check-in e dashboard bonitas, legíveis no
celular e coerentes com a marca, para acompanhar o progresso com prazer em vez de
encarar tabelas frias.

## Acceptance Criteria

### AC1 — ProjectDetail e InvitationAccept: cards no lugar de DataGrid

1. As metas viram **cards** (`.goal-list` / `.goal-tile`: rótulo + valor alvo em
   destaque + faixa min…max), não mais `FluentDataGrid`.
2. As change-requests viram **cards** (`.cr-list` / `.cr-card`: tipo, autor, data,
   badge de status colorido) mantendo os botões **"Aprovar"/"Rejeitar"** (nomes
   preservados) só para quem pode responder (pendente e não-autor).
3. Cabeçalho com `.detail-title` + `.status-pill` colorido; metadados em `.rm-meta`
   (label/valor). Datas em `dd/MM/yyyy`.
4. **Preservados** para o E2E: `FluentMessageBar` (`div.fluent-messagebar`) de
   status/erro; `h4:has-text('Convite')` e botão **"Aceitar Convite"** na prévia do
   convite; botão **"Aprovar"** + diálogo de confirmação; botão/link **"Convidar Parceiro"**.

### AC2 — Dashboard: grid consertado + cards quentes

1. Remove classes mortas do Bootstrap (`row`, `g-3`, `col-*`, `card-text`); usa
   `.dash-cards` (CSS grid `auto-fill`).
2. Cards de sentimento (`.feeling-card` com `FeelingFace`) e de médias (`.stat-card`)
   quentes; gráfico dentro de `.chart-wrap`. **Preservados**: nome dos participantes,
   `DashboardNoCheckIns`, `DashboardAverages`, seletor de meta (`fluent-select`),
   `BackToProject`, mensagens de erro/estado (bUnit `DashboardTests`).

### AC3 — Faces de sentimento coloridas

`FeelingFace` passa a desenhar um **círculo preenchido** na cor do nível (VerySad
vermelho → VeryHappy verde) com traços brancos — maior e mais legível. Usado no
CheckIn (seletor, mantendo `button[title='<sentimento>']`) e nos cards do Dashboard.

### AC4 — Limpeza de utilitários órfãos

Adiciona uma camada mínima de utilitários de espaçamento em `app.css`
(`.mt-2/.mt-3/.mb-2/.mb-3/.mb-4/.ms-2`) para os `Class="..."` remanescentes do
Bootstrap voltarem a ter efeito, sem editar cada uso.

### AC5 — Dark mode legível

Teal profundo (`--rm-secondary`) recebe variante mais clara no dark mode para títulos
e valores permanecerem legíveis.

### AC6 — Testes

`dotnet build` + `Web.Tests` (Dashboard/Render/Register) + `Infrastructure.Tests`
verdes. E2E `ProjectAndCheckInFlowTests` preservado (seletores das ACs acima).

## Security Constraints

- Sem mudança de lógica/serviço; apenas apresentação. Sem PII novo em tela/log.

## Out of Scope

- CreateProject / InvitePartner (já em FluentUI; sem grids quebrados) — mantidos.
- Novos ícones/ilustrações custom além dos SVGs de sentimento.

## Verification

- `dotnet build ResponsabiliMano.slnx`
- `dotnet test tests/ResponsabiliMano.Web.Tests` e `tests/ResponsabiliMano.Infrastructure.Tests`
- Visual (`/run`): ProjectDetail (metas/CR em cards, pill de status), Dashboard (grid
  de cards, gráfico), CheckIn (faces coloridas) no mobile (375px) e desktop, dark mode.
