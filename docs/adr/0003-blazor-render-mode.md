# 0003 — Render mode do Blazor (Interactive Server)

- **Status:** accepted
- **Data:** 2026-07-28
- **Contexto:** `Program.cs` registra componentes interativos de servidor
  (`AddInteractiveServerComponents` / `AddInteractiveServerRenderMode`), mas
  nenhuma página ou `Routes` aplica `@rendermode InteractiveServer`. Como
  resultado, as páginas com `EditForm` (Register, CreateProject, InvitePartner,
  ForgotPassword, ResetPassword) rodam como SSR estático e não fazem bind — o
  submit vai vazio. Login/Logout funcionam por serem forms HTML puros.
- **Decisão (spec X1):** Aplicar `@rendermode InteractiveServer` **por página**,
  apenas nas telas com interatividade C# (`EditForm`, `@onclick`, `@bind`) — e **não**
  globalmente no `<Routes>`/`App`. Páginas cobertas (7): `Register`, `CreateProject`,
  `InvitePartner`, `ForgotPassword`, `ResetPassword` (as com `EditForm` que a spec
  lista) **mais** `ProjectDetail` e `InvitationAccept`, que sofrem da mesma causa-raiz
  (botões de aprovar/propor/aceitar via `@onclick`/`@bind` que também não funcionavam).
  `CheckIn` já recebera o render mode na S3.2. Login/Logout permanecem
  `<form method="post">` estáticos — o cookie de auth não pode ser setado sobre o
  circuito SignalR — e as páginas de exibição (`Home`, `NavMenu`) seguem SSR estático.
- **Consequências:** Formulários passam a funcionar no navegador. Interactive
  Server abre um circuito SignalR — atenção a estado por conexão e à
  statelessness (persistir o que precisa em banco).
- **Alternativas consideradas:** Converter os forms para POST HTML puro (como
  Login) — funciona sem circuito, mas perde a experiência interativa; adotar
  render mode Auto/WebAssembly — fora de escopo do MVP (PRD define Blazor Server).

- **Adendo (spec RT1, 2026-07-29):** `Home` passou a `@rendermode InteractiveServer`.
  Motivo: como página estática, a Home não re-consultava ao navegar — depois de
  aceitar um convite (que seta `Project.PartnerId`), o projeto só aparecia após
  reload manual (Bug A). A Home apenas **lê** o estado de autenticação (via
  `CascadingAuthenticationState`); nunca seta o cookie — logo a restrição acima
  (cookie sobre circuito) não se aplica a ela. Login/Register **continuam
  estáticos**. Consequência: a Home agora abre um circuito e re-executa
  `OnInitializedAsync` a cada navegação, sempre com dado fresco (a leitura usa
  `AsNoTracking`, evitando o identity map do `DbContext` de vida longa do circuito).
  Isso também habilita a atualização ao vivo por polling (spec RT2).
