using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ResponsabiliMano.Core.Entities;
using ResponsabiliMano.Infrastructure.Data;
using ResponsabiliMano.Infrastructure.Identity;
using ResponsabiliMano.Infrastructure.Services;
using ResponsabiliMano.Infrastructure.Tests.TestHelpers;

namespace ResponsabiliMano.Infrastructure.Tests.Services;

public class PasswordResetServiceTests
{
    private static User SeedUser(AppDbContext context, string email = "user@example.com")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Reset User",
            Email = email,
            PasswordHash = PasswordHasher.Hash("original-password"),
            PreferredLanguage = "pt-BR",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        context.SaveChanges();
        return user;
    }

    private static PasswordResetService CreateService(AppDbContext context, FakeEmailService email)
        => new(context, email, NullLogger<PasswordResetService>.Instance);

    [Fact]
    public async Task RequestResetAsync_UnknownEmail_DoesNothing()
    {
        using var context = TestDbContextFactory.Create();
        var email = new FakeEmailService();
        var service = CreateService(context, email);

        await service.RequestResetAsync("ghost@example.com");

        Assert.Empty(context.PasswordResetTokens);
        Assert.Empty(email.SentEmails);
    }

    [Fact]
    public async Task RequestResetAsync_KnownEmail_CreatesTokenAndSendsEmail()
    {
        using var context = TestDbContextFactory.Create();
        var user = SeedUser(context);
        var email = new FakeEmailService();
        var service = CreateService(context, email);

        await service.RequestResetAsync("USER@example.com");

        var token = Assert.Single(context.PasswordResetTokens);
        Assert.Equal(user.Id, token.UserId);
        Assert.Null(token.UsedAt);
        Assert.True(token.ExpiresAt > DateTime.UtcNow);

        var sent = Assert.Single(email.SentEmails);
        Assert.Equal(user.Email, sent.To);
    }

    [Fact]
    public async Task RequestResetAsync_StoresHashedToken_NotRawTokenFromLink()
    {
        using var context = TestDbContextFactory.Create();
        SeedUser(context);
        var email = new FakeEmailService();
        var service = CreateService(context, email);

        await service.RequestResetAsync("user@example.com");

        var stored = Assert.Single(context.PasswordResetTokens);
        var sentBody = email.SentEmails[0].HtmlBody;
        var rawToken = sentBody.Split("token=")[1].Split('"')[0];

        Assert.NotEqual(rawToken, stored.TokenHash);
        Assert.True(await service.ResetPasswordAsync(rawToken, "brand-new-pass"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ResetPasswordAsync_ReturnsFalse_ForBlankToken(string? token)
    {
        using var context = TestDbContextFactory.Create();
        var service = CreateService(context, new FakeEmailService());

        var result = await service.ResetPasswordAsync(token!, "new-pass");

        Assert.False(result);
    }

    [Fact]
    public async Task ResetPasswordAsync_ReturnsFalse_ForUnknownToken()
    {
        using var context = TestDbContextFactory.Create();
        var service = CreateService(context, new FakeEmailService());

        Assert.False(await service.ResetPasswordAsync("does-not-exist", "new-pass"));
    }

    [Fact]
    public async Task ResetPasswordAsync_ReturnsFalse_ForExpiredToken()
    {
        using var context = TestDbContextFactory.Create();
        var email = new FakeEmailService();
        var service = CreateService(context, email);
        SeedUser(context);
        await service.RequestResetAsync("user@example.com");
        var rawToken = email.SentEmails[0].HtmlBody.Split("token=")[1].Split('"')[0];

        var token = await context.PasswordResetTokens.FirstAsync();
        token.ExpiresAt = DateTime.UtcNow.AddMinutes(-5);
        await context.SaveChangesAsync();

        Assert.False(await service.ResetPasswordAsync(rawToken, "new-pass"));
    }

    [Fact]
    public async Task ResetPasswordAsync_ReturnsFalse_ForAlreadyUsedToken()
    {
        using var context = TestDbContextFactory.Create();
        var email = new FakeEmailService();
        var service = CreateService(context, email);
        SeedUser(context);
        await service.RequestResetAsync("user@example.com");
        var rawToken = email.SentEmails[0].HtmlBody.Split("token=")[1].Split('"')[0];

        Assert.True(await service.ResetPasswordAsync(rawToken, "first-new-pass"));
        Assert.False(await service.ResetPasswordAsync(rawToken, "second-new-pass"));
    }

    [Fact]
    public async Task ResetPasswordAsync_ValidToken_UpdatesPasswordAndMarksUsed()
    {
        using var context = TestDbContextFactory.Create();
        var email = new FakeEmailService();
        var service = CreateService(context, email);
        var user = SeedUser(context);
        await service.RequestResetAsync("user@example.com");
        var rawToken = email.SentEmails[0].HtmlBody.Split("token=")[1].Split('"')[0];

        var result = await service.ResetPasswordAsync(rawToken, "the-new-password");

        Assert.True(result);
        var refreshed = await context.Users.FirstAsync(u => u.Id == user.Id);
        Assert.True(PasswordHasher.Verify("the-new-password", refreshed.PasswordHash));
        var token = await context.PasswordResetTokens.FirstAsync();
        Assert.NotNull(token.UsedAt);
    }
}
