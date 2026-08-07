# Specs — a fonte da verdade (SDD / AI-Native)

Cada arquivo aqui é uma **spec atômica**: a unidade de trabalho que a IA consome
para gerar código + testes e que o humano aprova (Gate 1). Uma spec = uma
iteração (2–3 dias) = uma entrega.

## Regras

- **Git = requisitos; tracker (GitHub Issues) = status.** O texto da spec vive
  aqui; `tracking:` liga ao item de status. `docs/plan.md` é o índice/roadmap.
- Toda spec segue `_template.md`. Nada de prosa livre — seções parseáveis.
- Ciclo de `status`: `draft → approved → in-progress → done`.
  - `draft`: em escrita/negociação. **A IA não gera código.**
  - `approved`: passou no **Gate 1** (humano). Liberado para implementar.
  - `in-progress`: sendo implementada (loop steps 2–3).
  - `done`: mergeada e aceita (após **Gate 2**).

## Autoria just-in-time (Tenet 3)

Não escrevemos todas as specs de uma vez. Cada spec é escrita **imediatamente
antes** da sua iteração, via o workflow `.devin/workflows/write-spec.md`. Por
isso este diretório contém as specs **imediatas** (refactors de leadoff +
Sprint 3). Sprint 4 (dashboard) e Sprint 5 (polimento) estão no roadmap
(`docs/plan.md`) e viram specs quando chegar a vez delas.

## Convenção de nome

`<id>-<slug>.md` — ex.: `S3.1-checkin-data-model.md`, `R1-extract-endpoints.md`.

- `S*` = specs de produto (features do PRD).
- `R*` = specs de refactor/engenharia (dívida técnica priorizada).
- `X*` = correções de bug/comportamento inesperado.

## Índice atual

| Spec | Título | Status |
|---|---|---|
| R1 | Extrair endpoints do `Program.cs` para módulos | done |
| R9 | Baseline OpenAPI dos endpoints atuais | done |
| X1 | Aplicar `@rendermode InteractiveServer` nas páginas interativas | done |
| S3.1 | Modelo de dados de check-in | done |
| S3.2 | Tela de check-in | done |
| S3.3 | Cronjob de envio de check-in | done |
| S3.4 | Lembretes de check-in não respondido | done |
| S3.5 | Envio real de e-mail (SMTP) + base URL nos links | done |
| S4.1 | Dashboard Data API | done |
| S4.2 | Dashboard UI — Gráfico de Evolução e Cards de Sentimento | done |
| S5.1 | Cobertura de Testes — Endpoints e Regras de Negócio | done |
| S5.2 | UX, Mensagens e i18n — Polimento do MVP | done |
| S5.3 | Produção — Domínio, Deploy Manual e Backup do Banco | done |
| D1 | Fundação: FluentUI + Tema + Layout | draft |
| D2 | Telas Auth + Home com FluentUI | draft |
| S6.1 | Painel global do usuário (`/dashboard`) | in-progress |
| X2 | Corrigir perda de casas decimais na entrada de números | in-progress |
| S7.1 | Editar e cancelar o check-in do período corrente | draft |
| S7.2 | Metas com alvo por participante | draft |
| S7.3 | Negociação meta a meta, com histórico de contrapropostas | draft |
| S7.4 | Tipos de meta booleano e escala | draft |
| S7.5 | Biblioteca de modelos de meta e wizard repensado | draft |

> As specs de produto (`S*`/`X*`) nascem `draft`; o PM aprova (muda para
> `approved`) no Gate 1, uma de cada vez, antes de implementar.

## Fase P3 (metas flexíveis) — Sprint 7

O app nasceu voltado a metas físicas. A Sprint 7 amplia o produto para estudos,
concurso, carreira e hábitos, e corrige dois defeitos que apareceram no uso real
(casas decimais e check-in imutável). Ordem de dependência:

```
X2  ──► S7.1 ──┐                    (correção + alívio imediato)
               ├─► S7.2 ──► S7.3    (alvo individual → negociação meta a meta)
               └─► S7.4 ──► S7.5    (novos tipos → biblioteca de modelos)
```

Decisões do PM que valem para toda a sprint:

- Modelo de metas: **campo comum, alvo por pessoa** (`GoalField` compartilhado +
  `GoalTarget` por participante, com `Baseline` e `GoalDirection`).
- Negociação: **aceite por meta com histórico** de contrapropostas.
- Novos tipos de dado: **booleano** e **escala** (contagem/duração e checklist
  ficaram fora).
- Editar check-in: **apenas o período corrente**, sem trilha de auditoria.
- **Sem correção dos dados já gravados errados** por script (ADR/decisão: corrigir
  pela UI depois da S7.1).
- **Sem nova feature flag** (ADR-0008): poucos usuários, e uma flag duplicaria o
  caminho de código no fluxo do acordo. Segurança de rollout fica nas migrations
  com backfill — que devem rodar em banco com dados sem erro.

## Fase P1 (higiene de engenharia) — feito

A primeira leva de refactors rodou como dogfooding do processo:

- **R1** (`done`) — endpoints extraídos para `Web/Endpoints/*.cs`; `Program.cs` só compõe.
- **R3** — `Class1.cs` removidos de `Core` e `Infrastructure`.
- **R7** — feature flags (`Microsoft.FeatureManagement`); flag `CheckIns` desligada (deploy ≠ release).
- **R8** — health checks (`/health`, `/health/ready`) + logging estruturado (Serilog).
- **R9** (`done`) — baseline OpenAPI em `contracts/responsabilimano-api.yaml` + gate de conformidade no CI.
- **R5** — postura de antiforgery revisada e registrada no `docs/adr/0004`; correção fica para spec futura.
- **R4** — decisão de organização de endpoints aceita (`docs/adr/0002`); centralização de validação segue como débito.

R3/R7/R8 não têm arquivo de spec próprio (mudanças pequenas/infra); estão
rastreadas aqui e no plano de ação (`docs/plano-ai-native-sdlc.md`, §4.8).

## Fase P2 (retomar o produto) — Sprint 3 feito

Segunda leva pelo loop: specs `approved` no Gate 1, implementadas + testadas e
aceitas no Gate 2 (`done`). Toda a feature nasce **atrás da flag `CheckIns`**
(desligada em produção; ligada em `Development`) — o release fica para o Gate 3:

- **S3.1** — `CheckIn`/`CheckInMetric` (já modeladas na Sprint 0); testes de
  unicidade por período e cascade adicionados.
- **S3.2** — `ICheckInService` + página `/projects/{id}/checkin` (feeling em 5
  rostos SVG, campos gerados das metas) + `POST /api/projects/{id}/checkins`.
- **S3.3** — `ICheckInNotificationService.DispatchCheckInEmailsAsync` +
  `POST /api/cron/checkins/dispatch` (secret `X-Cron-Secret`, ver `docs/adr/0005`),
  idempotente via `CheckInNotification`.
- **S3.4** — `DispatchRemindersAsync` + `POST /api/cron/checkins/reminders`;
  lembra só quem não preencheu, idempotente por período.
- **S3.5** — `SmtpEmailService` (MailKit) habilita envio real de e-mail;
  seleção por presença de `EmailSettings__SmtpPassword` (senão log). Corrige o
  link de recuperação de senha para usar o baseUrl da requisição.

`PeriodCalculator` (Core) deriva o período corrente a partir da frequência do
projeto — nunca confiado do cliente. Contrato de evento em
`contracts/checkin-events.yaml` (AsyncAPI); endpoints no OpenAPI + gate verde.
