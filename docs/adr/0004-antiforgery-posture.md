# 0004 — Postura de antiforgery/CSRF nos endpoints de estado

- **Status:** accepted
- **Data:** 2026-07-27
- **Contexto:** Vários endpoints que alteram estado usam `DisableAntiforgery()`:
  `/api/auth/{login,logout,forgot-password,reset-password}` e todos os
  `/api/projects*` de POST. Eles nasceram assim porque as páginas Blazor (SSR e
  interativas) e os posts de formulário chamavam os minimal APIs sem enviar um
  token antiforgery. A regra `.devin/rules/security.md` marca isso como débito e
  a spec **R1** exigiu preservar o comportamento 1:1 (sem tocar em segurança).
- **Decisão:** Nesta fase (P1) **documentamos e mantemos** o comportamento
  atual, sem removê-lo junto com a extração de endpoints (R1), para não misturar
  refatoração estrutural com mudança de segurança. A correção — reabilitar
  antiforgery nos endpoints chamados pela UI e restringir `DisableAntiforgery()`
  apenas a integrações máquina-a-máquina (cron/webhook) protegidas por segredo —
  fica numa spec **R5** dedicada, que passa pelo loop com testes próprios.
- **Consequências:** Risco de CSRF conhecido e rastreado permanece aberto até R5;
  em contrapartida, R1 entrega sem regressão e a decisão fica auditável. A cookie
  de auth já usa `HttpOnly` + `SameSite=Lax`, o que mitiga parcialmente CSRF em
  navegação cross-site simples, mas **não** substitui o token antiforgery.
- **Alternativas consideradas:** (1) Remover `DisableAntiforgery()` agora —
  quebraria os posts da UI sem antes propagar tokens, violando o escopo 1:1 de
  R1. (2) Mover os endpoints para dentro dos componentes Blazor (chamada direta a
  `IProjectService`) — é a decisão (b) em aberto no ADR-0002, maior que R5.

## Adendo (2026-07-28) — middlewares de UI escopados para fora de `/api`

Ao subir para produção (Cloud Run), **todo POST para `/api/*` que produzia um erro
com corpo vazio** (ex.: `401` do `Results.Unauthorized()`, `404` do feature gate do
cron) voltava como **`400` vazio**. Diagnóstico: `UseStatusCodePagesWithReExecute("/not-found")`
**re-executa** respostas de erro sem corpo re-emitindo a requisição para `/not-found`
com o **método original**. Para GET isso renderiza a página (404); para **POST**, a
re-execução bate no endpoint Razor de `/not-found`, que exige **antiforgery**, e o
resultado vira `400` vazio — mascarando o status real do endpoint `/api`. (A UI chama
os serviços direto, então isso só aparecia via HTTP.)

Correção: os dois middlewares **orientados à UI** passam a ser escopados para
requisições **não-`/api`**, deixando o `/api` com seus status reais (401/404/JSON):

```csharp
app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));
app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseAntiforgery());
```

Antiforgery continua protegendo os formulários Blazor (não-`/api`); no `/api` ele é
redundante (todos já usam `DisableAntiforgery`) e o CSRF é mitigado pelo cookie
`SameSite=Lax` e, no cron, pelo secret `X-Cron-Secret` (ADR-0005). Isso desbloqueia o
cron do Cloud Scheduler (S3.3/S3.4). O restante de R5 (reavaliar CSRF caso a UI passe
a chamar `/api`, ou tokens por request no cron) segue aberto.
