namespace ResponsabiliMano.Core.Common;

/// <summary>
/// Helpers for normalizing and validating user-supplied email addresses.
/// </summary>
public static class EmailAddress
{
    /// <summary>
    /// Trims surrounding whitespace and lower-cases the address for consistent storage and comparison.
    /// </summary>
    public static string Normalize(string email) => email.Trim().ToLowerInvariant();

    /// <summary>
    /// Performs the lightweight presence/shape check used across the API surface.
    /// </summary>
    public static bool IsValid(string? email) =>
        !string.IsNullOrWhiteSpace(email) && email.Contains('@');
}
