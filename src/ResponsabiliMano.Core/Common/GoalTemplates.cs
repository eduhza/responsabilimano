using ResponsabiliMano.Core.Enums;

namespace ResponsabiliMano.Core.Common;

/// <summary>
/// Static, version-controlled catalog of goal templates (spec S7.5). Not persisted,
/// not user-editable: each template is a ready-made starting point that pre-fills the
/// creation wizard. The server revalidates every field as if it were typed — the
/// template is only a convenience, never a source of truth.
/// </summary>
public static class GoalTemplates
{
    /// <summary>Slug used only for logging which template was picked.</summary>
    public const string BlankKey = "blank";

    public static readonly GoalTemplate SaudeECorpo = new(
        "saude",
        "Saúde & Corpo",
        "💪",
        ProjectFrequency.Weekly,
        [
            new GoalTemplateGoal("Peso", GoalDataType.Decimal, "kg", null, null, 90m, 85m, GoalDirection.Decrease),
            new GoalTemplateGoal("Adesão aos treinos", GoalDataType.Percent, "%", 0m, 100m, null, 100m, GoalDirection.Reach),
            new GoalTemplateGoal("Água por dia", GoalDataType.Decimal, "L", 0m, null, null, 2.5m, GoalDirection.Reach),
            new GoalTemplateGoal("Como me senti", GoalDataType.Scale, "nota", 1m, 5m, null, 4m, GoalDirection.Increase)
        ]);

    public static readonly GoalTemplate ConcursoEProvas = new(
        "concurso",
        "Concurso & Provas",
        "📚",
        ProjectFrequency.Weekly,
        [
            new GoalTemplateGoal("Horas de estudo", GoalDataType.Decimal, "h", 0m, null, 5m, 20m, GoalDirection.Increase),
            new GoalTemplateGoal("Questões resolvidas", GoalDataType.Integer, "questões", 0m, null, 20m, 100m, GoalDirection.Increase),
            new GoalTemplateGoal("Simulado feito?", GoalDataType.Boolean, "", 0m, 1m, 0m, 1m, GoalDirection.Reach),
            new GoalTemplateGoal("Foco na semana", GoalDataType.Scale, "nota", 1m, 5m, null, 4m, GoalDirection.Increase)
        ]);

    public static readonly GoalTemplate ProgramacaoECarreira = new(
        "carreira",
        "Programação & Carreira",
        "💻",
        ProjectFrequency.Weekly,
        [
            new GoalTemplateGoal("Horas de código", GoalDataType.Decimal, "h", 0m, null, 3m, 15m, GoalDirection.Increase),
            new GoalTemplateGoal("Commits/exercícios", GoalDataType.Integer, "commits", 0m, null, 2m, 10m, GoalDirection.Increase),
            new GoalTemplateGoal("Estudei algo novo?", GoalDataType.Boolean, "", 0m, 1m, 0m, 1m, GoalDirection.Reach),
            new GoalTemplateGoal("Confiança técnica", GoalDataType.Scale, "nota", 1m, 5m, null, 4m, GoalDirection.Increase)
        ]);

    public static readonly GoalTemplate EmpregoNovo = new(
        "emprego",
        "Emprego novo",
        "🎯",
        ProjectFrequency.Weekly,
        [
            new GoalTemplateGoal("Vagas aplicadas", GoalDataType.Integer, "vagas", 0m, null, 0m, 10m, GoalDirection.Increase),
            new GoalTemplateGoal("Entrevistas", GoalDataType.Integer, "entrevistas", 0m, null, 0m, 3m, GoalDirection.Increase),
            new GoalTemplateGoal("Currículo atualizado?", GoalDataType.Boolean, "", 0m, 1m, 0m, 1m, GoalDirection.Reach),
            new GoalTemplateGoal("Networking na semana", GoalDataType.Integer, "contatos", 0m, null, 1m, 5m, GoalDirection.Increase)
        ]);

    public static readonly GoalTemplate FinancasEHabitos = new(
        "financas",
        "Finanças & Hábitos",
        "💰",
        ProjectFrequency.Weekly,
        [
            new GoalTemplateGoal("Valor guardado", GoalDataType.Decimal, "R$", 0m, null, 100m, 500m, GoalDirection.Increase),
            new GoalTemplateGoal("Gastos por impulso", GoalDataType.Integer, "gastos", 0m, null, 8m, 3m, GoalDirection.Decrease),
            new GoalTemplateGoal("Cumpri o orçamento?", GoalDataType.Boolean, "", 0m, 1m, 0m, 1m, GoalDirection.Reach)
        ]);

    /// <summary>All templates, in display order.</summary>
    public static readonly IReadOnlyList<GoalTemplate> All =
    [
        SaudeECorpo,
        ConcursoEProvas,
        ProgramacaoECarreira,
        EmpregoNovo,
        FinancasEHabitos
    ];

    /// <summary>Looks up a template by its key, or null when not found.</summary>
    public static GoalTemplate? Find(string key) =>
        All.FirstOrDefault(t => string.Equals(t.Key, key, StringComparison.OrdinalIgnoreCase));
}

/// <summary>A ready-made project starting point (spec S7.5).</summary>
/// <param name="Key">Stable slug for logging only; never sent to the server.</param>
/// <param name="Name">Suggested project name.</param>
/// <param name="Icon">Emoji shown in the gallery and pre-filled as the project icon.</param>
/// <param name="Frequency">Suggested check-in frequency.</param>
/// <param name="Goals">3–5 pre-configured goals.</param>
public sealed record GoalTemplate(
    string Key,
    string Name,
    string Icon,
    ProjectFrequency Frequency,
    IReadOnlyList<GoalTemplateGoal> Goals);

/// <summary>One pre-configured goal inside a <see cref="GoalTemplate"/>.</summary>
/// <param name="Label">Goal label.</param>
/// <param name="DataType">How the value is captured and rendered.</param>
/// <param name="Unit">Unit text; empty only for <see cref="GoalDataType.Boolean"/>.</param>
/// <param name="Min">Validation lower bound.</param>
/// <param name="Max">Validation upper bound.</param>
/// <param name="Baseline">Optional starting point for Decrease/Increase progress.</param>
/// <param name="TargetValue">The target to reach.</param>
/// <param name="Direction">How progress is computed.</param>
public sealed record GoalTemplateGoal(
    string Label,
    GoalDataType DataType,
    string Unit,
    decimal? Min,
    decimal? Max,
    decimal? Baseline,
    decimal? TargetValue,
    GoalDirection Direction);
