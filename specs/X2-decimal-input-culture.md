---
id: X2
feature: Correção de comportamento — entrada numérica
pod: ResponsabiliMano (solo)
priority: P0
iteration: 1 (2-3 dias)
contract: none
tracking: gh-issue-#TBD
status: done
depends_on: []
adr: []
---

# Corrigir perda de casas decimais na entrada de números

## Contexto
Reproduzido: `<input type="number">` do HTML **sempre** envia o valor no formato
invariante (`3.6`, `96.8`), independentemente do idioma do navegador. O servidor
parseia com `CultureInfo.CurrentCulture`, que é `pt-BR`
(`Program.cs` → `SetDefaultCulture("pt-BR")`), e em pt-BR o `.` é **separador de
milhar**:

| Entrada digitada | String recebida | `decimal.TryParse` (pt-BR) | Gravado |
|---|---|---|---|
| `3,6` L | `"3.6"` | `true` | **36** |
| `96,8` kg | `"96.8"` | `true` | **968** |

Pontos com o defeito:
- `ProjectDetail.razor` → `ParseDecimal(object?)` (propor alteração de metas).
- `CheckInEditor.razor` → `decimal.TryParse(e.Value?.ToString(), out var v)`.
- Bug espelho na **exibição**: `value="@entry.Value"` renderiza `3,6` em pt-BR,
  formato que `input type=number` rejeita — o campo volta vazio ao reabrir.
- `CreateProject.razor` usa `InputNumber`, que é invariante nos dois sentidos
  (correto hoje), mas aceita `NumberStyles.AllowThousands`: se receber `3,6`
  também produz `36`.

## User Value
Como usuário brasileiro, quero digitar `3,6` ou `3.6` e ver `3,6` gravado, tanto
ao definir uma meta quanto ao registrar um check-in.

## Acceptance Criteria
1. `DecimalInput.TryParse` (Core) é uma função pura com este comportamento
   exato — cada linha vira um caso de teste:

   | Entrada | Resultado |
   |---|---|
   | `"3,6"` | `3.6` |
   | `"3.6"` | `3.6` |
   | `"96,8"` | `96.8` |
   | `"1.234,56"` | `1234.56` |
   | `"1,234.56"` | `1234.56` |
   | `"1.234.567"` | `1234567` |
   | `"1234"` | `1234` |
   | `"-2,5"` | `-2.5` |
   | `" 3,6 "` | `3.6` |
   | `""`, `null`, `"abc"`, `"3,6,"` | `null` |

   Regra: (a) se `.` e `,` aparecem, o **último a ocorrer** é o separador decimal
   e o outro é de milhar; (b) se só um tipo aparece **uma vez**, ele é o separador
   decimal; (c) se só um tipo aparece **mais de uma vez**, todos são de milhar;
   (d) depois de normalizar, parse invariante com
   `NumberStyles.AllowLeadingSign | AllowDecimalPoint | AllowLeadingWhite | AllowTrailingWhite`
   (**sem** `AllowThousands`).
2. `DecimalInput.ToInvariant(decimal?)` formata para atributos HTML (sempre `.`) e
   `DecimalInput.ToDisplay(decimal?, GoalDataType)` formata para exibição em pt-BR.
3. Novo `Design/RmNumberInput.razor` renderiza `type="text" inputmode="decimal"`
   (teclado numérico no celular, vírgula permitida), faz round-trip sem perder o
   valor digitado e substitui os campos numéricos de `CheckInEditor.razor`,
   `ProjectDetail.razor` e `CreateProject.razor`.
4. Regressão coberta: registrar check-in com `3,6` grava `3.6m`; com `96,8` grava
   `96.8m`; propor meta com `3,6` grava `3.6m`. Nenhum valor é multiplicado por 10.
5. Reabrir um formulário com valor `3.6m` exibe `3,6` e o submit sem alteração
   grava `3.6m` (não perde nem reformata errado).
6. Validação server-side por `GoalDataType`, com mensagem localizada:
   `Integer` rejeita fracionário; `Percent` exige `0 ≤ v ≤ 100`; `Decimal`
   normaliza para no máximo 4 casas (`Math.Round(v, 4, MidpointRounding.ToEven)`).
   Aplicada em `CheckInService.ValidateMetrics` **e** em `ProjectService`
   (`CreateProjectAsync` e `ApplyGoalChanges`) — cliente e servidor, nunca só um.
7. Nenhum `decimal.TryParse` / `decimal.Parse` sem cultura explícita permanece em
   `src/` (guard por teste de varredura ou análise, para não regredir).

## Data Model
- Sem mudança de esquema. Sem migration.
- Novo tipo puro `ResponsabiliMano.Core.Common.DecimalInput` (static).

## Security Constraints
- Entrada numérica continua validada no servidor (AC 6) — o cliente é
  conveniência, não fronteira de confiança.
- Sem PII em log; nada de logar o valor digitado junto do e-mail do usuário.

## API / Event Contract
- none. Os contratos JSON (`SubmitCheckInRequest`, `GoalFieldRequest`) já
  trafegam `number` e são desserializados por `System.Text.Json` em cultura
  invariante — não são afetados.

## Dependencies
- Nenhuma. Deve entrar antes de S7.1 (editar check-in) para que a correção pela
  UI grave o valor certo.

## Out of Scope
- Corrigir os dados já gravados errados (36 L, 968 kg): sem script de migração de
  dados, por decisão do PM. A correção será feita pela UI depois de S7.1.
- Fixar precisão da coluna `numeric` no banco — entra em S7.2, que já traz migration.
- Novos tipos de meta (S7.4).

## Verification
- `dotnet build ResponsabiliMano.slnx` e `dotnet test` verdes.
- Testes novos: `DecimalInputTests` (tabela do AC 1, um `[Theory]`);
  `CheckInServiceTests` para o AC 6; bUnit em `CheckInEditor` e `ProjectDetail`
  cobrindo AC 4 e 5, com `CultureInfo` de teste forçada em `pt-BR` (senão o teste
  passa em máquina en-US e o bug volta).
- Manual: com o app em pt-BR, criar meta "Água" alvo `3,6` L e conferir `3,6` na
  tela de detalhe; fazer check-in de peso `96,8` e conferir `96,8`.
