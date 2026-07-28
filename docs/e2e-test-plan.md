# Plano de Ação — Testes E2E via Browser (Playwright)

Este documento define como executar testes end-to-end (E2E) no navegador para validar, ponto a ponto, as specs já aplicadas em `specs/`.

## 1. Objetivo

- Cobrir todos os critérios de aceitação das specs `R1`, `R9`, `X1`, `S3.1`–`S3.5` através de fluxos reais no browser.
- Garantir que `Register`, `Login`, `CreateProject`, `InvitePartner`, `ProjectDetail`, `CheckIn`, `InvitationAccept`, `ForgotPassword` e `ResetPassword` funcionem como um usuário real usaria.
- Validar integração com backend (`/api/*`), banco, cookies de autenticação, feature flags e envio de e-mail.

## 2. Ferramenta escolhida

- **Microsoft.Playwright + xUnit** para C# (alinhado ao stack .NET 10 do projeto).
- Navegador padrão: **Chromium**.
- Os testes devem rodar em modo headless no CI e podem rodar headed localmente para debug.

## 3. Infraestrutura mínima para E2E

### 3.1 Novo projeto de testes

Criar `tests/ResponsabiliMano.Web.E2ETests/ResponsabiliMano.Web.E2ETests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="Microsoft.Playwright" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\ResponsabiliMano.Web\ResponsabiliMano.Web.csproj" />
  </ItemGroup>
</Project>
```

Adicionar no `Directory.Packages.props`:

```xml
<PackageVersion Include="Microsoft.Playwright" Version="1.48.0" />
```

Instalar browsers:

```bash
pwsh -Command "playwright install --with-deps chromium"
```

### 3.2 Padrão de fixture

Criar `PlaywrightFixture` (`tests/ResponsabiliMano.Web.E2ETests/PlaywrightFixture.cs`):

```csharp
public class PlaywrightFixture : IAsyncLifetime
{
    public IPlaywright Playwright { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new()
        {
            Headless = Environment.GetEnvironmentVariable("CI") != null,
            Args = ["--ignore-certificate-errors"]
        });
    }

    public async Task DisposeAsync()
    {
        await Browser.DisposeAsync();
        Playwright.Dispose();
    }
}

[CollectionDefinition("Browser")]
public class BrowserCollection : ICollectionFixture<PlaywrightFixture>
{
}
```

### 3.3 Aplicação de teste (WebApplicationFactory)

Usar `WebApplicationFactory<Program>` para subir o app em porta dinâmica:

```csharp
public class ResponsabiliManoApp : WebApplicationFactory<Program>, IAsyncLifetime
{
    private IHost? _host;
    public string BaseUrl => $"https://localhost:{_port}";
    private int _port;

    protected override IHost CreateHost(IHostBuilder builder)
    {
        _port = GetFreePort();
        builder.ConfigureWebHost(web =>
        {
            web.UseKestrel();
            web.UseUrls(BaseUrl);
        });
        _host = base.CreateHost(builder);
        return _host;
    }

    public async Task InitializeAsync()
    {
        await _host!.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        if (_host is not null)
            await _host.StopAsync();
    }

    private static int GetFreePort()
    {
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
```

### 3.4 Banco de dados isolado

Para evitar flakiness:

- Usar **SQLite em memória** (`DataSource=:memory:`) ou um **PostgreSQL de teste** por execução.
- Sobrescrever `ConnectionStrings:DefaultConnection` na configuração da factory.
- Garantir `EnsureCreated()` / `Migrate()` no startup do teste e `Dispose()` no fim.

Exemplo:

```csharp
builder.ConfigureAppConfiguration((_, config) =>
{
    config.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ConnectionStrings:DefaultConnection"] = "DataSource=:memory:",
        ["FeatureManagement:CheckIns"] = "true",
        ["Cron:Secret"] = "test-cron-secret",
        ["EmailSettings:SmtpHost"] = "127.0.0.1",
        ["EmailSettings:SmtpPort"] = "1025",
        ["EmailSettings:SmtpUser"] = "test@test.com",
        ["EmailSettings:FromName"] = "Test",
        ["EmailSettings:FromEmail"] = "test@test.com"
    });
});
```

### 3.5 Servidor de e-mail fake

- Usar **MailHog** ou **SMTP4DEV** (`smtp4dev`) via Testcontainers na porta `1025`.
- Lê o último e-mail capturado via API REST do MailHog (`http://localhost:8025/api/v2/messages`).
- Para `S3.5`, validar que o link de `forgot-password` e check-in usa `BaseUrl` correto (não `localhost:8080`).

## 4. Cenários E2E por spec

### 4.1 R1 — Extrair endpoints para módulos

- **Cenário R1.1**: Realizar `Register` → `Login` → `CreateProject` e verificar que as rotas `/api/auth/*` e `/api/projects/*` respondem `200`/`201`.
- **Cenário R1.2**: Usar a aba Network do Playwright para capturar as chamadas XHR/Fetch e garantir que nenhuma chamada interna usa `MapPost`/`MapGet` inline no `Program.cs` — o app continua funcional.
- **Cenário R1.3**: Logout funciona e redireciona para `/login`.

### 4.2 R9 — Baseline OpenAPI

- **Cenário R9.1**: Para cada fluxo (auth + projects + checkins), inspecionar as chamadas de rede e validar:
  - Rotas documentadas existem (`/api/auth/register`, `/api/auth/login`, etc.).
  - Status de erro (`400`, `401`, `409`) são observáveis na UI (mensagens de erro).
- **Cenário R9.2**: Chamar `/api/projects/{id}/checkins` com token inválido deve retornar `401`.

### 4.3 X1 — Render mode interativo

- **Cenário X1.1**: Preencher o formulário de `Register`, submeter e verificar redirecionamento para `/`.
- **Cenário X1.2**: `CreateProject` com um objetivo preenchido cria projeto e exibe página de detalhe.
- **Cenário X1.3**: `ForgotPassword` e `ResetPassword` funcionam via `EditForm` interativo.
- **Cenário X1.4**: `Login` e `Logout` continuam funcionando com form HTML puro.
- **Cenário X1.5**: Verificar ausência de `#blazor-error-ui` após cada submit.

### 4.4 S3.1 — Modelo de dados de check-in

- Coberto indiretamente via S3.2. Não precisa de teste browser específico além do fluxo de check-in.

### 4.5 S3.2 — Tela de check-in

- **Cenário S3.2.1**: Criar projeto ativo → acessar `/projects/{id}/checkin` → formulário exibe metas do projeto (`Label`, `Unit`, `Min`/`Max`).
- **Cenário S3.2.2**: Selecionar um sentimento (um dos 5 rostos SVG) e preencher valores válidos → submeter → mensagem de sucesso.
- **Cenário S3.2.3**: Tentar submeter valor fora do limite (`MaxValue` ou `MinValue`) → mensagem de erro clara.
- **Cenário S3.2.4**: Submeter check-in duas vezes no mesmo período → mensagem "A check-in for this period has already been submitted." (ou chave localizada).
- **Cenário S3.2.5**: Usuário que não é participante tenta acessar `/projects/{id}/checkin` → deve receber erro/projeto não encontrado.

### 4.6 S3.3 — Cronjob de envio de check-in

- **Cenário S3.3.1**: Criar projeto ativo → chamar `POST /api/cron/checkins/dispatch` com header `X-Cron-Secret` correto → status `200` e `sent > 0`.
- **Cenário S3.3.2**: Verificar e-mail capturado no MailHog contém link para `/projects/{id}/checkin` com `BaseUrl` correto.
- **Cenário S3.3.3**: Chamar o endpoint duas vezes no mesmo período → `sent == 0` (idempotência).
- **Cenário S3.3.4**: Chamar sem o secret ou com secret errado → `401`.

### 4.7 S3.4 — Lembretes de check-in não respondido

- **Cenário S3.4.1**: Criar projeto ativo, disparar `dispatch` (S3.3) → não preencher check-in → chamar `POST /api/cron/checkins/reminders` → `sent > 0` para participantes pendentes.
- **Cenário S3.4.2**: Após preencher check-in → chamar `reminders` → `sent == 0` para aquele participante.
- **Cenário S3.4.3**: Projeto `Pending` ou com `EndDate` passado não gera lembrete.

### 4.8 S3.5 — Envio real de e-mail (SMTP) + base URL nos links

- **Cenário S3.5.1**: Fluxo `ForgotPassword` → e-mail capturado no MailHog contém link de reset com `BaseUrl` da aplicação (ex.: `https://localhost:{port}/reset-password?token=...`), **nunca** `https://localhost:8080`.
- **Cenário S3.5.2**: E-mail de convite (`InvitePartner`) contém link com `BaseUrl` correto.
- **Cenário S3.5.3**: Quando `EmailSettings:SmtpPassword` não está configurado, o app usa `LoggingEmailService` (verificar nos logs).
- **Cenário S3.5.4**: Com senha SMTP configurada, e-mail é entregue via `SmtpEmailService` (verificar no MailHog).

## 5. Dados e ordem de execução

### 5.1 Suite de happy path (ordem linear)

1. Registrar `alice@test.com`.
2. Login.
3. Criar projeto `Projeto Alfa` com duas metas.
4. Convidar `bob@test.com`.
5. Aceitar convite como `bob`.
6. Aprovar projeto.
7. Preencher check-in como `alice`.
8. Preencher check-in como `bob`.
9. Disparar cron de dispatch e validar e-mails.
10. Disparar cron de reminders e validar que nenhum e-mail sai (todos preencheram).

### 5.2 Suite de erros/edge cases

- Registro com e-mail inválido / senha curta / confirmação diferente.
- Login com credenciais inválidas.
- Criar projeto sem metas ou com `EndDate <= StartDate`.
- Check-in fora de limite.
- Check-in duplicado.
- Acesso não autorizado a projeto de outro usuário.
- Cron sem `X-Cron-Secret`.

## 6. Passos para execução local

1. **Instalar dependências**:
   ```bash
   dotnet restore
   pwsh -Command "playwright install --with-deps chromium"
   ```

2. **Subir MailHog** (Docker):
   ```bash
   docker run -d -p 1025:1025 -p 8025:8025 --name mailhog mailhog/mailhog
   ```

3. **Executar os testes E2E**:
   ```bash
   dotnet test tests/ResponsabiliMano.Web.E2ETests --logger "console;verbosity=detailed"
   ```

4. **Debug visual**:
   ```bash
   $env:CI="false"
   dotnet test tests/ResponsabiliMano.Web.E2ETests --filter "FullyQualifiedName~CheckInFlow"
   ```

## 7. Integração com CI

Adicionar job ao `.github/workflows/ci-cd.yml`:

```yaml
e2e-tests:
  needs: build-and-test
  runs-on: ubuntu-latest
  steps:
    - uses: actions/checkout@v4
    - uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '10.0.x'
    - name: Install Playwright
      run: pwsh -Command "playwright install --with-deps chromium"
    - name: Start MailHog
      run: docker run -d -p 1025:1025 -p 8025:8025 mailhog/mailhog
    - name: Run E2E tests
      run: |
        dotnet build --configuration Release
        dotnet test tests/ResponsabiliMano.Web.E2ETests --no-build --configuration Release --logger trx
    - name: Upload E2E results
      uses: actions/upload-artifact@v4
      if: always()
      with:
        name: e2e-test-results
        path: tests/ResponsabiliMano.Web.E2ETests/TestResults/*.trx
```

## 8. Critérios de aceitação do próprio plano

- [ ] Projeto `ResponsabiliMano.Web.E2ETests` criado e compilando.
- [ ] Todos os cenários da seção 4 implementados.
- [ ] Testes passam localmente contra SQLite in-memory + MailHog.
- [ ] `#blazor-error-ui` nunca aparece em nenhum cenário.
- [ ] CI executa `e2e-tests` após o build.
- [ ] Screenshots automáticos em falha configurados (`screenshots/` gerados no CI artifact).

## 9. Riscos e mitigações

| Risco | Mitigação |
|-------|-----------|
| SignalR saturado com muitos testes paralelos | Usar `[Collection("Browser")]` e limitar `MaxParallelThreads` |
| E-mail assíncrono pode não estar disponível imediatamente | Poll na API do MailHog com timeout (ex.: 5s) |
| Blazor circuit desconectado durante teste headless | Aumentar `SlowMo` no debug; garantir `WaitForSelectorAsync` em vez de `Task.Delay` |
| `BaseUrl` dinâmico precisa ser propagado para e-mails | Usar `WebApplicationFactory` com `UseUrls` e injetar a URL no assertion |
| Banco SQLite in-memory fecha entre conexões | Usar `DataSource=:memory:` com `Mode=Memory` e manter conexão aberta |

## 10. Próximos passos imediatos

1. Criar o projeto de testes e adicionar `Microsoft.Playwright`.
2. Implementar `PlaywrightFixture`, `ResponsabiliManoApp` e helpers de e-mail.
3. Escrever os testes na ordem da seção 5.1 (happy path).
4. Adicionar testes de erro da seção 5.2.
5. Configurar job no CI e fazer merge na `develop`.
