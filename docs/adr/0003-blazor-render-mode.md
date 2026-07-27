# 0003 — Render mode do Blazor (Interactive Server)

- **Status:** proposed
- **Data:** 2026-07-27
- **Contexto:** `Program.cs` registra componentes interativos de servidor
  (`AddInteractiveServerComponents` / `AddInteractiveServerRenderMode`), mas
  nenhuma página ou `Routes` aplica `@rendermode InteractiveServer`. Como
  resultado, as páginas com `EditForm` (Register, CreateProject, InvitePartner,
  ForgotPassword, ResetPassword) rodam como SSR estático e não fazem bind — o
  submit vai vazio. Login/Logout funcionam por serem forms HTML puros.
- **Decisão (a decidir na spec X1):** Proposta — aplicar render mode interativo
  de forma explícita. Escolher entre: (i) global no `<Routes>`/`App`, ou (ii)
  por página, apenas onde há interatividade. Recomenda-se começar por página nas
  telas com `EditForm`, preservando o Login/Logout estáticos e a auth por cookie.
- **Consequências:** Formulários passam a funcionar no navegador. Interactive
  Server abre um circuito SignalR — atenção a estado por conexão e à
  statelessness (persistir o que precisa em banco).
- **Alternativas consideradas:** Converter os forms para POST HTML puro (como
  Login) — funciona sem circuito, mas perde a experiência interativa; adotar
  render mode Auto/WebAssembly — fora de escopo do MVP (PRD define Blazor Server).
