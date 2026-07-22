# Variáveis de Ambiente

## Aplicação

| Variável | Descrição | Exemplo local |
|----------|-----------|---------------|
| `ASPNETCORE_ENVIRONMENT` | Ambiente de execução (`Development`, `Staging`, `Production`) | `Development` |
| `ConnectionStrings__DefaultConnection` | Connection string do PostgreSQL | `Host=db;Database=responsabilimano;Username=postgres;Password=postgres` |

## Configuração do E-mail (S1.x)

| Variável | Descrição | Exemplo |
|----------|-----------|---------|
| `Email__SmtpHost` | Host do servidor SMTP | `smtp.example.com` |
| `Email__SmtpPort` | Porta do servidor SMTP | `587` |
| `Email__SmtpUser` | Usuário SMTP | `noreply@example.com` |
| `Email__SmtpPassword` | Senha SMTP | `secret` |
| `Email__FromAddress` | Endereço remetente padrão | `noreply@responsabilimano.app` |
| `Email__FromName` | Nome remetente padrão | `ResponsabiliMano` |

## GCP / Produção (secrets do GitHub Actions)

Usados pelo workflow `.github/workflows/ci-cd.yml` no deploy para Cloud Run via Workload Identity Federation.

| Secret | Descrição | Valor configurado |
|--------|-----------|-------------------|
| `GCP_PROJECT_ID` | ID do projeto no Google Cloud | `responsabilimano` |
| `GCP_REGION` | Região do Cloud Run / Artifact Registry | `us-central1` |
| `GCP_REPOSITORY` | Repositório do Artifact Registry (Docker) | `containers` |
| `GCP_SERVICE_NAME` | Nome do serviço no Cloud Run | `responsabilimano-web` |
| `GCP_SERVICE_ACCOUNT` | Service account de deploy | `github-deployer@responsabilimano.iam.gserviceaccount.com` |
| `GCP_WORKLOAD_IDENTITY_PROVIDER` | Provider WIF (OIDC) | `projects/144768016039/locations/global/workloadIdentityPools/github-pool/providers/github-provider` |

### Recursos provisionados no GCP

| Recurso | Nome |
|---------|------|
| Cloud SQL (PostgreSQL 16) | instância `responsabilimano-db`, database `responsabilimano`, usuário `appuser` |
| Instance connection name | `responsabilimano:us-central1:responsabilimano-db` |
| Secret Manager | `connection-string` (injetado no Cloud Run como `ConnectionStrings__DefaultConnection`) |
| Artifact Registry | `us-central1-docker.pkg.dev/responsabilimano/containers` |

> A connection string de produção usa o socket do Cloud SQL:
> `Host=/cloudsql/responsabilimano:us-central1:responsabilimano-db;Database=responsabilimano;Username=appuser;Password=***`

## Segurança

| Variável | Descrição |
|----------|-----------|
| `CRON_API_KEY` | Chave para proteger endpoints de cronjob (S3.x) |
| `ALLOWED_ORIGINS` | Origens permitidas para CORS, separadas por vírgula |
