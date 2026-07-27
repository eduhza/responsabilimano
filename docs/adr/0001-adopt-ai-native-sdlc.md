# 0001 — Adotar o AI-Native SDLC com 3 gates humanos

- **Status:** accepted
- **Data:** 2026-07-27
- **Contexto:** O projeto é desenvolvido com um agente de IA (Devin) por um único
  desenvolvedor que acumula todos os papéis. Queremos que o loop de
  desenvolvimento rode de forma autônoma, com intervenção humana apenas onde ela
  agrega mais valor, seguindo o framework AI-Native SDLC da Dell.
- **Decisão:** Adotar o loop de 5 passos (Spec → Gerar → Validar → Revisar →
  Deploy) com a IA como autora primária e o humano como curador. A intervenção
  humana é limitada a **3 gates**: (1) aprovar a spec; (2) aprovar o MR após CI
  100% verde; (3) aceitar a feature (E2E) antes de ligar a feature flag. As
  "Guilds" do framework são codificadas como rules + checks de CI (não pessoas).
  Specs atômicas vivem em `specs/`; decisões em `docs/adr/`; status em GitHub Issues.
- **Consequências:** Ganho de autonomia e rastreabilidade; exige disciplina de
  escrever specs boas antes de gerar. O CI ganha etapas de spec conformance e
  contract test. `docs/plan.md` vira índice; a fonte da verdade migra para `specs/`.
- **Alternativas consideradas:** Manter o fluxo por sprints de 2 semanas
  (descartado: não aproveita a geração por IA nem reduz o cycle time);
  colapsar tudo em "modo único" solo (descartado: perde a separação
  produção/curadoria que gera o valor do modelo).
