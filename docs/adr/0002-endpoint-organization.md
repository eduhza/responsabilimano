# 0002 — Organização dos endpoints e validação

- **Status:** accepted
- **Data:** 2026-07-27
- **Contexto:** Todos os endpoints HTTP estavam inline em `Program.cs`
  (~374 linhas), misturando bootstrap, roteamento e validação. As páginas Blazor
  também postam para esses minimal APIs, o que duplica validação (endpoint +
  componente). Precisávamos decidir (a) como organizar os endpoints e (b) se o
  Blazor deve chamar `IProjectService` direto ou continuar via API.
- **Decisão (a):** Extrair os endpoints para módulos por área
  (`Web/Endpoints/AuthEndpoints.cs`, `ProjectEndpoints.cs`) via `MapGroup` +
  extension methods sobre `IEndpointRouteBuilder`, deixando `Program.cs` só com
  bootstrap + pipeline (< 80 linhas). Implementado na spec **R1** sem mudança de
  comportamento observável.
- **Decisão (b) — em aberto:** manter o padrão híbrido atual (Blazor → API) por
  ora. Centralizar validação (ex.: FluentValidation compartilhada) para eliminar
  a duplicação endpoint/componente continua débito, a ser tratado numa spec
  **R4** dedicada. Registrar aqui quando decidido.
- **Consequências:** `Program.cs` enxuto e testável; menor risco de regressão; a
  IA gera código mais focado. A duplicação de validação permanece até R4.
- **Alternativas consideradas:** Migrar para Controllers MVC (mais cerimônia que
  o necessário para o MVP); manter tudo inline (não escala, dificulta review).
