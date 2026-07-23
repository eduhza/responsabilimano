using ResponsabiliMano.Core.Entities;
using ResponsabiliMano.Infrastructure.Data;
using ResponsabiliMano.Infrastructure.Identity;
using ResponsabiliMano.Infrastructure.Services;
using ResponsabiliMano.Infrastructure.Tests.TestHelpers;

namespace ResponsabiliMano.Infrastructure.Tests.Services;

public class UserLoginServiceTests
{
    private static User SeedUser(AppDbContext context, string email, string password)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = email,
            PasswordHash = PasswordHasher.Hash(password),
            PreferredLanguage = "pt-BR",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        context.SaveChanges();
        return user;
    }

    [Fact]
    public async Task LoginAsync_ReturnsUser_ForValidCredentials()
    {
        using var context = TestDbContextFactory.Create();
        SeedUser(context, "dave@example.com", "goodpass");
        var service = new UserLoginService(context);

        var result = await service.LoginAsync("dave@example.com", "goodpass");

        Assert.NotNull(result);
        Assert.Equal("dave@example.com", result!.Email);
    }

    [Fact]
    public async Task LoginAsync_NormalizesEmail_BeforeLookup()
    {
        using var context = TestDbContextFactory.Create();
        SeedUser(context, "erin@example.com", "goodpass");
        var service = new UserLoginService(context);

        var result = await service.LoginAsync("  Erin@Example.COM  ", "goodpass");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task LoginAsync_ReturnsNull_ForUnknownEmail()
    {
        using var context = TestDbContextFactory.Create();
        var service = new UserLoginService(context);

        var result = await service.LoginAsync("nobody@example.com", "whatever");

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_ReturnsNull_ForWrongPassword()
    {
        using var context = TestDbContextFactory.Create();
        SeedUser(context, "frank@example.com", "goodpass");
        var service = new UserLoginService(context);

        var result = await service.LoginAsync("frank@example.com", "wrongpass");

        Assert.Null(result);
    }
}
