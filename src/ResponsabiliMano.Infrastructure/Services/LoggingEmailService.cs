using Microsoft.Extensions.Logging;
using ResponsabiliMano.Core.Services;

namespace ResponsabiliMano.Infrastructure.Services;

public sealed class LoggingEmailService : IEmailService
{
    private readonly ILogger<LoggingEmailService> _logger;

    public LoggingEmailService(ILogger<LoggingEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Email sent (Dev): RecipientLength={ToHash}", to.Length);
        return Task.CompletedTask;
    }
}
