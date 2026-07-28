namespace ResponsabiliMano.Infrastructure.Configuration;

/// <summary>
/// SMTP settings bound from the <c>EmailSettings</c> configuration section. The
/// password is never committed — it is supplied at runtime via the environment
/// (<c>EmailSettings__SmtpPassword</c>, backed by Secret Manager in production).
/// When <see cref="SmtpPassword"/> is empty the app falls back to the logging
/// email service (see DI registration), so local development sends nothing.
/// </summary>
public sealed class EmailSettings
{
    public const string SectionName = "EmailSettings";

    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = "";
    public string SmtpPassword { get; set; } = "";
    public string FromName { get; set; } = "";
    public string FromEmail { get; set; } = "";

    /// <summary>True when a password is configured, i.e. real sending is enabled.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(SmtpPassword);
}
