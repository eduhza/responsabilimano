# Variáveis de Ambiente

## Aplicação

| Variável | Descrição | Exemplo local |
|----------|-----------|---------------|
| `ASPNETCORE_ENVIRONMENT` | Ambiente de execução (`Development`, `Staging`, `Production`) | `Development` |
| `ConnectionStrings__DefaultConnection` | Connection string do PostgreSQL | `Host=db;Database=responsabilimano;Username=postgres;Password=postgres` |

## Configuração do E-mail (S3.5)

Seção `EmailSettings`. Sem `SmtpPassword` a aplicação usa o `LoggingEmailService`
(dev local não envia nada); com a senha, usa o `SmtpEmailService` (MailKit).

| Variável | Descrição | Exemplo |
|----------|-----------|---------|
| `EmailSettings__SmtpHost` | Host do servidor SMTP | `smtp.gmail.com` |
| `EmailSettings__SmtpPort` | Porta do servidor SMTP (STARTTLS) | `587` |
| `EmailSettings__SmtpUser` | Usuário SMTP (autenticação) | `naoresponda@bomvoarturismo.com` |
| `EmailSettings__SmtpPassword` | Senha/app password — **só via Secret Manager** | `secret` |
| `EmailSettings__FromName` | Nome do remetente | `Clube BomVoar` |
| `EmailSettings__FromEmail` | Endereço do remetente (= SmtpUser) | `naoresponda@bomvoarturismo.com` |

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
| Secret Manager | `connection-string` → `ConnectionStrings__DefaultConnection`; `cron-secret` → `Cron__Secret`; `email-smtp-password` → `EmailSettings__SmtpPassword` |
| Artifact Registry | `us-central1-docker.pkg.dev/responsabilimano/containers` |

> A connection string de produção usa o socket do Cloud SQL:
> `Host=/cloudsql/responsabilimano:us-central1:responsabilimano-db;Database=responsabilimano;Username=appuser;Password=***`

## Segurança

| Variável | Descrição |
|----------|-----------|
| `Cron__Secret` | Segredo do header `X-Cron-Secret` dos endpoints de cron (S3.3/S3.4, ver `docs/adr/0005`) — **só via Secret Manager** |
| `FeatureManagement__CheckIns` | Liga/desliga a feature de check-in (deploy ≠ release; `true` libera aos usuários) |
| `ALLOWED_ORIGINS` | Origens permitidas para CORS, separadas por vírgula (futuro) |
