# Product Requirements Document (PRD)

**Nome do Projeto:** ResponsabiliMano  
**Versão:** 0.2 - Decisões do PM incorporadas  
**Objetivo:** Uma aplicação web simples para duplas (accountability partners) definirem metas — por exemplo de saúde/emagrecimento —, realizarem check-ins periódicos e acompanharem a evolução por meio de um dashboard comparativo.

## 1. Visão Geral

O sistema permite que um usuário crie um "Projeto" (ex: "Projeto Verão"), defina metas e convide outro usuário. O projeto só inicia quando ambos concordarem com as regras e metas (fluxo de negociação). Uma rotina em background (cronjob) cobrará relatórios de progresso, que alimentarão um dashboard visual.

## 2. Atores (Usuários)

*   **Criador (Usuário A):** Inicia o projeto, define as metas base e convida o parceiro.
*   **Convidado (Usuário B):** Recebe o convite, pode sugerir alterações nas metas ou aprovar.

## 3. Histórias de Usuário (Épicos e Features)

### Épico 1: Autenticação e Gestão de Usuários

*   [E1-U1] Como usuário, quero me cadastrar com Nome, E-mail e Senha para acessar a plataforma.
*   [E1-U2] Como usuário, quero fazer login na plataforma para usar meus projetos.
*   [E1-U3] Como usuário, quero recuperar o acesso caso esqueça minha senha.

### Épico 2: Gestão de Projetos e Acordos

*   [E2-U1] Como Criador, quero criar um novo projeto definindo: Nome do Projeto, Data de Fim, Frequência do Check-in e Campos de Metas configuráveis (label, tipo, unidade, mínimo, máximo e valor alvo).
*   [E2-U2] Como Criador, quero convidar um parceiro (Usuário B) via e-mail.
*   [E2-U3] Como Convidado, quero visualizar o projeto pendente e ter a opção de "Aprovar" ou "Sugerir Alterações".
*   [E2-U4] Como Criador, se o Convidado sugerir alterações, quero visualizar a sugestão e "Aprovar" para dar início ao projeto.
*   [E2-U5] Como Convidado, quero propor uma nova data de fim antes de aprovar o projeto.
*   [E2-U6] Como participante, quero propor alterações nas metas, data de fim ou frequência durante a execução, mediante aprovação do parceiro.
*   [E2-R1] O status do projeto deve mudar para "Em Andamento" apenas após o aceite mútuo de metas, data de fim e frequência.

### Épico 3: Check-in e Notificações (Cronjob)

*   [E3-U1] Como sistema, quero rodar um cronjob na frequência definida pelo criador do projeto (padrão: toda sexta-feira às 08h) que envia um e-mail para ambos os usuários do projeto em andamento.
*   [E3-U2] O e-mail deve conter um link mágico ou direcionamento para a tela de Check-in.
*   [E3-U3] Como usuário, na tela de Check-in, quero preencher: Peso atual, % de adesão aos treinos na semana, % de adesão à dieta/água, e Sentimento (Dropdown: Motivado, Normal, Desanimado, etc).
*   [E3-U4] Como usuário, quero receber lembretes por e-mail caso eu não preencha o check-in dentro do prazo.
*   [E3-U5] Como sistema, quero enviar e-mails transacionais (convite, cadastro, recuperação de senha, lembretes) usando o mesmo serviço de e-mail.

### Épico 4: Dashboard e Comparação

*   [E4-U1] Como usuário, quero visualizar um Dashboard do projeto em andamento.
*   [E4-U2] O dashboard deve exibir gráficos de linha mostrando a evolução do peso do Usuário A vs Usuário B.
*   [E4-U3] O dashboard deve exibir indicadores (cards) com a taxa de adesão semanal e o "sentimento" de ambos para gerar competitividade e apoio mútuo.
*   [E4-U4] Como usuário, quero ver o histórico de check-ins em uma linha do tempo (desejável).

## 4. Regras de Negócio

1. Um projeto só pode ser criado por um usuário autenticado.
2. O convite é enviado por e-mail. Se o convidado ainda não possuir conta, o convite deve conter um link de cadastro.
3. Enquanto o projeto estiver no status "Pendente" ou "Em Andamento", qualquer participante pode propor alterações nas metas, data de fim ou frequência.
4. O status muda para "Em Andamento" quando o convidado aprovar a versão atual das metas, data de fim e frequência.
5. O cronjob deve processar apenas projetos com status "Em Andamento" e cuja data de fim não tenha sido atingida.
6. Cada usuário pode preencher apenas um check-in por período (definido pela `Frequency` do projeto) em cada projeto.
7. Os gráficos do dashboard consideram apenas check-ins do projeto selecionado.
8. Propostas de alteração ficam pendentes até aprovação do outro parceiro.
9. O sistema envia lembretes por e-mail quando um check-in não é preenchido dentro do prazo definido pela frequência.

## 5. Requisitos Não Funcionais

*   **Frontend:** Blazor Server (ASP.NET Core) para o MVP, responsivo para desktop e mobile via navegador. PWA nativo fica para pós-MVP.
*   **Backend/Banco de Dados:** .NET 10 com PostgreSQL e serviços do Google Cloud Platform (GCP).
*   **E-mails:** Envio transacional via MailKit (SMTP) para convites, cadastro, recuperação de senha, lembretes e check-ins.
*   **Gráficos:** Biblioteca com licença MIT ou Apache (ex: Chart.js via interop JavaScript).
*   **Multi-idioma:** Textos externalizados em arquivos `.resx` no backend e no frontend Blazor; API pode retornar chaves/códigos para tradução.
*   **Segurança:** Senhas hash com salt, HTTPS, validação de e-mail básica, tokens de convite/check-in com expiração.
*   **Deploy:** Containerização com Docker; execução no GCP Cloud Run (free tier) + Cloud SQL PostgreSQL de menor tier; subdomínio do GCP para o MVP.
*   **Custo:** Usar recursos do free tier sempre que possível; manter instâncias no menor tier disponível.

## 6. Fluxos Principais

### Fluxo 1: Criação e Negociação do Projeto

1. Criador acessa "Novo Projeto".
2. Preenche Nome do Projeto, Data de Fim e Campos de Metas.
3. Insere o e-mail do parceiro e envia o convite.
4. Sistema envia e-mail para o convidado com link de visualização/aceite.
5. Convidado visualiza o projeto e escolhe "Aprovar" ou "Sugerir Alterações".
6. Se sugerir alterações, o Criador recebe notificação e decide "Aprovar" (aplica alterações) ou "Negociar" (nova sugestão).
7. Quando ambos aprovam, status muda para "Em Andamento".

### Fluxo 2: Check-in Semanal

1. Cronjob executa na periodicidade configurada (padrão: toda sexta-feira, 08h).
2. Sistema envia e-mail para os dois participantes.
3. Usuário acessa o link do e-mail e preenche o check-in.
4. Sistema armazena os dados e atualiza indicadores.

### Fluxo 3: Acompanhamento no Dashboard

1. Usuário acessa o projeto em andamento.
2. Dashboard exibe gráfico de evolução de peso comparativo.
3. Exibe cards com adesão e sentimento mais recente de cada um.

## 7. Decisões Validadas pelo PM

| # | Decisão |
|---|---------|
| 1 | **Blazor:** usar Blazor Server no MVP. A aplicação será web responsiva, acessível por computador e celular, sem publicação em stores. PWA fica para pós-MVP. |
| 2 | **E-mails transacionais:** sim. Arquitetura deve estar pronta para enviar e-mails de convite, cadastro, recuperação de senha, lembretes e check-ins. |
| 3 | **Metas configuráveis:** cada campo de meta terá label, tipo (decimal/inteiro/percentual), unidade, mínimo, máximo e valor alvo. Ex: `peso (kg)` decimal; `comprometimento` inteiro 0 a 10. |
| 4 | **Alteração de data de fim:** o convidado pode propor data de fim diferente. Durante a execução, qualquer participante pode propor prorrogação, com aprovação do parceiro. |
| 5 | **Frequência do check-in:** o criador define a frequência por projeto. O convidado pode solicitar alteração. |
| 6 | **Lembretes:** sim. Sistema envia lembrete por e-mail quando o check-in não é preenchido no prazo. |
| 7 | **Sentimento:** usar escala de 5 rostos (muito feliz a muito chateado). No dashboard, exibir rosto médio/ícone ou cor conforme contexto. |
| 8 | **Domínio:** manter subdomínio do GCP no MVP. |
| 9 | **Multi-idioma:** sim. Manter textos em `.resx` no backend ou retornar códigos que o frontend traduza; usar melhores práticas .NET 10 + Blazor. |
| 10 | **Custo GCP:** mínimo possível. PostgreSQL simples e recursos que caibam no free tier. |

## 8. Critérios de Aceite Gerais do MVP

*   Usuário consegue se cadastrar e logar.
*   Criador consegue criar projeto e convidar por e-mail.
*   Convidado consegue aprovar ou sugerir alterações.
*   Projeto inicia após aprovação mútua.
*   Cronjob envia e-mail semanal.
*   Usuário consegue preencher check-in.
*   Dashboard exibe gráfico comparativo de peso e cards de adesão/sentimento.
*   Aplicação roda em ambiente GCP com PostgreSQL.

## 9. Fora do Escopo (MVP)

*   Pagamentos, planos premium, notificações push mobile.
*   Múltiplos projetos simultâneos por dupla (foco em um projeto por dupla).
*   Integrações com wearables ou APIs de saúde.
*   Relatórios avançados ou exportação de dados.
