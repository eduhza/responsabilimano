using System.ComponentModel.DataAnnotations;

namespace ResponsabiliMano.Web.Models;

public sealed class ForgotPasswordRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; } = string.Empty;
}
