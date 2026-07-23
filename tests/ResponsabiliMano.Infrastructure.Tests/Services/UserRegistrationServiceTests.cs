using ResponsabiliMano.Infrastructure.Identity;
using ResponsabiliMano.Infrastructure.Services;
using ResponsabiliMano.Infrastructure.Tests.TestHelpers;

namespace ResponsabiliMano.Infrastructure.Tests.Services;

public class UserRegistrationServiceTests
{
    [Fact]
    public async Task RegisterAsync_PersistsUser_WithNormalizedEmailAndTrimmedName()
    {
        using var context = TestDbContextFactory.Create();
        var service = new UserRegistrationService(context);

        var user = await service.RegisterAsync("  Alice  ", "  Alice@Example.COM ", "secret123");

        Assert.Equal("Alice", user.Name);
        Assert.Equal("alice@example.com", user.Email);
        Assert.Equal("pt-BR", user.PreferredLanguage);
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Single(context.Users);
    }

    [Fact]
    public async Task RegisterAsync_StoresHashedPassword_NotPlaintext()
    {
        using var context = TestDbContextFactory.Create();
        var service = new UserRegistrationService(context);

        var user = await service.RegisterAsync("Bob", "bob@example.com", "plaintext-pass");

        Assert.NotEqual("plaintext-pass", user.PasswordHash);
        Assert.True(PasswordHasher.Verify("plaintext-pass", user.PasswordHash));
    }

    [Fact]
    public async Task RegisterAsync_Throws_WhenEmailAlreadyRegistered_CaseInsensitive()
    {
        using var context = TestDbContextFactory.Create();
        var service = new UserRegistrationService(context);
        await service.RegisterAsync("Carol", "carol@example.com", "pw1");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RegisterAsync("Carol Again", "CAROL@example.com", "pw2"));

        Assert.Equal("Email already registered.", ex.Message);
        Assert.Single(context.Users);
    }
}
