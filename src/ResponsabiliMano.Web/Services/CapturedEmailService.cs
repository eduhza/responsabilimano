using System.Collections.Concurrent;
using ResponsabiliMano.Core.Services;

namespace ResponsabiliMano.Web.Services;

public sealed class CapturedEmailService : IEmailService
{
    private readonly ConcurrentQueue<EmailMessage> _emails = new();

    public IReadOnlyCollection<EmailMessage> GetEmails() => _emails.ToArray();

    public void Clear() => _emails.Clear();

    public Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        _emails.Enqueue(new EmailMessage(to, subject, htmlBody));
        return Task.CompletedTask;
    }
}

public sealed record EmailMessage(string To, string Subject, string HtmlBody);
