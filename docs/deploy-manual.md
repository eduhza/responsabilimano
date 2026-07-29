# Manual de Deploy — ResponsabiliMano

Este documento descreve o deploy da aplicação ResponsabiliMano no Google Cloud
Platform (Cloud Run + Cloud SQL). Cobre deploy automatizado (CI/CD), deploy
manual (recuperação de desastre), rollback, verificação pós-deploy, configuração
de domínio/SSL, feature flags e backup do banco.

## Pré-requisitos

- **gcloud CLI** instalado e autenticado (`gcloud auth login`).
- Acesso ao projeto GCP `responsabilimano` (permissão de Cloud Run Admin, Cloud
  SQL Admin e Artifact Registry Writer no mínimo).
- **Docker** instalado (para build manual da imagem).
- Acesso ao repositório GitHub `eduhza/responsabilimano`.

## Recursos no GCP

| Recurso | Nome / Identificador |
|---------|----------------------|
| Projeto GCP | `responsabilimano` |
| Região | `us-central1` |
| Cloud SQL (PostgreSQL 16) | Instância `responsabilimano-db`, database `responsabilimano`, usuário `appuser` |
| Instance connection name | `responsabilimano:us-central1:responsabilimano-db` |
| Cloud Run (serviço) | `responsabilimano-web` |
| Artifact Registry | `us-central1-docker.pkg.dev/responsabilimano/containers` |
| Secret Manager | `connection-string`, `cron-secret`, `email-smtp-password` |
| Service account (runtime) | `responsabilimano-run@responsabilimano.iam.gserviceaccount.com` |
| Service account (deploy CI) | `github-deployer@responsabilimano.iam.gserviceaccount.com` |

> Para variáveis de ambiente e secrets completos, consulte
> [`docs/environment-variables.md`](environment-variables.md).

---

## Deploy automatizado (CI/CD)

O pipeline de CI/CD está definido em `.github/workflows/ci-cd.yml` e é acionado
em push/PR para `main` e `develop`.

### O que acontece no merge para `main`

1. **Build & Test** — `dotnet build` + `dotnet test` com cobertura.
2. **CodeQL (SAST)** — análise estática de código C#.
3. **Spec Conformance & Contract Test** — lint do OpenAPI e conformance de
   rotas.
4. **E2E Tests** — testes end-to-end com Playwright.
5. **Deploy para Cloud Run** — todos os jobs acima devem passar (`needs`).

### Secrets do GitHub Actions

O deploy usa Workload Identity Federation (WIF) para autenticar no GCP sem
chaves de longa duração. Os seguintes secrets estão configurados no repositório
GitHub:

| Secret | Descrição |
|--------|-----------|
| `GCP_WORKLOAD_IDENTITY_PROVIDER` | Provider OIDC para WIF |
| `GCP_SERVICE_ACCOUNT` | Service account de deploy (`github-deployer@...`) |
| `GCP_PROJECT_ID` | `responsabilimano` |
| `GCP_REGION` | `us-central1` |
| `GCP_REPOSITORY` | `containers` (Artifact Registry) |
| `GCP_SERVICE_NAME` | `responsabilimano-web` |

### Comando executado pelo CI

O workflow faz:

1. `docker build` da imagem com tag `:<git-sha>`.
2. `docker push` para o Artifact Registry.
3. `gcloud run deploy` com:
   - `--image` apontando para a imagem recém-built.
   - `--region us-central1 --platform managed --allow-unauthenticated`.
   - `--service-account responsabilimano-run@responsabilimano.iam.gserviceaccount.com`.
   - `--add-cloudsql-instances responsabilimano:us-central1:responsabilimano-db`.
   - `--set-env-vars ASPNETCORE_ENVIRONMENT=Production,FeatureManagement__CheckIns=true`.
   - `--set-secrets` mapeando `ConnectionStrings__DefaultConnection`,
     `Cron__Secret` e `EmailSettings__SmtpPassword` do Secret Manager.

> O deploy só ocorre em push/merge para `main`. A branch `develop` roda build e
> testes mas não publica em produção.

---

## Deploy manual (passo a passo)

Use este procedimento para recuperação de desastre ou quando o CI/CD estiver
indisponível.

### 1. Autenticar no GCP

```bash
gcloud auth login
gcloud config set project responsabilimano
gcloud auth configure-docker us-central1-docker.pkg.dev
```

### 2. Build da imagem Docker

```bash
git clone https://github.com/eduhza/responsabilimano.git
cd responsabilimano
git checkout <commit-ou-tag>

docker build \
  -t us-central1-docker.pkg.dev/responsabilimano/containers/responsabilimano-web:manual-$(date +%Y%m%d-%H%M%S) \
  .
```

### 3. Push para o Artifact Registry

```bash
docker push us-central1-docker.pkg.dev/responsabilimano/containers/responsabilimano-web:manual-<timestamp>
```

### 4. Deploy para Cloud Run

```bash
gcloud run deploy responsabilimano-web \
  --image us-central1-docker.pkg.dev/responsabilimano/containers/responsabilimano-web:manual-<timestamp> \
  --region us-central1 \
  --platform managed \
  --allow-unauthenticated \
  --service-account responsabilimano-run@responsabilimano.iam.gserviceaccount.com \
  --add-cloudsql-instances responsabilimano:us-central1:responsabilimano-db \
  --set-env-vars ASPNETCORE_ENVIRONMENT=Production,FeatureManagement__CheckIns=true,FeatureManagement__Dashboard=true \
  --set-secrets ConnectionStrings__DefaultConnection=connection-string:latest,Cron__Secret=cron-secret:latest,EmailSettings__SmtpPassword=email-smtp-password:latest
```

> **Importante:** Substitua `<timestamp>` pelo valor usado nos passos 2 e 3.
> Para a lista completa de variáveis de ambiente e secrets, consulte
> [`docs/environment-variables.md`](environment-variables.md).

---

## Rollback

Para reverter para uma revisão anterior do Cloud Run:

### Opção 1: Re-deployar uma imagem anterior

```bash
# Listar revisões
gcloud run revisions list --service responsabilimano-web --region us-central1

# Encontrar o SHA da imagem da revisão anterior
gcloud run revisions describe <REVISION_NAME> --region us-central1 --format='value(status.image)'

# Re-deployar com a imagem antiga
gcloud run deploy responsabilimano-web \
  --image <IMAGE_URL_DA_REVISAO_ANTERIOR> \
  --region us-central1 \
  --platform managed \
  --allow-unauthenticated \
  --service-account responsabilimano-run@responsabilimano.iam.gserviceaccount.com \
  --add-cloudsql-instances responsabilimano:us-central1:responsabilimano-db \
  --set-env-vars ASPNETCORE_ENVIRONMENT=Production,FeatureManagement__CheckIns=true,FeatureManagement__Dashboard=true \
  --set-secrets ConnectionStrings__DefaultConnection=connection-string:latest,Cron__Secret=cron-secret:latest,EmailSettings__SmtpPassword=email-smtp-password:latest
```

### Opção 2: Rolar para a revisão anterior

```bash
gcloud run services update-traffic responsabilimano-web \
  --to-revisions <REVISION_NAME>=100 \
  --region us-central1
```

---

## Verificação pós-deploy

Após qualquer deploy, verifique:

1. **Health checks:**
   ```bash
   SERVICE_URL=$(gcloud run services describe responsabilimano-web --region us-central1 --format='value(status.url)')
   curl "${SERVICE_URL}/health"        # liveness — deve retornar 200
   curl "${SERVICE_URL}/health/ready"  # readiness — deve retornar 200 (DB acessível)
   ```

2. **Smoke test manual** no navegador:
   - Acessar a URL do serviço.
   - Registrar um novo usuário.
   - Fazer login.
   - Criar um projeto.
   - (Opcional) Convidar parceiro e fazer check-in.

---

## Domínio customizado e SSL

Para mapear um domínio próprio ao serviço Cloud Run:

### 1. Criar o domain mapping

```bash
gcloud run domain-mappings create \
  --service responsabilimano-web \
  --domain seu-dominio.com.br \
  --region us-central1
```

### 2. Configurar DNS

Após criar o mapping, o Google retornará os registros DNS necessários
(geralmente um CNAME ou A record). Configure no seu provedor de DNS:

- **CNAME:** apontar `seu-dominio.com.br` para `responsabilimano-web-<hash>.a.run.app`
- **Ou A record:** apontar para os IPs fornecidos pelo `gcloud run domain-mappings describe`.

### 3. SSL

O SSL é provisionado automaticamente pelo **Google Managed Certificates**. Não
é necessário configurar certificados manualmente. A propagação pode levar alguns
minutos após o DNS estar correto.

### 4. Verificar

```bash
gcloud run domain-mappings list --service responsabilimano-web --region us-central1
```

> **Nota:** O domínio real e os registros DNS são configurados pelo PM fora da
> aplicação. Este documento descreve apenas o processo técnico.

---

## Feature flags de produção

A aplicação usa feature flags (Microsoft.FeatureManagement) para separar deploy
de release. As flags são controladas via env-vars no Cloud Run.

| Flag (env-var) | Descrição | Estado em produção |
|----------------|-----------|--------------------|
| `FeatureManagement__CheckIns` | Check-in capture e lembretes (Sprint 3) | `true` (liberada em 2026-07-28) |
| `FeatureManagement__Dashboard` | Dashboard de evolução (Sprint 4) | `true` |

### Como ligar/desligar uma flag

```bash
# Ligar o Dashboard
gcloud run services update responsabilimano-web \
  --region us-central1 \
  --set-env-vars ASPNETCORE_ENVIRONMENT=Production,FeatureManagement__CheckIns=true,FeatureManagement__Dashboard=true

# Desligar o Dashboard (rollback de feature)
gcloud run services update responsabilimano-web \
  --region us-central1 \
  --set-env-vars ASPNETCORE_ENVIRONMENT=Production,FeatureManagement__CheckIns=true,FeatureManagement__Dashboard=false
```

> **Atenção:** `--set-env-vars` substitui **todas** as env-vars. Sempre inclua
> todas as variáveis necessárias, não apenas a que está alterando. Consulte
> [`docs/environment-variables.md`](environment-variables.md) para a lista
> completa.

---

## Backup do banco

### Script automatizado

O script `scripts/backup-cloudsql.sh` exporta o banco Cloud SQL para um arquivo
`.sql` e faz upload para o bucket GCS `gs://responsabilimano-backups/`.

**Pré-requisitos:**
- Bucket GCS provisionado: `gsutil mb -l us-central1 gs://responsabilimano-backups/`
- O bucket **não deve ser público** — restringir acesso via IAM.
- `gcloud` CLI autenticado.

**Execução manual:**
```bash
chmod +x scripts/backup-cloudsql.sh
./scripts/backup-cloudsql.sh
```

O script:
1. Exporta o banco via `gcloud sql export sql` para `gs://responsabilimano-backups/responsabilimano-YYYYMMDD-HHMMSS.sql`.
2. Verifica que o arquivo foi criado no bucket.
3. Remove backups com mais de 30 dias (opcional).

### Agendamento via Cloud Scheduler

Para executar o backup diariamente (ex: 02:00 BRT):

1. Criar um job no Cloud Scheduler que aciona um Cloud Run Job ou Cloud Function
   que execute o script:

```bash
gcloud scheduler jobs create http backup-daily \
  --schedule="0 2 * * *" \
  --time-zone="America/Sao_Paulo" \
  --uri="https://<cloud-run-job-url>/run" \
  --http-method=POST \
  --oidc-service-account-email=responsabilimano-run@responsabilimano.iam.gserviceaccount.com
```

### Alternativa: lifecycle do bucket GCS

Em vez de limpar backups no script, configurar lifecycle do bucket para
auto-expirar objetos com mais de 30 dias:

```json
// lifecycle.json
{
  "lifecycle": {
    "rule": [
      {
        "action": { "type": "Delete" },
        "condition": { "age": 30 }
      }
    ]
  }
}
```

```bash
gsutil lifecycle set lifecycle.json gs://responsabilimano-backups/
```

### Segurança

- O script **não contém credenciais** — usa a autenticação do `gcloud` CLI.
- O bucket GCS de backups deve ter **acesso restrito** (não público).
- O manual de deploy **não inclui valores de secrets** — apenas nomes e
  referências ao Secret Manager.
