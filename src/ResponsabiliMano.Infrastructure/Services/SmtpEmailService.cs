using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Infrastructure.Configuration;

namespace ResponsabiliMano.Infrastructure.Services;

/// <summary>
/// Sends HTML email over SMTP (MailKit). Used in production, where
/// <see cref="EmailSettings.SmtpPassword"/> is provided via Secret Manager;
/// locally the app registers <see cref="LoggingEmailService"/> instead.
/// </summary>
public sealed class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<EmailSettings> settings, ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var message = BuildMessage(to, subject, htmlBody);

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls, cancellationToken);
        await client.AuthenticateAsync(_settings.SmtpUser, _settings.SmtpPassword, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);

        // No PII in logs: recipient is not logged (rule security.md).
        _logger.LogInformation("Email sent: subject {Subject}", subject);
    }

    /// <summary>
    /// Builds the MIME message. Separated from the network I/O so the address and
    /// body mapping can be unit-tested without an SMTP server.
    /// </summary>
    internal MimeMessage BuildMessage(string to, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();
        return message;
    }
}
