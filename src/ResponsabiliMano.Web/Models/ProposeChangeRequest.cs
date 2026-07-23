using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using ResponsabiliMano.Core.Enums;

namespace ResponsabiliMano.Web.Models;

public sealed class ProposeChangeRequest
{
    [Required(ErrorMessage = "Change type is required.")]
    public ChangeRequestType Type { get; set; }

    public DateTime? NewEndDate { get; set; }

    public ProjectFrequency? NewFrequency { get; set; }

    public List<GoalFieldRequest>? Goals { get; set; }

    public string ToPayloadJson()
    {
        return Type switch
        {
            ChangeRequestType.EndDate when NewEndDate.HasValue =>
                JsonSerializer.Serialize(new { EndDate = NewEndDate.Value }),
            ChangeRequestType.Frequency when NewFrequency.HasValue =>
                JsonSerializer.Serialize(new { Frequency = NewFrequency.Value }),
            ChangeRequestType.Goals when Goals is not null && Goals.Count > 0 =>
                JsonSerializer.Serialize(new { Goals = Goals.Select(g => new {
                    g.Label, g.DataType, g.Unit, g.MinValue, g.MaxValue, g.TargetValue
                }) }),
            _ => throw new ArgumentException("Invalid payload for the specified change type.")
        };
    }
}
