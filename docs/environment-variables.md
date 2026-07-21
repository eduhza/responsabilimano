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

## GCP / Produção

| Variável | Descrição |
|----------|-----------|
| `GCP_PROJECT_ID` | ID do projeto no Google Cloud |
| `GCP_REGION` | Região do Cloud Run (ex: `us-central1`) |
| `GCP_SERVICE_NAME` | Nome do serviço no Cloud Run |

## Segurança

| Variável | Descrição |
|----------|-----------|
| `CRON_API_KEY` | Chave para proteger endpoints de cronjob (S3.x) |
| `ALLOWED_ORIGINS` | Origens permitidas para CORS, separadas por vírgula |
