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

## Adendo (2026-07-28) — antiforgery escopado à UI (fecha parte de R5)

Ao subir para produção (Cloud Run), descobrimos que os POST de `/api/*` recebiam
**400 do middleware de antiforgery** mesmo com `DisableAntiforgery()` por endpoint
(o `DisableAntiforgery` não estava sendo honrado de forma consistente para os grupos
`/api/projects` e `/api/cron`; a UI usa os serviços diretamente, então o problema
só aparecia via HTTP). Como **todos** os endpoints `/api` já eram antiforgery-exempt
por intenção (este ADR) e a UI **não** chama `/api`, escopamos o middleware:

```csharp
app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseAntiforgery());
```

Antiforgery continua protegendo os formulários Blazor (não-`/api`). O `/api` fica
exempt, apoiado no cookie `SameSite=Lax` (bloqueia POST cross-site) para endpoints
de usuário e no secret `X-Cron-Secret` para o cron (ADR-0005). Isso desbloqueia o
cron do Cloud Scheduler (S3.3/S3.4). O restante de R5 (reavaliar CSRF caso a UI passe
a chamar `/api`, ou tokens por request no cron) segue aberto.
