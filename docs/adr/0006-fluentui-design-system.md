# 0006 — FluentUI Blazor como Design System

- **Status:** proposed
- **Data:** 2026-07-29
- **Contexto:** A aplicação foi construída com o template Blazor default
  (Bootstrap 5 sem customização). A UI é funcional mas sem identidade visual.
  O PM aprovou um redesign com paleta "Energia" (Coral + Teal sobre creme),
  tipografia Outfit + Inter, layout mobile-first (bottom nav no mobile,
  sidebar no desktop) e dark mode. Ver `docs/design-brief.md`.
- **Decisão (spec D1):** Substituir Bootstrap por
  `Microsoft.FluentUI.AspNetCore.Components` v4 como único framework de
  componentes UI. FluentUI auto-carrega CSS/JS via static web assets —
  sem `<link>` ou `<script>` manuais para o core. Design tokens customizados
  (paleta "Energia") em `app.css` via CSS custom properties.
  `FluentDesignTheme` com `Mode="System"` para dark/light mode automático.
  Layout responsivo: `FluentNavMenu` (sidebar) no desktop, bottom navigation
  bar custom no mobile. Google Fonts (Outfit + Inter) via CDN.
- **Compatibilidade com ADR-0003:** O render mode **por página** é mantido.
  A troca de framework CSS não afeta a estratégia de render mode — páginas
  estáticas continuam SSR, páginas interativas continuam `InteractiveServer`.
- **Consequências:** Bootstrap é totalmente removido. Classes Bootstrap
  residuais nas páginas existentes (btn, form-control, card, etc.) não
  causam erro (são classes sem estilo), mas serão substituídas por
  componentes FluentUI nas specs D2 e D3. Os providers do FluentUI
  (Toast, Dialog, MessageBar, Tooltip, KeyCode) devem estar no MainLayout
  para que os serviços funcionem.
- **Alternativas consideradas:** MudBlazor (outra opção popular, mas
  FluentUI oferece melhor alinhamento com design system Microsoft e
  dark mode nativo); manter Bootstrap e customizar (menor esforço inicial,
  mas resultado menos distintivo); TailwindCSS (flexibilidade máxima,
  mas exige setup manual de componentes).
