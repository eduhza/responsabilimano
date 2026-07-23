using System.ComponentModel.DataAnnotations;

namespace ResponsabiliMano.Web.Models;

public sealed class InvitePartnerRequest
{
    [Required(ErrorMessage = "Partner email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters.")]
    public string PartnerEmail { get; set; } = string.Empty;
}
