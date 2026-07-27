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
| R1 | Extrair endpoints do `Program.cs` para módulos | draft |
| R9 | Baseline OpenAPI dos endpoints atuais | draft |
| X1 | Aplicar `@rendermode InteractiveServer` nas páginas com EditForm | draft |
| S3.1 | Modelo de dados de check-in | draft |
| S3.2 | Tela de check-in | draft |
| S3.3 | Cronjob de envio de check-in | draft |
| S3.4 | Lembretes de check-in não respondido | draft |

> Todas nascem `draft`. O PM aprova (muda para `approved`) no Gate 1, uma de cada
> vez, antes de implementar.
