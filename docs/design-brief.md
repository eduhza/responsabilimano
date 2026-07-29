# Design Brief — ResponsabiliMano

**Versão:** 1.0 — Decisões do brainstorm de redesign
**Data:** 2026-07-29

## 1. Contexto

A aplicação ResponsabiliMano foi concluída funcionalmente, mas utiliza o estilo default do template Blazor (Bootstrap 5 sem customização). Este brief define a direção de design para tornar a UI atrativa, mobile-first e distintiva.

## 2. Conceito

**"Diário de um par motivado"** — a app é sobre parceria, motivação e progresso. O design deve parecer um coach pessoal acessível: quente, encorajador, com senso de momentum. Não clínico, não corporativo.

## 3. Paleta — "Energia"

| Token | Cor | Uso |
|---|---|---|
| `--rm-primary` | `#E85D4E` (Coral) | Ações primárias, links, destaques |
| `--rm-primary-dark` | `#C84A3D` | Hover/active states |
| `--rm-secondary` | `#0D5C63` (Teal profundo) | Elementos secundários, headers |
| `--rm-accent` | `#F4A261` (Âmbar suave) | Badges, alertas de progresso |
| `--rm-bg` | `#FAF6F0` (Creme) | Background principal |
| `--rm-surface` | `#FFFFFF` | Cards, inputs, surfaces |
| `--rm-text` | `#2D2D2D` | Texto principal |
| `--rm-text-muted` | `#7A7A7A` | Texto secundário |

### Dark mode

Tokens correspondentes para tema escuro (via `FluentDesignTheme`):

| Token | Light | Dark |
|---|---|---|
| `--rm-bg` | `#FAF6F0` | `#1A1A1A` |
| `--rm-surface` | `#FFFFFF` | `#2A2A2A` |
| `--rm-text` | `#2D2D2D` | `#E8E8E8` |
| `--rm-text-muted` | `#7A7A7A` | `#A0A0A0` |

## 4. Tipografia

| Role | Fonte | Uso |
|---|---|---|
| Display | `Outfit` (Google Fonts) | Títulos, headers, nomes de projeto |
| Body | `Inter` (Google Fonts) | Texto geral, labels, descrições |
| Dados | `Inter` com `tabular-nums` | Métricas, números de check-in, valores |

### Escala tipográfica

| Nível | Tamanho | Peso | Uso |
|---|---|---|---|
| H1 | 2rem (32px) | 700 | Título de página |
| H2 | 1.5rem (24px) | 600 | Seções dentro de página |
| H3 | 1.25rem (20px) | 600 | Subseções, títulos de card |
| Body | 1rem (16px) | 400 | Texto padrão |
| Small | 0.875rem (14px) | 400 | Labels, captions, hints |
| Data | 1.5rem (24px) | 600 | Valores de métricas em destaque |

## 5. Layout

### Mobile (< 641px)

- **Bottom navigation bar** fixa na parte inferior com 3 itens: Home, Novo Projeto, Sair
- Conteúdo em coluna única, full-width, padding 16px
- Cards com border-radius 12px, sombra suave
- Inputs touch-friendly (min-height 44px)
- Sem sidebar

### Desktop (≥ 641px)

- **Sidebar** à esquerda (240px) com logo + navegação vertical
- Conteúdo central com max-width 900px
- Dashboard: grid de 2 colunas para cards, chart full-width
- Sidebar oculta no mobile, bottom nav oculta no desktop

## 6. Componentes FluentUI Blazor

Substituição completa do Bootstrap por `Microsoft.FluentUI.AspNetCore.Components`:

| Atual | Proposto |
|---|---|
| Bootstrap CSS | FluentUI (auto-loaded, sem `<link>` manual) |
| `btn-primary` | `FluentButton Appearance="Accent"` |
| `form-control` | `FluentTextField` |
| `form-select` | `FluentSelect` com `Items`/`OptionText`/`OptionValue` |
| `alert-danger/success` | `FluentMessageBar` |
| `card` | `FluentCard` |
| `list-group` | Cards customizados com `FluentCard` |
| Sidebar custom | `FluentNavMenu` + `FluentNavItem` |
| Validação | `FluentValidationMessage` / `FluentValidationSummary` |
| Feedback | `IToastService` + `FluentToastProvider` |
| Confirmações | `IDialogService` + `FluentDialogProvider` |
| Tema | `FluentDesignTheme Mode="DesignThemeModes.System"` |

### Providers obrigatórios no MainLayout

```razor
<FluentToastProvider />
<FluentDialogProvider />
<FluentMessageBarProvider />
<FluentTooltipProvider />
<FluentKeyCodeProvider />
```

### Registro no Program.cs

```csharp
builder.Services.AddFluentUIComponents();
```

## 7. Elemento assinatura — Streak de check-ins

Linha do tempo horizontal de pontos mostrando check-ins preenchidos vs. pendentes. Fica no topo do `ProjectDetail` e `Dashboard`.

- Cada período é um círculo: preenchido (verde coral) = check-in feito; vazio (cinza) = pendente
- Hover mostra tooltip com período e data
- Click navega para o check-in daquele período
- Scroll horizontal quando há muitos períodos

## 8. Faces de sentimento

As 5 faces SVG atuais serão estilizadas com:
- Tamanho maior (40px em vez de 28px)
- Cor de fundo circular colorida por nível:
  - VerySad: vermelho `#E85D4E`
  - Sad: laranja `#F4A261`
  - Neutral: cinza `#B0B0B0`
  - Happy: verde `#5B9279`
  - VeryHappy: verde-vibrante `#2D9D78`
- Estado selecionado: anel de destaque na cor correspondente

## 9. Outros elementos

### Skeleton loading

- Skeleton screens (pulsing gray blocks) durante carregamento de dados
- Substitui texto "Carregando..." em todas as telas

### Empty states

- Quando não há projetos: ilustração + "Crie seu primeiro projeto"
- Quando não há check-ins: ilustração + "Aguarde o próximo período de check-in"
- Quando dashboard sem dados: ilustração + "Os dados aparecerão após o primeiro check-in"

### Micro-interações

- Hover suave em botões e cards (transição 150ms)
- Transições de página sutis
- Toast notifications para sucesso/erro de operações

## 10. Estratégia de implementação

3 specs atômicas, na ordem:

### Spec D1 — Fundação: FluentUI + Tema + Layout

- Instalar `Microsoft.FluentUI.AspNetCore.Components` + `Microsoft.FluentUI.AspNetCore.Components.Icons`
- Registrar serviços no `Program.cs`
- Adicionar providers no `MainLayout.razor`
- Substituir `app.css` e CSS do layout pelos design tokens
- Novo `MainLayout` com sidebar (desktop) + bottom nav (mobile)
- Novo `NavMenu` usando `FluentNavMenu`
- `FluentDesignTheme` para dark/light/system
- Carregar Google Fonts (Outfit + Inter)
- Remover dependência do Bootstrap

### Spec D2 — Telas Auth + Home

- Refatorar: Login, Register, ForgotPassword, ResetPassword, Home
- Usar `FluentTextField`, `FluentButton`, `FluentMessageBar`
- Skeleton loading states
- Empty states na Home (sem projetos)
- Toast notifications para feedback

### Spec D3 — Telas de Projeto + CheckIn + Dashboard

- Refatorar: CreateProject, InvitePartner, ProjectDetail, InvitationAccept, CheckIn, Dashboard
- Streak de check-ins no ProjectDetail e Dashboard
- Faces de sentimento redesenhadas (maiores, coloridas)
- Dashboard com `FluentCard`, chart mantido (Chart.js)
- Skeleton loading + empty states
- Dialogs de confirmação (aprovar/rejeitar propostas)

## 11. Fora do escopo

- PWA / instalação mobile
- Animações complexas (parallax, scroll-triggered)
- Ilustrações customizadas (usar SVGs simples ou placeholder)
- Internacionalização de novos textos (manter pt-BR, estrutura .resx existente)
