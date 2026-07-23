using ResponsabiliMano.Core.Services;

namespace ResponsabiliMano.Infrastructure.Tests.TestHelpers;

internal sealed class FakeEmailService : IEmailService
{
    public List<SentEmail> SentEmails { get; } = new();

    public Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        SentEmails.Add(new SentEmail(to, subject, htmlBody));
        return Task.CompletedTask;
    }
}

internal sealed record SentEmail(string To, string Subject, string HtmlBody);
