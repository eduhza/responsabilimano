# Plano de Sprints — ResponsabiliMano

Este documento organiza o desenvolvimento em sprints curtas. Cada sprint possui um spec com critérios de aceite claros, seguindo Spec Driven Development (SDD).

## Convenções

*   Todo código deve ser gerado a partir de um spec aprovado.
*   Antes de iniciar uma sprint, a IA deve reler o PRD (`docs/prd.md`) e a Arquitetura (`docs/architecture.md`).
*   Ao final de cada sprint, o spec deve ser marcado como concluído e uma breve nota de status adicionada.

---

## Sprint 0 — Setup do Repositório e Infraestrutura

**Objetivo:** Ter um repositório configurado com .NET 10, Blazor, PostgreSQL, Docker e deploy inicial no GCP.

### Spec S0.1 — Estrutura da Solução

*   Criar solução `ResponsabiliMano.sln`.
*   Criar projetos:
    *   `ResponsabiliMano.Web` (Blazor + host ASP.NET Core).
    *   `ResponsabiliMano.Core` (domínio e interfaces).
    *   `ResponsabiliMano.Infrastructure` (EF Core, e-mail, persistência).
*   Configurar Dependency Injection básica.
*   Configurar infraestrutura de i18n (arquivos .resx) e tema responsivo (mobile e desktop).
*   Inicializar repositório Git local, criar branches `main` e `develop` e configurar remote apontando para o GitHub.

### Spec S0.2 — Banco de Dados e Migrations

*   Configurar `DbContext` com PostgreSQL (Npgsql).
*   Criar entidades iniciais: `User`, `Project`, `GoalField`, `ProjectInvitation`, `ProjectChangeRequest`, `CheckIn`, `CheckInMetric`.
*   Configurar migrations iniciais.
*   Script de seed para desenvolvimento.

### Spec S0.3 — Containerização e Deploy GCP

*   Criar `Dockerfile` para a aplicação.
*   Criar `docker-compose.yml` local com PostgreSQL.
*   Definir pipeline CI/CD mínima (GitHub Actions): build e testes em PRs/pushes para `develop`; build, teste e deploy no GCP (Cloud Run) apenas no push/merge para `main`.
*   Configurar proteção de branches: `main` e `develop` só aceitam atualizações via pull request.
*   Documentar variáveis de ambiente necessárias.

**Critérios de aceite da Sprint 0:**

*   A aplicação sobe localmente via Docker.
*   Migrations executam sem erros.
*   Deploy no GCP está funcional (mesmo que com uma página placeholder).

---

## Sprint 1 — Autenticação e Gestão de Usuários

**Objetivo:** Permitir cadastro e login de usuários.

### Spec S1.1 — Cadastro de Usuário

*   Tela de cadastro com campos: Nome, E-mail, Senha, Confirmação de Senha.
*   Validações: e-mail único, senha mínima de 8 caracteres, senhas iguais.
*   Hash de senha com BCrypt ou Argon2.
*   Endpoint `POST /api/auth/register`.

### Spec S1.2 — Login

*   Tela de login com E-mail e Senha.
*   Autenticação com cookies ASP.NET Core Identity ou JWT.
*   Endpoint `POST /api/auth/login`.
*   Proteção das rotas internas.
*   **Status:** Concluído — login/logout via cookie testados localmente, rotas internas protegidas.

### Spec S1.3 — Layout Base

*   Criar layout base Blazor Server com navegação responsiva.
*   Tema visual simples.
*   Integrar `IStringLocalizer` e carregar cultura padrão `pt-BR`.
*   **Status:** Concluído — layout responsivo, tema simples e textos carregados em pt-BR.

### Spec S1.4 — Recuperação de Senha

*   Fluxo "Esqueci minha senha" com envio de e-mail via `IEmailService`.
*   Tela para inserir nova senha usando token único com expiração.
*   Endpoints `POST /api/auth/forgot-password` e `POST /api/auth/reset-password`.
*   **Status:** Concluído — fluxo de recuperação testado via e2e, token com expiração de 1h, e-mail enviado via LoggingEmailService (dev).

**Critérios de aceite da Sprint 1:**

*   Usuário consegue se cadastrar.
*   Usuário consegue fazer login e logout.
*   Senhas não são armazenadas em texto plano.
*   Recuperação de senha funciona e envia e-mail.

---

## Sprint 2 — Projetos e Acordos

**Objetivo:** Permitir criação, convite e negociação de projetos.

### Spec S2.1 — Criar Projeto

*   Tela de criação com: Nome do Projeto, Data de Início, Data de Fim, Frequência do Check-in e Campos de Metas configuráveis (`Label`, `DataType`, `Unit`, `MinValue`, `MaxValue`, `TargetValue`).
*   Endpoint `POST /api/projects`.
*   Projeto criado com status `Pendente`.

### Spec S2.2 — Convidar Parceiro

*   Formulário de convite com e-mail do parceiro.
*   Envio de e-mail com link de convite usando MailKit.
*   Endpoint `POST /api/projects/{id}/invite`.

### Spec S2.3 — Aprovar ou Sugerir Alterações

*   Tela para o convidado visualizar projeto pendente e durante a execução.
*   Botões "Aprovar" e "Sugerir Alterações".
*   Propostas (`ProjectChangeRequest`) podem alterar metas, data de fim ou frequência.
*   Ambos os participantes podem visualizar, aprovar ou rejeitar uma proposta.
*   Ao aprovar, o sistema aplica a mudança no projeto.
*   Fluxo de aceite mútuo: status muda para `Em Andamento` após aprovação de metas, data de fim e frequência.

**Critérios de aceite da Sprint 2:**

*   Criador consegue criar projeto e convidar.
*   Convidado consegue aprovar ou sugerir alterações.
*   O status muda corretamente para `Em Andamento`.
*   E-mail de convite é enviado.

---

## Sprint 3 — Check-in e Notificações

**Objetivo:** Implementar coleta de dados semanais via cronjob.

### Spec S3.1 — Modelo de Dados de Check-in

*   Entidade `CheckIn` com `ProjectId`, `UserId`, `SubmittedAt`, `PeriodNumber` e `Feeling`.
*   Entidade `CheckInMetric` ligada a `GoalField` armazenando `Value`.
*   Migrations.

### Spec S3.2 — Tela de Check-in

*   Tela acessível por link mágico (com ou sem login).
*   Formulário gerado dinamicamente a partir dos `GoalField` do projeto (respeitando tipo e limites).
*   Seletor de sentimento com 5 rostos (SVG).
*   Endpoint `POST /api/projects/{id}/checkins`.

### Spec S3.3 — Cronjob de Envio de E-mail

*   Job executado conforme `Project.Frequency` (padrão: sexta-feira, 08h).
*   Envia e-mail para ambos os participantes com link para check-in.
*   No GCP, usar Cloud Scheduler acionando endpoint HTTP protegido por secret.

### Spec S3.4 — Lembretes de Check-in Não Respondido

*   Job secundário verifica, após prazo configurado, quais check-ins ainda não foram preenchidos.
*   Envia lembrete por e-mail para os participantes pendentes.

**Critérios de aceite da Sprint 3:**

*   Cronjob envia e-mail na `Project.Frequency`.
*   Usuário consegue preencher check-in com campos gerados pelas metas.
*   Lembretes são enviados para check-ins não respondidos.
*   Dados são persistidos e vinculados ao projeto.

---

## Sprint 4 — Dashboard e Comparação

**Objetivo:** Visualizar evolução da dupla.

### Spec S4.1 — Página do Dashboard

*   Rota `/projects/{id}/dashboard`.
*   Layout com gráfico e cards.

### Spec S4.2 — Gráfico de Evolução de Peso

*   Gráfico de linha comparando peso do Usuário A e Usuário B ao longo do tempo.
*   Biblioteca com licença MIT/Apache.

### Spec S4.3 — Indicadores de Adesão e Sentimento

*   Cards com a média do período dos valores de `CheckInMetric` (adesão, comprometimento, etc.).
*   Card com o sentimento mais recente de cada usuário (5 rostos: média ou último).

**Critérios de aceite da Sprint 4:**

*   Dashboard exibe gráfico comparativo de peso.
*   Cards mostram adesão e sentimento de ambos.
*   Dados refletem os check-ins registrados.

---

## Sprint 5 — Polimento, Testes e Lançamento

**Objetivo:** Preparar o MVP para uso real.

### Spec S5.1 — Testes

*   Testes unitários para regras de negócio.
*   Testes de integração para endpoints principais.

### Spec S5.2 — UX, Mensagens e i18n

*   Revisar textos de e-mail e telas.
*   Garantir que todos os textos visíveis estejam em `.resx` para `pt-BR` e estrutura pronta para novos idiomas.
*   Adicionar feedback visual (loading, erros, sucessos).

### Spec S5.3 — Produção

*   Configurar domínio/SSL.
*   Documentar manual de deploy.
*   Criar script de backup do banco.

---

## Status

*   [x] Sprint 0
    *   [x] S0.1 — Estrutura da Solução
    *   [x] S0.2 — Banco de Dados e Migrations
    *   [x] S0.3 — Containerização e Deploy GCP
*   [x] Sprint 1
    *   [x] S1.1 — Cadastro de Usuário
    *   [x] S1.2 — Login
    *   [x] S1.3 — Layout Base
*   [ ] Sprint 2
    *   [x] S2.1 — Criar Projeto
*   [ ] Sprint 3
*   [ ] Sprint 4
*   [ ] Sprint 5
