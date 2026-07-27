---
id: R9
feature: Contratos e evolução segura (Fase P1)
pod: ResponsabiliMano (solo)
priority: P1
iteration: 1 (2-3 dias)
contract: contracts/responsabilimano-api.yaml
tracking: gh-issue-#TBD
status: draft
depends_on: []
adr: []
---

# Baseline OpenAPI dos endpoints atuais (brownfield)

## User Value
Como mantenedor, quero um contrato OpenAPI que descreva os endpoints existentes
para servir de baseline de contract testing e permitir evolução incremental
segura (Tenet 6).

## Acceptance Criteria
1. `contracts/responsabilimano-api.yaml` (OpenAPI 3.1) descreve os endpoints
   atuais: `/api/auth/{register,login,logout,forgot-password,reset-password}` e
   `/api/projects` (+ `/{id}`, `/invite`, `/approve`, `/change-requests`,
   `/change-requests/{crId}/respond`).
2. Cada operação documenta request body, respostas (200/201/400/401/403/409) e
   schemas coerentes com os `Models/*Request.cs` e os retornos atuais.
3. Um job de **contract test** no CI valida a implementação contra o contrato.
4. O `.devin/rules/contracts.md` é referenciado; specs novas apontam para este
   arquivo no campo `contract:`.

## Data Model
- Sem mudança de banco. Schemas do contrato refletem os DTOs existentes.

## Security Constraints
- Documentar o esquema de auth (cookie `ResponsabiliMano.Auth`) nas operações
  protegidas. Sem expor segredos no contrato.

## API / Event Contract
Este é o artefato produzido.

## Dependencies
- Nenhuma (é o provider baseline).

## Out of Scope
- Contratos de evento (AsyncAPI) do cron — virão com S3.3.
- Refatorar os endpoints (R1).

## Verification
- Lint do OpenAPI (ex.: Spectral) verde.
- Contract test roda no CI e passa contra a implementação atual.
