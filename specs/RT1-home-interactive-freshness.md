---
id: RT1
feature: Reatividade — atualização automática das telas
pod: ResponsabiliMano (solo)
priority: P1
iteration: 1 (0.5 dia)
contract: none
tracking: gh-issue-#TBD
status: approved
depends_on: []
adr: [0003]
---

# RT1 — Home interativa + dado sempre fresco

## User Value

Como usuário, quero que a Home mostre meus projetos atualizados assim que eu chego nela (depois de me cadastrar, aceitar um convite ou ter um status mudado), sem precisar recarregar a página manualmente, para confiar que o sistema reflete a realidade.

## Contexto do bug (Bug A)

`Home.razor` é SSR estático. Depois de aceitar um convite (que seta `Project.PartnerId`), o projeto só aparece após um reload manual, porque a página estática não re-consulta ao navegar e a enhanced navigation pode reaproveitar o DOM anterior. Além disso, o `AppDbContext` é `Scoped` (vive o circuito inteiro), então re-consultas dentro do mesmo circuito podem devolver instâncias já rastreadas (identity map) em vez do dado fresco do banco.

## Acceptance Criteria

1. `Home.razor` declara `@rendermode InteractiveServer`. A cada navegação para `/`, `OnInitializedAsync` re-executa e recarrega a lista de projetos do usuário.
2. `ProjectService.GetUserProjectsAsync` usa `.AsNoTracking()` — a consulta sempre reflete o estado atual do banco, sem devolver entidades rastreadas obsoletas de um circuito de vida longa.
3. `AuthorizeView`/estado de autenticação continua funcionando na Home interativa (via `CascadingAuthenticationState` já registrado). Nenhum PII é logado.
4. `Login.razor` e `Register.razor` permanecem **estáticos** (SSR, POST HTTP puro) — o cookie de auth não pode ser setado sobre circuito (ADR-0003). Esta spec não altera essas páginas.
5. `RenderModeTests`: `Home` passa da lista `StaticPages` para `InteractivePages`. Os testes de guarda de render mode passam.
6. `docs/adr/0003-blazor-render-mode.md` recebe um adendo documentando que a Home passou a interativa para reatividade (exibição), mantendo Login/Register estáticos.
7. O comportamento visual da Home (markup atual) é preservado — o redesign visual da Home é a spec D4.

## Data Model

Nenhum.

## Security Constraints

- Home interativa apenas **lê** o estado de autenticação; não seta cookie. Login/Register seguem estáticos (ADR-0003).
- Ownership inalterado: `GetUserProjectsAsync` continua filtrando por `CreatorId == userId || PartnerId == userId`.
- Sem PII em log.

## API / Event Contract

none — nenhum endpoint ou contrato alterado.

## Dependencies

- ADR-0003 (render mode por página) — esta spec adiciona um adendo.

## Out of Scope

- Redesign visual da Home (cards, empty state, skeleton) → spec **D4**.
- Atualização ao vivo quando o **parceiro** age em outra sessão → spec **RT2** (polling).
- Converter Login/Register para FluentUI → spec **D4**.

## Verification

- `dotnet build ResponsabiliMano.slnx`
- `dotnet test tests/ResponsabiliMano.Web.Tests` — `RenderModeTests` com Home em `InteractivePages`.
- Manual (1 navegador): cadastrar um usuário convidado → aceitar o convite → navegar para `/` pela navegação → o projeto aparece **sem reload manual**.
