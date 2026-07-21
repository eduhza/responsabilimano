# Arquitetura e Decisões Técnicas — ResponsabiliMano

## 1. Visão da Arquitetura

Aplicação web em .NET 10 com Blazor Server no frontend (responsivo para desktop e mobile via navegador), PostgreSQL como banco relacional, e serviços do Google Cloud Platform (GCP) para hospedagem, agendamento de tarefas e envio de e-mails.

```text
┌─────────────────────┐
│   Blazor (ASP.NET)  │
│  Server-Side / WASM │
└─────────┬───────────┘
          │
┌─────────▼───────────┐
│  .NET 10 API + Core │
│  Clean-ish / Layered  │
└─────────┬───────────┘
          │
┌─────────▼───────────┐
│  PostgreSQL (Cloud  │
│     SQL no GCP)     │
└─────────────────────┘
          │
┌─────────▼───────────┐
│  MailKit (SMTP)     │
│  Cloud Scheduler    │
└─────────────────────┘
```

## 2. Stack Tecnológico

| Camada | Tecnologia | Motivo |
|---|---|---|
| Frontend | Blazor Server (.NET 10) | Unificar linguagem com backend; deploy simples; responsivo para desktop e mobile. |
| Backend | ASP.NET Core 10 | Plataforma principal, alta performance e madura. |
| Banco de dados | PostgreSQL 16+ | Banco relacional robusto, bom suporte no GCP. |
| ORM | Entity Framework Core 10 | Produtividade com .NET. |
| E-mail | MailKit (SMTP) | Envio direto via código sem serviços adicionais pagos. |
| Gráficos | Chart.js (MIT) | Popular, leve, fácil integração com Blazor via JS interop. |
| Container | Docker | Portabilidade entre ambientes. |
| Nuvem | Google Cloud Platform | Requisito do projeto. |
| CI/CD | GitHub Actions | Build, testes e deploy automatizados. |

## 3. Estrutura de Projetos .NET

```
ResponsabiliMano/
├── src/
│   ├── ResponsabiliMano.Web/          # Blazor + host ASP.NET Core
│   ├── ResponsabiliMano.Core/         # Entidades, interfaces, regras de domínio
│   ├── ResponsabiliMano.Infrastructure/ # EF Core, repositórios, e-mail, jobs
│   └── ResponsabiliMano.Tests/        # Testes unitários e de integração
├── docs/
├── .devin/
├── Dockerfile
├── docker-compose.yml
└── README.md
```

## 4. Serviços do GCP

*   **Cloud Run:** Hospedar a aplicação containerizada.
*   **Cloud SQL (PostgreSQL):** Banco de dados gerenciado.
*   **Cloud Scheduler:** Disparar o cronjob de envio de e-mails periodicamente.
*   **Secret Manager:** Armazenar senhas de e-mail, connection strings e chaves de API.
*   **Cloud Build / Cloud Deploy (opcional):** Alternativa ao GitHub Actions.

## 5. Modelo de Dados Inicial

### Enum `Feeling`

```csharp
public enum Feeling
{
    VerySad = 1,
    Sad = 2,
    Neutral = 3,
    Happy = 4,
    VeryHappy = 5
}
```

### Enum `ProjectFrequency`

```csharp
public enum ProjectFrequency
{
    Daily,
    Weekly,
    Biweekly,
    Monthly
}
```

### Enum `GoalDataType`

```csharp
public enum GoalDataType
{
    Decimal,
    Integer,
    Percent
}
```

### Entidade `User`

```csharp
public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string PreferredLanguage { get; set; } = "pt-BR";
    public DateTime CreatedAt { get; set; }
}
```

### Entidade `Project`

```csharp
public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public Guid CreatorId { get; set; }
    public Guid? PartnerId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public ProjectFrequency Frequency { get; set; }
    public ProjectStatus Status { get; set; } // Pending, Active, Finished, Cancelled
    public List<GoalField> Goals { get; set; } = new();
}
```

### Entidade `GoalField`

```csharp
public class GoalField
{
    public Guid Id { get; set; }
    public string Label { get; set; } = null!; // ex: "Peso (kg)"
    public GoalDataType DataType { get; set; } // Decimal, Integer, Percent
    public string Unit { get; set; } = null!; // ex: "kg"
    public decimal? MinValue { get; set; } // ex: 0
    public decimal? MaxValue { get; set; } // ex: 10
    public decimal? TargetValue { get; set; } // meta desejada
}
```

### Entidade `CheckIn`

```csharp
public class CheckIn
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public Feeling Feeling { get; set; }
    public DateTime SubmittedAt { get; set; }
    public int PeriodNumber { get; set; } // semana/quinzena/mês sequencial
    public List<CheckInMetric> Metrics { get; set; } = new();
}
```

### Entidade `CheckInMetric`

```csharp
public class CheckInMetric
{
    public Guid Id { get; set; }
    public Guid CheckInId { get; set; }
    public Guid GoalFieldId { get; set; }
    public decimal Value { get; set; } // suporta decimal, inteiro e percentual
}
```

### Entidade `ProjectChangeRequest`

```csharp
public class ProjectChangeRequest
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public ChangeRequestType Type { get; set; } // EndDate, Frequency, Goals
    public string PayloadJson { get; set; } = null!; // valores propostos (JSONB no PostgreSQL)
    public ChangeRequestStatus Status { get; set; } // Pending, Approved, Rejected
    public DateTime CreatedAt { get; set; }
}
```

## 6. Decisões Aplicadas

1. **Blazor Server:** escolhido para o MVP. Responsivo, sem lojas; PWA fica para depois.
2. **E-mails transacionais:** serviço genérico `IEmailService` usando MailKit (SMTP) para convite, cadastro, recuperação de senha, lembretes e check-ins.
3. **Metas configuráveis:** `GoalField` com `GoalDataType`, unidade, mínimo, máximo e valor alvo. Valores reais ficam em `CheckInMetric`.
4. **Alterações durante execução:** `ProjectChangeRequest` armazena propostas de mudança em data de fim, frequência ou metas; aprovação mútua exigida.
5. **Frequência do check-in:** `Project.Frequency` (Daily, Weekly, Biweekly, Monthly) definida pelo criador e passível de proposta de alteração.
6. **Lembretes:** cronjob secundário verifica check-ins não respondidos e envia lembretes via `IEmailService`.
7. **Sentimento:** enum `Feeling` com 5 níveis; UI com 5 rostos (SVGs), podendo exibir ícone/cor média no dashboard.
8. **Domínio:** subdomínio do GCP para o MVP.
9. **Multi-idioma:** arquivos `.resx` no backend (`IStringLocalizer`) e no frontend Blazor. API retorna chaves/códigos de mensagens quando necessário.
10. **Custo GCP:** Cloud Run (free tier) + Cloud SQL PostgreSQL de menor tier; usar Secret Manager para credenciais.

## 7. Versionamento e GitFlow

*   Repositório no GitHub com branches `main` (produção) e `develop` (integração).
*   GitFlow simplificado: feature branches saem de `develop` e voltam via pull request; hotfixes saem de `main` quando necessário.
*   Deploy no GCP Cloud Run é acionado apenas por push ou merge para `main`; a branch `develop` roda build e testes, mas não publica em produção.
