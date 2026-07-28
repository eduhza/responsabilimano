using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;
using ResponsabiliMano.Infrastructure.Configuration;
using ResponsabiliMano.Infrastructure.Services;

namespace ResponsabiliMano.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for the MIME-message building of <see cref="SmtpEmailService"/>.
/// The network path (connect/authenticate/send) is I/O and is exercised in the
/// deployed smoke test, not here.
/// </summary>
public class SmtpEmailServiceTests
{
    private static SmtpEmailService CreateService() => new(
        Options.Create(new EmailSettings
        {
            SmtpHost = "smtp.example.com",
            SmtpPort = 587,
            SmtpUser = "user@example.com",
            SmtpPassword = "secret",
            FromName = "Clube BomVoar",
            FromEmail = "no-reply@bomvoarturismo.com"
        }),
        NullLogger<SmtpEmailService>.Instance);

    [Fact]
    public void BuildMessage_MapsFromToSubjectAndHtmlBody()
    {
        var service = CreateService();

        var message = service.BuildMessage("dest@example.com", "Assunto", "<h2>Ola</h2>");

        var from = Assert.IsType<MailboxAddress>(Assert.Single(message.From));
        Assert.Equal("Clube BomVoar", from.Name);
        Assert.Equal("no-reply@bomvoarturismo.com", from.Address);

        var to = Assert.IsType<MailboxAddress>(Assert.Single(message.To));
        Assert.Equal("dest@example.com", to.Address);

        Assert.Equal("Assunto", message.Subject);
        Assert.Contains("<h2>Ola</h2>", message.HtmlBody);
    }
}
