# Architecture Decision Records (ADRs)

Decisões estruturais vivem aqui, uma por arquivo, numeradas sequencialmente.
Substituem as decisões numeradas embutidas em `architecture.md` (que passa a
apontar para cá). Regra: toda escolha de padrão, dependência, tecnologia ou
arquitetura vira um ADR, referenciado pela spec que o motivou (campo `adr:`).

## Status possíveis
`proposed` → `accepted` → (`superseded by NNNN` | `deprecated`)

## Template

```markdown
# NNNN — <título da decisão>

- **Status:** proposed | accepted | superseded by ADR-XXXX
- **Data:** AAAA-MM-DD
- **Contexto:** <forças em jogo, restrições, problema>
- **Decisão:** <o que foi decidido>
- **Consequências:** <trade-offs, o que fica mais fácil/difícil>
- **Alternativas consideradas:** <opções descartadas e por quê>
```

## Índice

| ADR | Título | Status |
|---|---|---|
| 0001 | Adotar o AI-Native SDLC com 3 gates humanos | accepted |
| 0002 | Organização dos endpoints e validação | proposed |
| 0003 | Render mode do Blazor (Interactive Server) | proposed |
