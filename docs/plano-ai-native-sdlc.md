# Plano de Ação — ResponsabiliMano rumo ao AI-Native SDLC

**Versão:** 1.0
**Data:** 2026-07-23
**Autor / PM / Pod (solo):** Eduardo Arruda
**Referência de framework:** Vault "Projeto Dell — AI-Native SDLC"
**Objetivo deste documento:** Estruturar o ResponsabiliMano para operar sob o AI-Native SDLC da Dell, configurando **Rules, Skills, Workflows, Memories e MCP Servers** de forma que o loop de desenvolvimento (Devin AI) rode de forma autônoma e o humano atue **apenas nos gates de decisão**.

> Este é um **plano de ação** — não altera o repositório. Ele diagnostica, adapta o modelo para um time de uma pessoa e entrega um roadmap priorizado com templates prontos nos apêndices.

---

## 1. Sumário Executivo

O ResponsabiliMano já nasceu com um esqueleto de SDD (`.devin/` + `docs/prd.md`, `plan.md`, `architecture.md`) e um pipeline CI/CD para GCP Cloud Run. Isso é uma base **boa** — está alinhado ao Tenet 8 ("construa sobre o que já tem"). Porém, para atingir o loop autônomo do AI-Native SDLC, faltam quatro coisas essenciais:

1. **Specs no formato certo.** Hoje os "specs" são seções de `plan.md` (sprint-based, 2 semanas). O modelo exige **specs atômicas, legíveis por máquina, uma por iteração (2–3 dias)**, cada uma em seu próprio arquivo com critérios testáveis, modelo de dados, contrato e restrições de segurança.
2. **Portões automatizados de qualidade.** O CI roda `dotnet test` mas **não existe projeto de testes** — o gate passa a vazio. Faltam **spec conformance** e **contract testing**, as duas etapas que o AI-Native adiciona ao pipeline.
3. **Gates humanos explícitos.** Num time solo, o risco é colapsar tudo num "vou codando". O ganho do modelo vem justamente de **separar produção (IA) de curadoria (você)** e só interromper você em 3 momentos: aprovar spec, aprovar MR, aceitar feature.
4. **Guilds como automação.** Você não tem uma Quality Guild nem uma Architecture Guild humanas. A adaptação solo é **codificar cada Guild como uma rule + um check de CI + um checklist de skill**, para que a governança rode sozinha.

Com esses quatro pontos resolvidos, mais os refactors de engenharia já identificados (extrair endpoints do `Program.cs`, criar o projeto de testes, remover arquivos `Class1.cs`, iniciar log de ADRs, introduzir feature flags), o projeto passa a executar o loop de 5 passos de forma autônoma.

---

## 2. Diagnóstico — Estado Atual vs. AI-Native

### 2.1 O que já existe (e mapeia bem)

| Ativo atual | Onde | Status AI-Native |
|---|---|---|
| Esqueleto SDD | `.devin/rules/core.md`, `.devin/skills/sdd.md` | Bom começo, precisa expandir |
| Workflows | `.devin/workflows/{brainstorm, implement-spec, finalize-spec.ps1}` | Mapeiam parcialmente o loop; faltam gates e validação |
| PRD / Plano / Arquitetura | `docs/prd.md`, `docs/plan.md`, `docs/architecture.md` | Fonte da verdade parcial; specs no formato errado |
| CI/CD | `.github/workflows/ci-cd.yml` | Build/test/deploy ok; **faltam gates novos** |
| GitFlow | `main` / `develop` + feature branches + PRs | Alinhado; PRs já são o mecanismo de MR |
| Deploy atrás de infra | GCP Cloud Run | Ok; **falta feature flag** (deploy ≠ release) |
| Camadas | `Core` / `Infrastructure` / `Web` | Arquitetura limpa-ish; boa para statelessness |

### 2.2 Gaps contra os 9 Princípios (Tenets)

| Tenet | Situação hoje | Gap |
|---|---|---|
| **T1 · Specs são a fonte da verdade** | Specs vivem como seções de `plan.md`, em prosa e por sprint | Migrar para arquivos `specs/*.md` atômicos, legíveis por máquina, com frontmatter |
| **T2 · IA produz, humano cura** | Devin já gera, mas sem gates formais | Definir os 3 gates humanos e o que cada um verifica |
| **T3 · Uma spec, uma iteração, uma entrega** | Iterações são sprints de 2 semanas | Decompor Sprints 3–5 em specs de 2–3 dias |
| **T4 · Pods enxutos e auto-geridos** | Você é o Pod inteiro (solo) | Explicitar o acúmulo de papéis e os gates (seção 3) |
| **T5 · Qualidade automatizada, não guarnecida** | `dotnet test` roda, mas **não há testes** | Criar `ResponsabiliMano.Tests` + thresholds + spec conformance |
| **T6 · Contratos acima de coordenação** | Sem contratos formais (OpenAPI/AsyncAPI) | Extrair OpenAPI dos endpoints atuais (baseline brownfield) |
| **T7 · Governança escala com o esforço** | Governança fixa (sempre "sprint") | Ativar níveis Feature/Program só quando necessário |
| **T8 · Construa sobre o que já temos** | ✅ Respeitado | Manter; estender o CI em vez de substituir |
| **T9 · Repos de Pod exclusivos** | Solo, monorepo único | OK; adotar `CODEOWNERS` + specs como interface de mudança |

### 2.3 Gaps de engenharia e código (achados na varredura)

| # | Achado | Arquivo/Evidência | Severidade |
|---|---|---|---|
| G1 | **Todos os endpoints inline no `Program.cs`** (374 linhas, misturando bootstrap + 9 endpoints + validação) | `src/ResponsabiliMano.Web/Program.cs` | Alta |
| G2 | **Não existe projeto de testes**, embora `architecture.md` cite `ResponsabiliMano.Tests` e o CI rode `dotnet test` | `.github/workflows/ci-cd.yml` roda test; `src/` sem `.Tests` | Alta |
| G3 | **Arquivos de template `Class1.cs`** deixados no domínio e na infra | `Core/Class1.cs`, `Infrastructure/Class1.cs` | Baixa |
| G4 | **Validação duplicada** (nos endpoints minimal API e presumivelmente nos componentes Blazor) | `Program.cs` + `Components/Pages/*.razor` | Média |
| G5 | **Padrão híbrido** Blazor Server → POST em minimal API por form; decisão arquitetural não registrada | `Program.cs` + páginas | Média |
| G6 | **`DisableAntiforgery()` em vários endpoints** de estado | `Program.cs` (login, projects, change-requests) | Média (segurança) |
| G7 | **`plan.md` desatualizado**: S1.4 e Sprint 2 concluídos mas sem check; docs divergem do código | `docs/plan.md` §Status | Média |
| G8 | **Sem log de ADRs** (decisões estão embutidas em `architecture.md`) | `docs/` | Média |
| G9 | **Sem feature flags** — deploy = release, contraria o passo 5 do loop | `Program.cs`, CI/CD | Média |
| G10 | **Sem observabilidade / health checks / readiness** (preocupação de SRE) | `Program.cs` | Média |
| G11 | **Sprints 3–5 não implementadas** (check-in/cronjob, dashboard, polimento) | `docs/plan.md` | — (backlog) |

---

## 3. Adaptação do Modelo para um Time Solo

Você acumula **todos** os papéis humanos. O erro a evitar é deixar isso virar "um modo só". O valor do AI-Native vem de **trocar de chapéu de forma deliberada** e, principalmente, de **codificar em automação os papéis que não são pessoas** (as Guilds, o review bot, os gates de CI).

### 3.1 Os papéis humanos, colapsados em "chapéus" e gates

| Papel Dell | Seu chapéu | Quando você o veste |
|---|---|---|
| **Business Lead** | Estratégia de produto | Ao definir o Program/roadmap e o "porquê" (uma vez por bloco de trabalho) |
| **IT Lead** | Arquitetura | Ao escrever ADRs, escolher stack, definir NFRs |
| **Feature Lead** | Coordenação | Ao decompor uma feature em specs e definir contratos |
| **Pod Lead** | Aprovador de spec/MR | **Gate 1** (aprovar spec) e **Gate 2** (aprovar MR) |
| **AI-Augmented Dev** | Curador do código | Guiar o Devin e revisar criticamente o output |

> **Regra prática solo:** não misture "escrever spec" com "revisar código gerado". São chapéus diferentes, em momentos diferentes. A spec fica **pronta e aprovada antes** de o Devin gerar qualquer linha.

### 3.2 As Guilds viram automação (o coração da autonomia solo)

Como não há Guilds humanas, cada preocupação de Guild é encapsulada em **rule + check de CI + checklist**:

| Guild Dell | Vira, no seu projeto | Onde |
|---|---|---|
| **Quality** | Thresholds de cobertura + spec conformance no CI + projeto de testes | `ci-cd.yml`, `.Tests` |
| **Architecture** | Rule "toda decisão estrutural vira ADR" + revisão de contrato | `.devin/rules/architecture.md`, `docs/adr/` |
| **Data Governance** | Check "sem PII em logs" + validação de input | rule + CI (SAST) |
| **Tech Ops** | Templates de CI + Dockerfile + Secret Manager (já existe) | `.github/`, `Dockerfile` |
| **SRE** | Health checks + observabilidade + rollback trigger | `Program.cs`, `deploy-checklist` |
| **UX / Design** | i18n `.resx` + design tokens + checklist de UX copy | `AppStrings.resx`, rule |
| **Security (transversal)** | SAST/DAST/SCA no CI + antiforgery + JWT/cookie corretos | `ci-cd.yml`, `Program.cs` |

### 3.3 Arquitetura do loop autônomo — onde o Devin roda sozinho e onde você entra

```text
         VOCÊ (Gate 1)            DEVIN AUTÔNOMO                    VOCÊ (Gate 2)         AUTO
   ┌───────────────────┐   ┌──────────────────────────────┐   ┌──────────────────┐   ┌────────┐
   │ 1. Aprova a SPEC  │──▶│ 2. Gera código + testes       │──▶│ 4. Revisa o MR:  │──▶│ 5.     │
   │ (clara, testável, │   │ 3. Abre PR + CI:              │   │  testes reais?   │   │ Merge  │
   │  contrato, sec.)  │   │  build→test→SAST→spec-conf→   │   │  edge cases?     │   │ Deploy │
   │                   │   │  contract-test→review-bot     │   │  aprova          │   │ atrás  │
   └───────────────────┘   └──────────────────────────────┘   └──────────────────┘   │ flag   │
            ▲                                                                          └───┬────┘
            │                        VOCÊ (Gate 3, só em feature multi-spec)               │
            │              ┌──────────────────────────────────────────┐                    │
            └──────────────│ 10. E2E ok → liga a feature flag (release)│◀───────────────────┘
                           └──────────────────────────────────────────┘
```

**Você só é interrompido em 3 pontos:**

- **Gate 1 — Aprovar a spec** (antes de gerar). Se a spec está vaga, o código sai errado. É o ponto de maior alavancagem.
- **Gate 2 — Aprovar o MR** (depois do CI verde). O bot faz a 1ª passada; você é o portão final: os testes cobrem casos de borda reais? A segurança da spec virou código? Bate com a spec?
- **Gate 3 — Aceitar a feature / ligar a flag** (só quando várias specs compõem uma feature). Valida o E2E antes de liberar ao usuário.

Tudo **entre** os gates roda sozinho: geração, PR, CI (com os gates novos), auto-remediação de lint/segurança e comentário do review bot.

---

## 4. Plano por Eixo

### 4.1 Rules (`.devin/rules/`)

`core.md` é bom mas genérico. Dividir em rules focadas, cada uma encapsulando uma "Guild":

| Rule (novo/editar) | Propósito | Origem no framework |
|---|---|---|
| `core.md` (editar) | Contexto do projeto + modos Agent/Editor + **referência aos 3 gates** | Base atual |
| `spec-driven.md` (novo) | O que é uma spec válida; proibir geração sem spec aprovada (Gate 1) | T1, "O que é uma Spec" |
| `quality-gates.md` (novo) | Cobertura significativa (não só verde), CI 100% verde, spec conformance | Quality Guild, T5 |
| `architecture.md` (novo) | Toda decisão estrutural → ADR; padrões .NET; statelessness; camadas | Architecture Guild |
| `security.md` (novo) | Antiforgery, JWT/cookie, validação de input, **sem PII em log**, OWASP | Data Gov + Security |
| `contracts.md` (novo) | Integrações precisam de contrato (OpenAPI/AsyncAPI) antes de codar | T6, "Contratos" |
| `dotnet-conventions.md` (novo) | C# 13/.NET 10, nullable, async, EF Core, naming; usar skill `dotnet-best-practices` | Seu dia a dia |

> **Ganho de autonomia:** com `spec-driven.md` e `quality-gates.md`, o Devin passa a **recusar** trabalhar fora de uma spec aprovada e a **não pedir merge** sem cobertura real — reduzindo idas e vindas até você.

### 4.2 Skills (`.devin/skills/`)

`sdd.md` já existe. Adicionar skills que codificam os "checklists de Guild":

| Skill (novo/editar) | O que ensina o agente a fazer |
|---|---|
| `sdd.md` (editar) | Alinhar ao template de spec do Apêndice A; ligar spec ↔ item de tracking |
| `write-spec.md` (novo) | Transformar uma ideia/épico numa spec atômica testável (Gate 1 self-check) |
| `generate-tests.md` (novo) | Gerar testes a partir dos critérios de aceite; cobrir edge cases, não só happy-path |
| `extract-openapi.md` (novo) | Extrair contrato OpenAPI de endpoints existentes (baseline brownfield) |
| `review-mr.md` (novo) | Checklist do Gate 2: spec conformance, segurança, cobertura significativa |
| `write-adr.md` (novo) | Registrar decisões arquiteturais no formato ADR (usar skill `engineering:architecture`) |

### 4.3 Workflows (`.devin/workflows/`)

Mapear explicitamente aos 5 passos do loop e aos 12 passos ponta a ponta. Editar os 3 existentes e adicionar:

| Workflow | Passos do loop | Ação |
|---|---|---|
| `brainstorm.md` (editar) | Passos 0–2 (visão → features) | Já bom; adicionar saída "lista de specs candidatas" |
| `write-spec.md` (novo) | Passo 4 (decompor → spec) | Produz um arquivo `specs/*.md` pronto para o Gate 1 |
| `implement-spec.md` (editar) | Passos 2–3 (gerar + PR/CI) | **Bloquear se a spec não estiver aprovada**; parar no Gate 2 |
| `review-and-merge.md` (novo) | Passos 4–5 (review → deploy) | Checklist Gate 2 → merge → deploy atrás de flag |
| `finalize-spec.ps1` (manter) | Passo 3/5 (PR + push) | Funciona; considerar migrar para MCP do GitHub (§4.5) |
| `accept-feature.md` (novo) | Passo 10 (E2E → flip flag) | Só para features multi-spec; Gate 3 |

### 4.4 Memories

Persistir os fatos que o agente precisa reter entre iterações (evita re-explicar). Candidatos:

- **Projeto:** "MSS pessoal — accountability app; solo dev; todos os papéis são o Eduardo; gates 1/2/3 definidos neste plano."
- **Convenções:** ".NET 10 + Blazor Server + EF Core + PostgreSQL; DB em lowercase/snake_case; docs em pt-BR; commits em inglês imperativo."
- **Arquitetura:** "Camadas Core/Infra/Web; alvo stateless; endpoints sendo migrados de Program.cs para módulos (ver ADR-000x)."
- **Estado do roadmap:** "Sprints 0–2 concluídas; próximas specs = check-in/cronjob (Sprint 3)."
- **Contratos:** "Baseline OpenAPI extraído em `contracts/`; toda nova integração precisa de contrato."

> No Devin, isso vive como Knowledge/Memories; neste plano estão como fatos a cadastrar. Regra: **um fato por memory**, curto e verificável.

### 4.5 MCP Servers

Para o loop rodar autônomo, o agente precisa de mãos em Git e no tracker. Prioridades:

| MCP | Por quê | Observação |
|---|---|---|
| **GitHub** | Abrir/aprovar PRs, ler CI, gerenciar branches — substitui a chamada REST crua do `finalize-spec.ps1` | Requer autorização OAuth do conector |
| **Work Tracker** (GitHub Issues / Jira / Linear / Notion) | Status e rastreabilidade (spec no Git ↔ item no tracker) | Solo → **GitHub Issues + Projects** é o mais simples e mantém tudo no Git |
| **Filesystem / repo** | Ler specs, código e contratos | Já disponível localmente |
| **(Opcional) Figma** | Design tokens → UI (dashboard da Sprint 4) | Só quando chegar no front visual |

> **Regra de ouro do vault:** o que a IA consome ou o CI valida → **Git**; o que é status/coordenação → **tracker**. Para solo, GitHub Issues como tracker mantém atrito mínimo.
>
> **Atenção:** os conectores (GitHub, Jira, Linear, Notion, Slack, etc.) aparecem como disponíveis mas exigem **autorização OAuth** — que não pode ser feita nesta sessão. Autorize-os nas configurações de conectores do Claude.ai (ou via `/mcp` numa sessão interativa) antes de esperar que o loop os use.

### 4.6 Estrutura de repositório, Specs e Contratos

Adotar o modelo do vault adaptado a um monorepo solo (cenário brownfield):

```text
ResponsabiliMano/
├── specs/                      # NOVO — specs atômicas (fonte da verdade)
│   ├── S3.1-checkin-data-model.md
│   └── ...
├── contracts/                  # NOVO — OpenAPI (REST) e AsyncAPI (eventos/cron)
│   ├── auth-api.yaml
│   └── projects-api.yaml
├── docs/
│   ├── adr/                    # NOVO — Architecture Decision Records
│   │   └── 0001-endpoints-organization.md
│   ├── prd.md  plan.md  architecture.md
├── .devin/                     # rules, skills, workflows (§4.1–4.3)
├── src/  ...  tests/           # tests/ = NOVO projeto de testes
└── .github/workflows/ci-cd.yml
```

Ações:

- **Migrar specs**: cada spec de `plan.md` (S3.1, S3.2, …) vira um arquivo em `specs/` no template do Apêndice A. `plan.md` passa a ser o índice/roadmap que **linka** as specs.
- **Baseline de contratos (brownfield)**: extrair OpenAPI dos endpoints atuais de `Program.cs` para `contracts/` — vira a baseline dos contract tests. (Vault: "Sem contratos existentes → comece extraindo as interfaces atuais para specs OpenAPI".)
- **`CODEOWNERS`**: mesmo solo, formaliza o Tenet 9 e prepara para futuros colaboradores.

### 4.7 CI/CD — adicionar os gates que faltam

O `ci-cd.yml` atual faz `build → test → deploy(main)`. Estender para a sequência do AI-Native:

```text
Build → Test → SAST/DAST/SCA → Spec Conformance → Contract Test → Deploy(flag)
```

| Etapa | Como implementar |
|---|---|
| **Test (de verdade)** | Criar `ResponsabiliMano.Tests` (xUnit) + falhar o build se cobertura < limite mínimo |
| **SAST/DAST/SCA** | GitHub CodeQL (SAST) + `dotnet list package --vulnerable` (SCA); DAST leve pós-deploy |
| **Spec Conformance** | Job que verifica que o PR referencia uma spec aprovada e que os critérios viraram testes |
| **Contract Test** | Validar implementação contra `contracts/*.yaml` (ex.: Schemathesis/Pact) |
| **Feature Flag** | Introduzir flags (Microsoft.FeatureManagement) para separar deploy de release |
| **Branch protection** | Confirmar que `main`/`develop` só aceitam via PR com CI verde (Sprint 0 previa isso) |

### 4.8 Refactors de código e engenharia

Priorizados; cada um deve virar uma **spec** e passar pelo loop (dogfooding do próprio processo):

| Ref | Ação | Resolve |
|---|---|---|
| R1 | **Extrair endpoints do `Program.cs`** para módulos (`AuthEndpoints`, `ProjectEndpoints`) via `MapGroup` + extension methods; `Program.cs` só orquestra | G1, G5 |
| R2 | **Criar `ResponsabiliMano.Tests`** (xUnit) com testes de domínio (`ProjectService`, `PasswordResetService`) e integração dos endpoints | G2 |
| R3 | **Remover `Class1.cs`** de `Core` e `Infrastructure` | G3 |
| R4 | **Registrar ADR** sobre o padrão de endpoints (API vs. chamar `IProjectService` direto no Blazor) e resolver a duplicação de validação (ex.: FluentValidation compartilhado) | G4, G8 |
| R5 | **Revisar `DisableAntiforgery()`** — manter só onde há justificativa real (webhooks/cron); documentar | G6 |
| R6 | **Reconciliar `plan.md`** com o estado real e migrar para `specs/` | G7 |
| R7 | **Introduzir feature flags** (Microsoft.FeatureManagement) | G9 |
| R8 | **Adicionar health/readiness checks** + logging estruturado (Serilog) | G10 |

### 4.9 PRD e documentação

- **PRD (`prd.md`)**: está bom e detalhado. Evoluir para v0.3 alinhando a linguagem "épico/feature/história" ao vocabulário **Program → Feature → Spec** e adicionando NFRs mensuráveis (latência, cobertura) que virem critérios de aceite.
- **`plan.md`**: reduzir a "roadmap índice" que aponta para `specs/`. Manter os critérios de aceite por sprint como **Feature briefs**.
- **`architecture.md`**: corrigir a referência ao `ResponsabiliMano.Tests` (ainda não existe) e mover as decisões numeradas (§6) para ADRs individuais em `docs/adr/`.

---

## 5. Roadmap Priorizado

Cada fase entrega valor e é dogfooding: as próprias mudanças passam pelo loop (spec → gerar → validar → gate → merge).

### Fase P0 — Fundação do loop autônomo (habilita tudo)

**Meta:** o loop roda sozinho e só te chama nos gates.

1. Criar `ResponsabiliMano.Tests` + primeiros testes (R2) — desbloqueia o gate de qualidade.
2. Estender `.devin/rules/` (`spec-driven`, `quality-gates`, `security`) e editar `sdd.md` (§4.1–4.2).
3. Estender `ci-cd.yml` com cobertura mínima + SAST (CodeQL) + confirmação de branch protection (§4.7).
4. Autorizar o **MCP do GitHub** e adotar **GitHub Issues** como tracker (§4.5).
5. Criar `specs/` e migrar as specs restantes de `plan.md` para o template (§4.6 + Apêndice A).

> **Gate humano nesta fase:** você aprova as rules e o template de spec. Depois disso, o loop está "armado".

### Fase P1 — Higiene de engenharia (dogfooding)

**Meta:** provar o loop consertando o que já existe.

6. R1 — extrair endpoints do `Program.cs` (primeira spec real a rodar no loop novo).
7. R3, R5, R6 — limpar `Class1.cs`, revisar antiforgery, reconciliar docs.
8. R4 + R7 + R8 — ADR de endpoints, feature flags, health checks/observabilidade.
9. Extrair baseline **OpenAPI** dos endpoints (§4.6) + primeiro contract test no CI.

### Fase P2 — Retomar o produto no novo processo

**Meta:** entregar as Sprints 3–5 como specs atômicas.

10. Decompor **Sprint 3** (check-in + cronjob) em specs de 2–3 dias e rodar o loop.
11. **Sprint 4** (dashboard) — aqui entra design tokens / Figma (MCP opcional).
12. **Sprint 5** (polimento, i18n, produção) — deploy-checklist + backup + SSL.

### Onde você (humano) atua em cada iteração

| Momento | Frequência | Chapéu |
|---|---|---|
| Aprovar spec (Gate 1) | 1×/spec, ~15–30 min | Pod Lead |
| Aprovar MR (Gate 2) | 1×/spec, ~30–60 min | AI-Aug Dev |
| Aceitar feature / flip flag (Gate 3) | 1×/feature | Feature Lead |
| Definir roadmap/ADR | Esporádico | Business/IT Lead |

Todo o resto — geração, PR, CI, review bot, auto-remediação — é autônomo.

---

## 6. KPIs Adaptados ao Solo Dev

Os 3 KPIs do vault, recalibrados para uma pessoa (metas iniciais mais folgadas, apertando com o tempo):

| KPI | Definição | Meta solo (inicial) |
|---|---|---|
| **Cycle Time** | Spec "em progresso" → produção | ≤ 3–4 dias/spec |
| **AI First-Pass Acceptance** | % de PRs do Devin que passam no CI sem você editar código | ≥ 70% (subir p/ 80%) |
| **Feature Lead Time** | Feature criada → aceita (E2E em prod) | ≤ 2 semanas |

> Se o **First-Pass** ficar baixo, o problema quase sempre é **spec ambígua** — invista no Gate 1, não em revisar mais código.

---

## 7. Riscos (adaptação solo)

| Risco | Mitigação |
|---|---|
| **Colapsar os chapéis** ("só vou codar") e perder os ganhos | Ritual explícito: spec aprovada **antes** de gerar; nunca revisar código sem checklist do Gate 2 |
| **Specs viram burocracia** | Template enxuto (Apêndice A). Se o Devin não gera bem a partir dela, a spec está errada — conserte a spec |
| **Testes "verdes vazios"** | `quality-gates.md` exige cobertura significativa; você valida edge cases no Gate 2 |
| **Sem contratos, integrações quebram** (cron, ERP futuro) | Contract-first: extrair OpenAPI e validar no CI antes de qualquer consumidor |
| **Dependência da ferramenta (Devin)** | Specs em Markdown/YAML e código no Git são agnósticos — trocar de agente = retreinar, não reescrever |

---

## Apêndice A — Template de Spec (pronto para `specs/`)

```markdown
---
id: S3.1
feature: Sprint 3 — Check-in e Notificações
pod: ResponsabiliMano (solo)
priority: P1
iteration: 1 (2-3 dias)
contract: contracts/checkins-api.yaml
tracking: gh-issue-#NN
status: draft            # draft | approved | in-progress | done
---

# Modelo de Dados de Check-in

## User Value
Como sistema, preciso persistir check-ins periódicos por usuário/projeto
para alimentar lembretes e o dashboard comparativo.

## Acceptance Criteria
1. Entidade `CheckIn` (ProjectId, UserId, SubmittedAt, PeriodNumber, Feeling).
2. Entidade `CheckInMetric` ligada a `GoalField` com `Value` (decimal).
3. Um check-in por usuário por período por projeto (constraint única).
4. Migration EF Core com tabelas/colunas em lowercase/snake_case.
5. Testes: unit de domínio + integração da persistência.

## Data Model
- CheckIn { Id, ProjectId, UserId, Feeling, SubmittedAt, PeriodNumber }
- CheckInMetric { Id, CheckInId, GoalFieldId, Value }

## Security Constraints
- Endpoint autenticado; usuário só grava check-in de projeto do qual participa.
- Sem PII em logs.

## API Contract
Ver contracts/checkins-api.yaml (OpenAPI 3.1).

## Dependencies
- Projeto em status "Em Andamento" (Sprint 2, concluída).

## Out of Scope
- Tela de check-in (spec S3.2).
- Cronjob de envio (spec S3.3).
```

## Apêndice B — Rule sketch: `.devin/rules/quality-gates.md`

```markdown
# Quality Gates (Quality Guild automatizada)

- Nenhum PR é aberto sem testes gerados a partir dos critérios de aceite da spec.
- Cobertura mínima significativa: falhe o CI abaixo do limite acordado.
- "Verde" não basta: cada critério de aceite tem ao menos um teste que exercita
  o caso de borda real, não apenas o happy-path.
- O CI deve estar 100% verde (build, test, SAST/DAST/SCA, spec conformance,
  contract test) antes de solicitar aprovação humana (Gate 2).
- Se um teste gerado é trivial (getter/setter/markup), remova-o.
```

## Apêndice C — Rule sketch: `.devin/rules/spec-driven.md`

```markdown
# Spec-Driven (Tenet 1)

- Não gere código sem uma spec APROVADA em `specs/` (status: approved).
- Se o pedido não tem spec, PARE e proponha uma spec no template do Apêndice A;
  aguarde aprovação humana (Gate 1).
- A spec é a fonte da verdade. Se código e spec divergirem, a spec vence
  (ou atualize a spec explicitamente via novo commit).
- Toda spec liga-se a um item de tracking (GitHub Issue) para status.
```

## Apêndice D — Workflow sketch: `.devin/workflows/review-and-merge.md`

```markdown
---
description: Gate 2 — revisar o PR gerado, aprovar e fazer deploy atrás de flag
---
# Review & Merge (passos 4–5 do loop)

1. Confirme CI 100% verde (build, test, SAST, spec conformance, contract test).
2. Rode o checklist do Gate 2:
   - Os testes cobrem os casos de borda reais da spec?
   - As restrições de segurança da spec viraram código?
   - O código bate com a spec (spec conformance)?
   - Segue os padrões (.devin/rules/dotnet-conventions.md)?
3. Se algo falhar, devolva ao passo de geração com contexto específico.
4. Aprovado → merge → deploy automático ATRÁS de feature flag (desligada).
5. Valide em produção; só então ligue a flag (ou deixe para o Gate 3 da feature).
```

## Apêndice E — Mapa loop de 5 passos ↔ seus workflows

| Passo do loop | Quem executa | Seu workflow `.devin` |
|---|---|---|
| 1 · Spec Review | **Você (Gate 1)** | `write-spec.md` → aprovação |
| 2 · IA gera código+testes | Devin | `implement-spec.md` |
| 3 · MR + CI | Devin + CI | `implement-spec.md` + `ci-cd.yml` |
| 4 · Review + Approve | Bot + **Você (Gate 2)** | `review-and-merge.md` |
| 5 · Deploy + Flag | CI | `review-and-merge.md` + `finalize-spec.ps1` |
| 10 · E2E + Accept | **Você (Gate 3)** | `accept-feature.md` |
| 11 · Measure | Monitoring | (pós-MVP: observabilidade) |

---

*Fim do plano. Próximo passo sugerido: aprovar as prioridades da Fase P0 e, a partir daí, transformar cada item de refactor (R1–R8) na primeira leva de specs a rodar pelo loop.*
