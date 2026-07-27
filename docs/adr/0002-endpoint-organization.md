# 0002 — Organização dos endpoints e validação

- **Status:** proposed
- **Data:** 2026-07-27
- **Contexto:** Hoje todos os endpoints HTTP estão inline em `Program.cs`
  (~374 linhas), misturando bootstrap, roteamento e validação. As páginas Blazor
  também postam para esses minimal APIs, o que duplica validação (endpoint +
  componente). Precisamos decidir (a) como organizar os endpoints e (b) se o
  Blazor deve chamar `IProjectService` direto ou continuar via API.
- **Decisão (a decidir na spec R1/R4):** Proposta — extrair os endpoints para
  módulos por área (`Web/Endpoints/*.cs`) via `MapGroup` + extension methods,
  deixando `Program.cs` só com bootstrap; centralizar validação (ex.:
  FluentValidation) para eliminar duplicação. A decisão sobre "Blazor chama
  serviço direto vs. via API" será registrada ao concluir R4.
- **Consequências:** `Program.cs` enxuto e testável; menor risco de regressão; a
  IA gera código mais focado. Custo: refatoração sem mudança de comportamento
  (coberta por testes de paridade).
- **Alternativas consideradas:** Migrar para Controllers MVC (mais cerimônia que
  o necessário para o MVP); manter tudo inline (não escala, dificulta review).
