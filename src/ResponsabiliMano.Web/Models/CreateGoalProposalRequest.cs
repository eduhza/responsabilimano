using System.ComponentModel.DataAnnotations;
using ResponsabiliMano.Core.Enums;

namespace ResponsabiliMano.Web.Models;

public sealed class CreateGoalProposalRequest
{
    public decimal? Baseline { get; set; }

    public decimal? TargetValue { get; set; }

    [Required(ErrorMessage = "Direction is required.")]
    public GoalDirection Direction { get; set; }

    [StringLength(500, ErrorMessage = "Comment cannot exceed 500 characters.")]
    public string? Comment { get; set; }
}
