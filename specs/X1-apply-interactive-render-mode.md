---
id: X1
feature: Correção de comportamento (Fase P1)
pod: ResponsabiliMano (solo)
priority: P0
iteration: 1 (2-3 dias)
contract: none
tracking: gh-issue-#TBD
status: done
depends_on: []
adr: [0003]
---

# Aplicar render mode interativo nas páginas com EditForm

## Contexto
`Program.cs` registra `AddInteractiveServerComponents()` e
`AddInteractiveServerRenderMode()`, mas **nenhuma página/`Routes` aplica**
`@rendermode InteractiveServer`. Resultado: páginas baseadas em `EditForm`
(Register, CreateProject, InvitePartner, ForgotPassword, ResetPassword) renderizam
como SSR estático — os campos não fazem bind e o submit vai vazio, disparando
validação "obrigatório". Login/Logout funcionam por serem `<form method="post">`
HTML puro. (Achado documentado em `.agents/skills/testing-responsabilimano`.)

## User Value
Como usuário, quero que os formulários da aplicação (cadastro, criar projeto,
convidar, recuperar/redefinir senha) funcionem no navegador.

## Acceptance Criteria
1. As páginas com `EditForm` fazem bind e submetem corretamente no navegador.
2. A decisão de render mode (global vs. por página) é registrada no ADR-0003.
3. Não quebrar o fluxo de Login/Logout (forms HTML) nem a autenticação por cookie.
4. Teste que cubra o comportamento (bUnit para o componente, ou teste E2E do
   fluxo de cadastro) — verde.

## Data Model
- Sem mudança.

## Security Constraints
- Manter antiforgery correto nos forms interativos. Sem PII em logs.

## API / Event Contract
- none.

## Dependencies
- Nenhuma. (Recomendado antes de retomar features de UI da Sprint 3/4.)

## Out of Scope
- Redesenhar as telas; apenas corrigir o render mode e o bind.

## Verification
- Manual: cadastrar um usuário pela UI e criar um projeto ponta a ponta.
- Automático: teste do fluxo de cadastro (bind + submit) passando.

## Resultado (entregue)
- `@rendermode InteractiveServer` aplicado **por página** (decisão registrada no
  ADR-0003, agora `accepted`) em 7 telas: as 5 com `EditForm` do escopo original
  **mais** `ProjectDetail` e `InvitationAccept`, que tinham a mesma causa-raiz
  (`@onclick`/`@bind` sem render mode — aprovar/propor/aceitar não funcionavam).
  `Login`/`Logout` e as telas de exibição (`Home`, `NavMenu`) seguem SSR estático.
- Novo projeto `tests/ResponsabiliMano.Web.Tests` (bUnit): teste de comportamento do
  `Register` (bind + submit chamam o serviço com os valores digitados; validação
  bloqueia o submit) **e** um guard por reflexão que assevera que as páginas
  interativas declaram `InteractiveServerRenderMode` e as estáticas não — protege
  contra remoção futura do `@rendermode` (que o bUnit sozinho não pegaria).
