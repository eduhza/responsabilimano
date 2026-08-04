---
id: D3
feature: Redesign UI — FluentUI Blazor + Design System
pod: ResponsabiliMano (solo)
priority: P1
iteration: 1 (2-3 dias)
contract: none
tracking: gh-issue-#TBD
status: done
depends_on: [D1, D2]
adr: [0003, 0006]
---

# D3 — Telas de Projeto + CheckIn + Dashboard com FluentUI

## User Value

Como usuário, quero telas de gestão de projetos, check-in e dashboard com visual profissional, feedback visual claro (streak, faces de sentimento coloridas) e estados de carregamento/erro elegantes, para que a experiência de uso diário do ResponsabiliMano seja motivadora e agradável.

## Acceptance Criteria

### AC1 — CreateProject refatorada com FluentUI

1. `CreateProject.razor` mantém `@rendermode InteractiveServer` (ADR-0003).
2. O `EditForm` usa `FluentTextField` para Nome, `FluentDatePicker` para datas, `FluentSelect` para frequência.
3. Os campos de meta (goal fields) usam `FluentCard` por meta (em vez de `card` Bootstrap), com `FluentTextField`/`FluentNumberField`/`FluentSelect` para os campos internos.
4. O botão "Adicionar meta" usa `FluentButton` com `Appearance="Appearance.Outline"` e ícone `Icons.Regular.Size20.Add`.
5. O botão "Remover" usa `FluentButton` com `Appearance="Appearance.Outline"` e `IconColor="Color.Error"`.
6. O botão de submit usa `FluentButton` com `Appearance="Appearance.Accent"`.
7. O estado de sucesso exibe `FluentMessageBar` com `Severity="MessageBarSeverity.Success"` + botões "Convidar Parceiro" e "Voltar para Home".
8. O erro (`_error`) é exibido via `FluentMessageBar` com `Severity="MessageBarSeverity.Error"`.
9. O input de nome mantém `id="name"` — o E2E testa `#name`.
10. O título `h3` com `@Localizer["CreateProjectTitle"]` (pt-BR: "Criar Projeto") é mantido — o E2E testa `h3:has-text('Criar Projeto')`.
11. O botão de submit mantém o nome acessível — o E2E testa `GetByRole(AriaRole.Button, { Name = "Criar Projeto" })`.
12. O estado de sucesso mantém `div.alert-success` ou equivalente — o E2E testa `div.alert-success`. **Nota:** Se `FluentMessageBar` não renderizar com classe `alert-success`, o E2E deve ser atualizado para usar texto/role.
13. Os campos de meta dentro de cards devem manter a classe `form-control` ou equivalente selecionável — o E2E testa `.card .form-control` para preencher campos. Se FluentUI não usar essas classes, o seletor E2E deve ser atualizado para `[class*='card'] input` ou similar.

### AC2 — InvitePartner refatorada com FluentUI

1. `InvitePartner.razor` mantém `@rendermode InteractiveServer` (ADR-0003).
2. O `EditForm` usa `FluentTextField` para o e-mail do parceiro (em vez de `InputText`).
3. O input mantém `id="partnerEmail"` — o E2E testa `#partnerEmail`.
4. O botão de submit usa `FluentButton` com `Appearance="Appearance.Accent"`.
5. O estado de sucesso exibe `FluentMessageBar` com `Severity="MessageBarSeverity.Success"`.
6. O erro é exibido via `FluentMessageBar` com `Severity="MessageBarSeverity.Error"`.
7. O título `h3` com `@Localizer["InvitePartnerTitle"]` (pt-BR: "Convidar Parceiro") é mantido — o E2E testa `h3:has-text('Convidar Parceiro')`.
8. O botão mantém o nome acessível — o E2E testa `GetByRole(AriaRole.Button, { Name = "Convidar Parceiro" })`.
9. O estado de sucesso deve ser detectável pelo E2E — atualizar seletor se `alert-success` não existir mais.

### AC3 — ProjectDetail refatorada com FluentUI

1. `ProjectDetail.razor` mantém `@rendermode InteractiveServer` (ADR-0003).
2. O estado de carregamento (`_loading`) exibe skeleton loading (3 linhas pulsantes) em vez de `<p>Loading...</p>`.
3. O erro "projeto não encontrado" usa `FluentMessageBar` com `Severity="MessageBarSeverity.Error"`.
4. As informações do projeto (status, datas, frequência, criador, parceiro) usam `FluentStack` horizontal com `FluentCard` ou labels estilizadas (em vez de `row`/`col-md-*`).
5. A tabela de metas usa `FluentDataGrid` (em vez de `table.table-striped`).
6. O botão "Fazer Check-in" usa `FluentButton` com `Appearance="Appearance.Accent"` e ícone `Icons.Regular.Size20.CheckmarkCircle`.
7. O botão "Dashboard" usa `FluentButton` com `Appearance="Appearance.Neutral"` e ícone `Icons.Regular.Size20.ChartMultiple`.
8. O botão "Convidar Parceiro" (quando não há parceiro) usa `FluentButton` com `Appearance="Appearance.Outline"`.
9. A seção "Aprovar Projeto" usa `FluentCard` com `FluentButton` de `Appearance="Appearance.Accent"`.
10. A tabela de change requests usa `FluentDataGrid` com botões "Aprovar"/"Rejeitar" como `FluentButton`.
11. A seção "Propor Mudança" usa `FluentCard` com `FluentSelect` para tipo de mudança, `FluentDatePicker` para nova data final, `FluentSelect` para nova frequência.
12. O formulário de propostas de metas usa `FluentCard` por meta (mesmo padrão de CreateProject).
13. As mensagens de status (`_statusMessage`) usam `FluentMessageBar` com `Severity="MessageBarSeverity.Info"`.
14. O botão "Voltar para Home" usa `FluentButton` com `Appearance="Appearance.Neutral"`.

### AC4 — InvitationAccept refatorada com FluentUI

1. `InvitationAccept.razor` mantém `@rendermode InteractiveServer` (ADR-0003).
2. O estado de carregamento exibe skeleton loading.
3. O erro usa `FluentMessageBar` com `Severity="MessageBarSeverity.Error"`.
4. O card de convite usa `FluentCard` com informações do projeto em `FluentStack`.
5. A lista de metas usa `FluentList` ou lista estilizada (em vez de `<ul>` simples).
6. O botão "Aceitar Convite" usa `FluentButton` com `Appearance="Appearance.Accent"`.
7. O título `h4` com `@Localizer["InvitationTitle"]` (pt-BR: "Convite") é mantido — o E2E testa `h4:has-text('Convite')`.
8. O botão mantém o nome acessível — o E2E testa `GetByRole(AriaRole.Button, { Name = "Aceitar Convite" })`.
9. Após aceitar, o botão "Aprovar" (se projeto pendente) usa `FluentButton` — o E2E testa `GetByRole(AriaRole.Button, { Name = "Aprovar" })` e `button:has-text('Aprovar')`.
10. A mensagem de sucesso "Convite aceito" usa `FluentMessageBar` com `Severity="MessageBarSeverity.Success"`.
11. A mensagem de status após aprovação usa `FluentMessageBar` com `Severity="MessageBarSeverity.Info"` — o E2E testa `div.alert-info:has-text('aprovado')`. Atualizar seletor se necessário.

### AC5 — CheckIn refatorada com FluentUI

1. `CheckIn.razor` mantém `@rendermode InteractiveServer` (ADR-0003).
2. O título `h3` com `@Localizer["CheckInTitle"]` (pt-BR: "Check-in") é mantido — o E2E testa `h3:has-text('Check-in')`.
3. Os campos de métricas usam `FluentNumberField` (em vez de `input type="number"`). O E2E testa `input[type='number']` — se `FluentNumberField` renderizar `<input type="number">`, o seletor continua funcionando; caso contrário, atualizar E2E.
4. O seletor de sentimento (faces) é redesenhado (ver AC6).
5. O botão "Enviar check-in" usa `FluentButton` com `Appearance="Appearance.Accent"`.
6. O botão mantém o nome acessível — o E2E testa `GetByRole(AriaRole.Button, { Name = "Enviar check-in" })`.
7. O estado de sucesso exibe `FluentMessageBar` com `Severity="MessageBarSeverity.Success"` — o E2E testa `div.alert-success:has-text('Check-in registrado')`. Atualizar seletor se necessário.
8. O erro de validação (valor fora do range) exibe `FluentMessageBar` com `Severity="MessageBarSeverity.Error"` — o E2E testa `div.alert-danger` e verifica texto "maximum". Atualizar seletor se necessário.
9. O estado "já registrou" exibe `FluentMessageBar` com `Severity="MessageBarSeverity.Warning"` ou `Success` — o E2E testa `div.alert-success` e texto "já registrou". Atualizar seletor se necessário.
10. O botão "Voltar ao Projeto" usa `FluentButton` com `Appearance="Appearance.Neutral"`.
11. Skeleton loading enquanto carrega os dados do projeto e metas.

### AC6 — Faces de sentimento redesenhadas

1. As 5 faces de sentimento são redesenhadas como SVGs maiores (48x48px em vez do tamanho atual) e coloridas:
   - **VerySad** (`#E85D4E` — Coral): boca curvada para baixo, olhos fechados
   - **Sad** (`#F4A261` — Âmbar): boca levemente curvada para baixo
   - **Neutral** (`#B0B0B0` — Cinza): boca reta
   - **Happy** (`#5B9279` — Verde suave): boca sorrindo
   - **VeryHappy** (`#2D9D78` — Verde vibrante): boca sorrindo aberta, olhos felizes
2. Cada face é um `<button>` com `title` contendo o texto localizado — o E2E testa `button[title='Bem']` (Happy em pt-BR). Os titles devem ser mantidos: `title="@Localizer["FeelingVerySad"]"`, `title="@Localizer["FeelingSad"]"`, etc.
3. A face selecionada tem um anel visual (border ou box-shadow) com a cor da paleta primária (`--rm-primary`).
4. As faces não selecionadas têm `opacity: 0.6` e aumentam para `1.0` no hover.
5. As faces são dispostas horizontalmente com `gap: 12px`, centralizadas.
6. O SVG é inline (não imagem externa) para permitir estilização via CSS.
7. O nome do sentimento aparece abaixo de cada face em texto pequeno (`font-size: 0.75rem`).

### AC7 — Dashboard refatorada com FluentUI

1. `Dashboard.razor` mantém `@rendermode InteractiveServer` (ADR-0003).
2. O seletor de meta usa `FluentSelect` (em vez de `<select>` HTML).
3. O gráfico Chart.js é mantido — o container do canvas permanece com `id` ou classe selecionável. O JS interop para Chart.js não é alterado.
4. Os cards de sentimento e médias usam `FluentCard` (em vez de `card` Bootstrap).
5. Os SVGs de sentimento no dashboard usam as mesmas faces redesenhadas (AC6), em tamanho menor (32x32px).
6. O estado de erro usa `FluentMessageBar` com `Severity="MessageBarSeverity.Error"`.
7. O estado "sem check-ins" exibe um empty state com ícone e mensagem `@Localizer["DashboardNoCheckIns"]`.
8. Skeleton loading enquanto carrega os dados do dashboard.
9. O título `h3` com `@Localizer["DashboardTitle"]` é mantido.
10. O botão "Voltar ao Projeto" usa `FluentButton` com `Appearance="Appearance.Neutral"`.
11. As cores do gráfico Chart.js são atualizadas para usar a paleta "Energia": linha principal `#E85D4E` (Coral), grid `--rm-text-muted` com opacity 0.1.

### AC8 — Streak de check-ins (elemento assinatura)

1. Um novo componente `StreakIndicator.razor` é criado em `Components/Shared/`.
2. O componente recebe `int CurrentStreak` e `int BestStreak` como parâmetros.
3. O streak é calculado como o número de check-ins consecutivos completados no prazo (não perdidos).
4. A implementação do cálculo de streak é feita no `DashboardService` ou `ProjectService` (backend) — adicionar método `GetStreakAsync(projectId, userId)` que retorna `(int current, int best)`.
5. O componente exibe:
   - Um ícone de chama (SVG inline ou `Icons.Regular.Size24.Flame` se disponível, caso contrário SVG customizado) com a cor `--rm-accent` (Âmbar).
   - O número do streak atual em `font-family: var(--rm-font-display)`, `font-size: 2rem`, `font-weight: 700`, `color: var(--rm-accent)`.
   - O texto "dias consecutivos" (i18n: `StreakDays` — EN: "day streak" / PT-BR: "dias consecutivos").
   - O recorde em texto menor: "Recorde: {N}" (i18n: `StreakBest` — EN: "Best: {N}" / PT-BR: "Recorde: {N}").
6. O `StreakIndicator` é exibido no `ProjectDetail.razor` (quando o projeto está Active e check-ins habilitados) e no `Dashboard.razor` (no topo, acima dos cards).
7. Quando o streak é 0, o ícone de chama tem `opacity: 0.3` e o número mostra "0".
8. Animação sutil no ícone de chama quando streak > 0: `animation: flicker 2s infinite` (definida em `app.css`). Respeita `prefers-reduced-motion`.
9. Novas chaves de i18n: `StreakDays`, `StreakBest` em ambos `.resx`.

### AC9 — Skeleton loading nas telas de Projeto e Dashboard

1. `ProjectDetail.razor` exibe skeleton (3 linhas pulsantes) durante `_loading`.
2. `InvitationAccept.razor` exibe skeleton durante `_loading`.
3. `CheckIn.razor` exibe skeleton enquanto carrega dados do projeto e metas.
4. `Dashboard.razor` exibe skeleton enquanto carrega dados.
5. O skeleton usa a animação `pulse` já definida em `app.css` (spec D2).
6. Todos os skeletons respeitam `prefers-reduced-motion`.

### AC10 — Empty states

1. `Dashboard.razor` — quando não há check-ins, exibe empty state com ícone `Icons.Regular.Size48.ChartMultiple` (ou similar), mensagem `@Localizer["DashboardNoCheckIns"]` e texto explicativo.
2. `ProjectDetail.razor` — quando não há change requests, o texto `@Localizer["NoChangeRequests"]` é exibido com estilo muted (já existe, apenas estilizar com `color: var(--rm-text-muted)`).
3. Novas chaves de i18n para empty states do dashboard:
   - `DashboardEmptyDescription` — EN: "Complete your first check-in to see progress charts and insights." / PT-BR: "Complete seu primeiro check-in para ver gráficos de progresso e insights."

### AC11 — Dialogs de confirmação

1. A ação "Aprovar Projeto" no `ProjectDetail.razor` e `InvitationAccept.razor` usa `FluentDialog` de confirmação antes de executar (via `IDialogService.ShowDialogAsync`).
2. O dialog contém: título "Aprovar Projeto", mensagem `@Localizer["ApproveProjectDescription"]`, botões "Aprovar" (Accent) e "Cancelar" (Neutral).
3. A ação "Aprovar/Rejeitar Change Request" no `ProjectDetail.razor` usa dialog de confirmação.
4. Os dialogs usam o `FluentDialogProvider` já presente no `MainLayout` (spec D1).
5. Novas chaves de i18n:
   - `ConfirmApproveTitle` — EN: "Confirm Approval" / PT-BR: "Confirmar Aprovação"
   - `ConfirmRejectTitle` — EN: "Confirm Rejection" / PT-BR: "Confirmar Rejeição"
   - `ConfirmRejectMessage` — EN: "Are you sure you want to reject this change request?" / PT-BR: "Tem certeza que deseja rejeitar esta solicitação de mudança?"

### AC12 — Render mode inalterado

1. `RenderModeTests.cs` continua passando sem modificação.
2. CreateProject, InvitePartner, ProjectDetail, InvitationAccept, CheckIn, Dashboard permanecem na lista `InteractivePages` (com `@rendermode InteractiveServer`).

### AC13 — E2E e testes existentes passam

1. Os testes E2E (`ProjectAndCheckInFlowTests`) passam. Seletores críticos preservados ou atualizados:
   - `h3:has-text('Criar Projeto')` no CreateProject
   - `#name` no CreateProject
   - `.card .form-control` para campos de meta — **pode necessitar atualização** se FluentCard não usar classe `card`
   - `GetByRole(AriaRole.Button, { Name = "Criar Projeto" })` no CreateProject
   - `div.alert-success` — **pode necessitar atualização** para `FluentMessageBar`
   - `h3:has-text('Convidar Parceiro')` no InvitePartner
   - `#partnerEmail` no InvitePartner
   - `GetByRole(AriaRole.Button, { Name = "Convidar Parceiro" })` no InvitePartner
   - `h4:has-text('Convite')` no InvitationAccept
   - `GetByRole(AriaRole.Button, { Name = "Aceitar Convite" })` no InvitationAccept
   - `GetByRole(AriaRole.Button, { Name = "Aprovar" })` no InvitationAccept/ProjectDetail
   - `div.alert-info:has-text('aprovado')` — **pode necessitar atualização**
   - `h3:has-text('Check-in')` no CheckIn
   - `input[type='number']` no CheckIn — **pode necessitar atualização** se FluentNumberField não renderizar `type="number"`
   - `button[title='Bem']` no CheckIn — **deve ser mantido** (title do botão de sentimento)
   - `GetByRole(AriaRole.Button, { Name = "Enviar check-in" })` no CheckIn
   - `div.alert-success:has-text('Check-in registrado')` — **pode necessitar atualização**
   - `div.alert-danger` com texto "maximum" — **pode necessitar atualização**
2. A prioridade é manter os seletores funcionando. Se a troca de componente FluentUI mudar o DOM, o teste E2E é atualizado para usar seletores equivalentes (texto, role, aria-label).
3. Os testes de integração continuam passando.

### AC14 — Novas chaves de i18n

1. Novas chaves adicionadas em ambos os `.resx` (EN e pt-BR):
   - `StreakDays` — EN: "day streak" / PT-BR: "dias consecutivos"
   - `StreakBest` — EN: "Best: {0}" / PT-BR: "Recorde: {0}"
   - `DashboardEmptyDescription` — EN: "Complete your first check-in to see progress charts and insights." / PT-BR: "Complete seu primeiro check-in para ver gráficos de progresso e insights."
   - `ConfirmApproveTitle` — EN: "Confirm Approval" / PT-BR: "Confirmar Aprovação"
   - `ConfirmRejectTitle` — EN: "Confirm Rejection" / PT-BR: "Confirmar Rejeição"
   - `ConfirmRejectMessage` — EN: "Are you sure you want to reject this change request?" / PT-BR: "Tem certeza que deseja rejeitar esta solicitação de mudança?"
2. Todas as chaves existentes continuam funcionando.

## Data Model

### Novo: Streak de check-ins

- Adicionar método `GetStreakAsync(Guid projectId, Guid userId)` ao `IProjectService` (ou `IDashboardService` se existir).
- O cálculo: percorrer os check-ins ordenados por período descendente; contar consecutivos que foram completados (não perdidos) a partir do mais recente.
- Retorno: `(int CurrentStreak, int BestStreak)`.
- **Não altera o schema do banco** — o streak é calculado em runtime a partir dos check-ins existentes.
- **Não adiciona entidade nova** — é uma query/computação sobre `CheckIn` existente.

## Security Constraints

- Todas as páginas mantêm `[Authorize]` e validam que o usuário atual tem acesso ao projeto.
- Os dialogs de confirmação não substituem a validação de autorização no backend — `ApproveProjectAsync`, `RespondToChangeRequestAsync`, etc. continuam validando permissões.
- O antiforgery continua aplicado (ADR-0004) — os `EditForm` com `FormName` mantêm a proteção.
- Nenhum PII é exposto em logs.

## API / Event Contract

none — esta spec não altera endpoints ou contratos. O cálculo de streak é feito via serviços de domínio existentes, não via novo endpoint.

## Dependencies

- **D1 (Fundação)** — FluentUI instalado, providers no MainLayout, design tokens.
- **D2 (Auth + Home)** — Padrões visuais estabelecidos (FluentTextField, FluentButton, FluentMessageBar, skeleton, empty state).
- `Microsoft.FluentUI.AspNetCore.Components` v4 (instalado em D1).
- `Microsoft.FluentUI.AspNetCore.Components.Icons` v4 (instalado em D1).
- Chart.js (CDN) — já carregado, sem mudança.

## Out of Scope

- **Novos endpoints ou contratos API** — streak é computado em runtime.
- **PWA** — fora do escopo do MVP.
- **Notificações push** — fora do escopo.
- **Animações complexas** (parallax, scroll-triggered) — fora do escopo.
- **Ilustrações customizadas** — usar SVGs simples inline.
- **Refatoração do backend** — apenas adicionar o método de cálculo de streak.
- **Internacionalização adicional** — apenas as novas chaves listadas em AC14.

## Verification

### Build

```powershell
dotnet build ResponsabiliMano.slnx
```

### Testes unitários (bUnit + RenderMode)

```powershell
dotnet test tests/ResponsabiliMano.Web.Tests
```

`RenderModeTests` deve continuar passando — CreateProject, InvitePartner, ProjectDetail, InvitationAccept, CheckIn, Dashboard na lista `InteractivePages`.

### Testes de integração

```powershell
dotnet test tests/ResponsabiliMano.Web.IntegrationTests
```

### Testes E2E (Playwright)

```powershell
dotnet test tests/ResponsabiliMano.Web.E2ETests --filter "ProjectAndCheckInFlowTests"
```

Testes críticos que devem passar:
- `Create_project_invite_accept_approve_and_check_in` — fluxo completo.
- `CheckIn_rejects_out_of_range_value` — validação de range.
- `CheckIn_prevents_duplicate_submission` — check-in duplicado.

### Verificação manual

1. Rodar a app localmente.
2. Criar um projeto — formulário FluentUI, cards de metas, validação funcionando.
3. Convidar parceiro — formulário FluentUI, feedback de sucesso.
4. Aceitar convite — card de convite, botão de aceitar, aprovação com dialog.
5. Fazer check-in — faces de sentimento coloridas e selecionáveis, campos numéricos, validação.
6. Verificar streak no ProjectDetail e Dashboard — chama, número, recorde.
7. Abrir Dashboard — gráfico Chart.js com cores da paleta, cards de sentimento, empty state se sem check-ins.
8. Alternar dark mode — todas as telas adaptam cores.
9. Abrir no mobile (375px) — conteúdo responsivo, bottom nav visível.
10. Verificar skeleton loading em ProjectDetail, CheckIn e Dashboard durante carregamento.
11. Verificar dialogs de confirmação ao aprovar projeto e ao responder change request.
