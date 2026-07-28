using System.Collections.Concurrent;
using ResponsabiliMano.Core.Services;

namespace ResponsabiliMano.Web.E2ETests;

/// <summary>
/// Test-only <see cref="IEmailService"/> that records every e-mail in memory so
/// E2E tests can assert subjects, recipients and link contents without a real
/// SMTP server.
/// </summary>
public sealed class CapturedEmailService : IEmailService
{
    private readonly ConcurrentQueue<EmailMessage> _emails = new();

    public Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        _emails.Enqueue(new EmailMessage(to, subject, htmlBody));
        return Task.CompletedTask;
    }

    public IReadOnlyCollection<EmailMessage> GetAll() => _emails.ToArray();

    public IReadOnlyCollection<EmailMessage> GetTo(string email) =>
        _emails.Where(e => e.To.Equals(email, StringComparison.OrdinalIgnoreCase)).ToArray();

    public EmailMessage? GetLastTo(string email) => GetTo(email).LastOrDefault();

    public void Clear() => _emails.Clear();

    public record EmailMessage(string To, string Subject, string HtmlBody);
}
