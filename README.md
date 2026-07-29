# ResponsabiliMano

ResponsabiliMano é uma aplicação web para duplas (accountability partners) definirem metas comuns, realizarem check-ins periódicos e acompanharem a evolução por meio de um dashboard comparativo.

## Como rodar localmente

```bash
# 1. Copiar o arquivo de exemplo e configurar a senha do Postgres
cp .env.example .env
# Editar .env e definir POSTGRES_PASSWORD

# 2. Subir a aplicação + banco
docker-compose up --build

# 3. Acessar http://localhost:8080
```

## Documentação

- [Product Requirements Document (PRD)](docs/prd.md)
- [Plano de Sprints e Specs](docs/plan.md)
- [Arquitetura e Decisões Técnicas](docs/architecture.md)
- [Manual de Deploy (GCP Cloud Run)](docs/deploy-manual.md)
- [Variáveis de Ambiente e Secrets](docs/environment-variables.md)

## Como usar este repositório com IA

1. Sempre inicie uma sprint revisando o spec correspondente em `docs/plan.md`.
2. Use o workflow `.devin/workflows/brainstorm.md` para novas ideias e refino de requisitos.
3. Use o workflow `.devin/workflows/implement-spec.md` para implementar specs.
4. Consulte `.devin/rules/core.md` e `.devin/skills/sdd.md` antes de gerar código.

## Status

**MVP em produção** — hospedado no GCP Cloud Run (us-central1) com Cloud SQL
PostgreSQL. Deploy automatizado via GitHub Actions no merge para `main`.

Funcionalidades ativas:
- Cadastro, login e recuperação de senha
- Criação de projetos, convite de parceiro e aceite de alterações
- Check-ins periódicos com lembretes por e-mail (flag `CheckIns` ativa)
- Dashboard de evolução com gráfico comparativo (flag `Dashboard` ativa)
