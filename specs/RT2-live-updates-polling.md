---
id: RT2
feature: Reatividade — atualização automática das telas
pod: ResponsabiliMano (solo)
priority: P1
iteration: 1 (0.5 dia)
contract: none
tracking: gh-issue-#TBD
status: approved
depends_on: [RT1]
adr: [0003]
---

# RT2 — Atualização ao vivo entre parceiros (polling)

## User Value

Como participante, quero que a tela que estou vendo se atualize sozinha quando meu
parceiro age em outra sessão (aceita o convite, aprova, faz check-in), para eu não
precisar recarregar a página manualmente para ver o que mudou.

## Contexto do bug (Bug B)

Cada usuário tem seu próprio circuito SignalR. Quando B aceita/aprova, a tela aberta
de A não muda — não há push entre circuitos. Escolha do usuário: **polling leve**
(~5s), robusto entre instâncias do Cloud Run, sem infra nova.

## Acceptance Criteria

1. Componente `Components/Shared/AutoRefresh.razor` (não renderiza nada): parâmetros
   `IntervalSeconds` (default 5) e `OnRefresh` (`Func<Task>`). Dono de um
   `PeriodicTimer`; a cada tick invoca `OnRefresh` via `InvokeAsync` (sincroniza no
   renderer). Pula o tick se o anterior ainda roda (`_busy`); só inicia na primeira
   renderização **interativa** (`OnAfterRender(firstRender)`, nunca no prerender);
   `IAsyncDisposable` para o timer/loop.
2. `Home` re-consulta os projetos e re-renderiza só quando o conjunto (id/status/nome)
   muda — reflete "virei parceiro" ou mudança de status sem reload.
3. `ProjectDetail` re-consulta o projeto e atualiza **só o estado de exibição**
   (status, parceiro, metas, change-requests, streak) quando a assinatura do projeto
   muda. **Não** re-semeia os campos do formulário de proposta em edição
   (`_proposeType/_proposeEndDate/_proposeFrequency/_proposeGoals`), nem atualiza
   durante uma ação (`_actionLoading`).
4. `Dashboard` re-consulta e, quando os check-ins/sentimentos mudam, atualiza os cards
   e **re-renderiza o gráfico** (via `_pendingChartRender` no próximo `OnAfterRenderAsync`;
   o JS `dashboardChart.render` já destrói o gráfico anterior).
5. As leituras usadas no polling usam `AsNoTracking()` — `ProjectService.GetProjectAsync`,
   `ProjectService.GetStreakAsync`, `DashboardService.GetDashboardAsync` (e
   `GetUserProjectsAsync`, da RT1) — para não devolver entidades obsoletas do identity
   map do `DbContext` de vida longa do circuito.
6. Build e todos os testes existentes (Infrastructure + Web) permanecem verdes.

## Data Model

Nenhum.

## Security Constraints

- O polling só re-lê via serviços que já validam ownership (`GetProjectAsync`/
  `GetDashboardAsync` lançam `UnauthorizedAccessException` para não-participantes).
  Falhas de poll são silenciosas (logadas), nunca derrubam o circuito.
- Sem novos endpoints; sem PII em log.

## API / Event Contract

none — reusa `IProjectService`/`IDashboardService`.

## Dependencies

- RT1 (Home interativa) — pré-requisito para a Home poder pollar.

## Out of Scope

- Push instantâneo (SignalR backplane / Postgres LISTEN-NOTIFY) — decisão foi polling.
- `InvitationAccept` (fluxo pontual) e telas estáticas (Login/Register).

## Verification

- `dotnet build ResponsabiliMano.slnx`
- `dotnet test tests/ResponsabiliMano.Infrastructure.Tests` e `tests/ResponsabiliMano.Web.Tests`
- Manual (2 navegadores): A abre ProjectDetail (Pendente); B aceita+aprova → a tela de A
  vira "Em Andamento" em ~5s sem reload. B faz check-in → Dashboard/streak de A atualizam
  em ~5s. Digitar no formulário de proposta de A não é apagado por um refresh.
