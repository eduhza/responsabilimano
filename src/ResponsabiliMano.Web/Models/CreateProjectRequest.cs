using System.ComponentModel.DataAnnotations;
using ResponsabiliMano.Core.Enums;

namespace ResponsabiliMano.Web.Models;

public sealed class CreateProjectRequest
{
    [Required(ErrorMessage = "Project name is required.")]
    [StringLength(200, ErrorMessage = "Project name cannot exceed 200 characters.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Emoji picked in step one; purely decorative, so it stays optional.</summary>
    [StringLength(16)]
    public string? Icon { get; set; }

    [Required(ErrorMessage = "Start date is required.")]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "End date is required.")]
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(30);

    [Required(ErrorMessage = "Frequency is required.")]
    public ProjectFrequency Frequency { get; set; } = ProjectFrequency.Weekly;

    [Required(ErrorMessage = "At least one goal is required.")]
    [MinLength(1, ErrorMessage = "At least one goal is required.")]
    public List<CreateProjectGoalRequest> Goals { get; set; } = new();
}

public sealed class CreateProjectGoalRequest
{
    [Required]
    public GoalFieldRequest Goal { get; set; } = new();

    [Required]
    public GoalTargetRequest CreatorTarget { get; set; } = new();

    public GoalTargetRequest SuggestedPartnerTarget { get; set; } = new();
}

public sealed class GoalFieldRequest
{
    [Required(ErrorMessage = "Label is required.")]
    [StringLength(200, ErrorMessage = "Label cannot exceed 200 characters.")]
    public string Label { get; set; } = string.Empty;

    [Required(ErrorMessage = "Data type is required.")]
    public GoalDataType DataType { get; set; } = GoalDataType.Decimal;

    [StringLength(50, ErrorMessage = "Unit cannot exceed 50 characters.")]
    public string Unit { get; set; } = string.Empty;

    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
}

public sealed class GoalTargetRequest
{
    public decimal? Baseline { get; set; }
    public decimal? TargetValue { get; set; }
    public GoalDirection Direction { get; set; } = GoalDirection.Reach;
}
