---
id: S0.0                      # S* produto | R* refactor | X* correção
feature: <Feature/Sprint a que pertence>
pod: ResponsabiliMano (solo)
priority: P1                  # P0 | P1 | P2
iteration: 1 (2-3 dias)
contract: contracts/<arquivo>.yaml   # ou "none"
tracking: gh-issue-#NN
status: draft                 # draft | approved | in-progress | done
depends_on: []                # ex.: [S3.1]
adr: []                       # ADRs relacionados, ex.: [0002]
---

# <Título curto>

## User Value
Como <ator>, quero <objetivo> para <benefício>.

## Acceptance Criteria
<!-- Cada critério deve ser testável e virar ao menos um teste. -->
1.
2.
3.

## Data Model
<!-- Entidades/campos novos ou alterados. Omitir se não aplicável. -->
-

## Security Constraints
<!-- Auth, ownership, validação de input, sem PII em log, antiforgery, etc. -->
-

## API / Event Contract
<!-- Referência ao contrato OpenAPI/AsyncAPI, ou "none". -->
Ver contracts/<arquivo>.yaml

## Dependencies
-

## Out of Scope
-

## Verification
<!-- Como provar que "pronto": testes, comandos, passos manuais. -->
-
