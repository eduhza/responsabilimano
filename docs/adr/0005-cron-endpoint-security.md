# 0005 — Segurança dos endpoints de cron (check-in)

- **Status:** accepted
- **Data:** 2026-07-27
- **Contexto:** As specs **S3.3** (envio periódico) e **S3.4** (lembretes) exigem
  processamento disparado por um agendador externo. No GCP isso é o **Cloud
  Scheduler** chamando um endpoint HTTP. Esse endpoint altera estado (envia
  e-mails, grava `CheckInNotification`) mas **não** tem um usuário/cookie por
  trás — é máquina-a-máquina. Precisa de autenticação própria e não pode usar o
  token antiforgery (que depende de sessão), o que reabre a questão registrada no
  ADR-0004 sobre onde `DisableAntiforgery()` é legítimo.
- **Decisão:** Os endpoints `POST /api/cron/checkins/dispatch` e
  `/api/cron/checkins/reminders`:
  1. Ficam sob o grupo `/api/cron`, **gated pela feature flag `CheckIns`** (R7):
     respondem 404 enquanto a flag está desligada.
  2. Autenticam por **segredo compartilhado** no header `X-Cron-Secret`,
     comparado em tempo constante (`CryptographicOperations.FixedTimeEquals`)
     com `Cron:Secret` da configuração. **Fail-closed:** sem segredo configurado,
     o endpoint responde 401 (nunca fica aberto por engano).
  3. Usam `DisableAntiforgery()` — legítimo aqui por serem M2M protegidos por
     segredo, exatamente a exceção prevista no ADR-0004 ("restringir
     `DisableAntiforgery()` a integrações máquina-a-máquina protegidas por
     segredo").
- **Consequências:** O segredo vem do **Secret Manager** em produção
  (`Cron__Secret`), nunca commitado; `appsettings.json` traz a chave vazia
  (fail-closed) e `appsettings.Development.json` um valor de dev. Sem PII em logs
  (loga contagem e ids, não e-mails). Limitação conhecida: um único segredo
  estático, sem rotação automática — aceitável para o MVP solo; rotação/HMAC por
  requisição fica como débito se o vetor de risco crescer.
- **Alternativas consideradas:** (1) **OIDC do Cloud Scheduler** (token Google
  assinado, verificado pela app) — mais forte, porém mais setup; adotável depois
  sem quebrar o contrato, trocando só a verificação. (2) **Cloud Tasks / Pub/Sub**
  com push autenticado — overkill para dois jobs. (3) Endpoint sem proteção
  atrás de rede privada — frágil e contraria a regra de "sem endpoint de estado
  público".
