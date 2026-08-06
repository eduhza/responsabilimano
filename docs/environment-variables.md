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

## GCP / Produção (substituições do Cloud Build)

O deploy roda no Google Cloud Build (`cloudbuild.yaml`), não no GitHub Actions.
As substituições abaixo têm default no próprio arquivo e podem ser sobrescritas
por build.

| Substituição | Descrição | Default |
|--------------|-----------|---------|
| `_REGION` | Região do Cloud Run / Artifact Registry | `us-central1` |
| `_REPOSITORY` | Repositório do Artifact Registry (Docker) | `containers` |
| `_SERVICE` | Nome do serviço no Cloud Run | `responsabilimano-web` |
| `_SQL_INSTANCE` | Instância do Cloud SQL | `responsabilimano-db` |
| `_TAG` | Tag da imagem (o trigger passa o commit SHA) | `latest` |

`PROJECT_ID` é uma substituição nativa do Cloud Build (`responsabilimano`).

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

## Internacionalização (i18n)

A aplicação usa `IStringLocalizer<AppStrings>` com arquivos `.resx` em
`src/ResponsabiliMano.Web/`. O arquivo `AppStrings.resx` é o fallback (en) e
`AppStrings.pt-BR.resx` é a cultura padrão (pt-BR).

### Como adicionar um novo idioma

1. **Criar o arquivo `.resx`**: copie `AppStrings.resx` para
   `AppStrings.<culture>.resx` (ex: `AppStrings.es.resx` para espanhol) e
   traduza os `<value>` de cada `<data>`. As chaves (`name=`) devem ser
   idênticas em todos os arquivos.

2. **Registrar a cultura**: em `Program.cs`, adicione a cultura à lista de
   culturas suportadas em `RequestLocalizationOptions`:

   ```csharp
   var supportedCultures = new[]
   {
       new CultureInfo("pt-BR"),
       new CultureInfo("en"),
       new CultureInfo("es") // novo idioma
   };
   ```

3. **Definir cultura padrão** (opcional): se o novo idioma deve ser o padrão,
   ajuste `DefaultRequestCulture` no mesmo bloco.

4. **Sincronizar chaves**: garanta que `AppStrings.resx` (fallback en) e o novo
   arquivo tenham exatamente as mesmas chaves (`data name=`). O CI verifica isso
   indiretamente — chaves faltantes resultam em texto não traduzido em runtime.

5. **Testar**: rode `dotnet build` e `dotnet test` para garantir que nada quebra.
   Testes E2E usam seletores por texto em pt-BR; se mudar a cultura padrão,
   atualize os seletores.
