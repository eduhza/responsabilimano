---
id: R1
feature: Higiene de engenharia (Fase P1)
pod: ResponsabiliMano (solo)
priority: P1
iteration: 1 (2-3 dias)
contract: contracts/responsabilimano-api.yaml
tracking: gh-issue-#TBD
status: done
depends_on: []
adr: [0002]
---

# Extrair endpoints do Program.cs para módulos

## User Value
Como mantenedor, quero os endpoints HTTP organizados em módulos por área para que
`Program.cs` seja só bootstrap, reduzindo risco de regressão e facilitando a
geração de código pela IA.

## Acceptance Criteria
1. `Program.cs` não contém nenhum `app.MapPost/MapGet` inline — apenas
   composição (`app.MapAuthEndpoints()`, `app.MapProjectEndpoints()`).
2. Endpoints agrupados via `MapGroup` + extension methods sobre
   `IEndpointRouteBuilder`, em `Web/Endpoints/AuthEndpoints.cs` e
   `Web/Endpoints/ProjectEndpoints.cs`.
3. Rotas, verbos, respostas e `DisableAntiforgery()` atuais preservados 1:1
   (nenhuma mudança de comportamento observável nesta spec).
4. `Program.cs` reduzido a bootstrap + pipeline (< ~80 linhas).
5. Build e testes existentes continuam verdes.

## Data Model
- Sem mudança.

## Security Constraints
- Comportamento de auth/antiforgery idêntico ao atual (a revisão do antiforgery é
  a spec R5, separada). Sem PII em logs.

## API / Event Contract
Ver contracts/responsabilimano-api.yaml (a baseline vem da spec R9; se R9 ainda
não estiver pronta, os contratos devem ser gerados/atualizados aqui).

## Dependencies
- Idealmente após R9 (baseline OpenAPI) para validar conformance na extração.

## Out of Scope
- Mudar a decisão "Blazor chama serviço direto vs. via API" (isso é o ADR-0002 / R4).
- Alterar validação ou antiforgery (R5).

## Verification
- `dotnet test` verde.
- Diff mostra paridade de rotas (comparar lista de endpoints antes/depois).
- Smoke: login, criar projeto, convidar, aprovar change-request via API.
