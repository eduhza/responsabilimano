using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ResponsabiliMano.Core.Entities;
using ResponsabiliMano.Core.Services;
using ResponsabiliMano.Web;
using ResponsabiliMano.Web.Components.Pages;

namespace ResponsabiliMano.Web.Tests.Pages;

// Behavior guard for spec X1: with the Interactive Server render mode, the EditForm
// fields bind and the submit handler runs with the typed values. bUnit renders the
// component interactively, exercising the same bind/submit path the browser uses once
// the circuit is live. (Presence of @rendermode itself is covered by RenderModeTests.)
public class RegisterTests : TestContext
{
    public RegisterTests()
    {
        Services.AddSingleton<IStringLocalizer<AppStrings>>(new PassthroughLocalizer());
        Services.AddSingleton<ILogger<Register>>(NullLogger<Register>.Instance);
    }

    [Fact]
    public void Valid_submit_registers_user_with_the_typed_values()
    {
        var registration = new SpyRegistrationService();
        Services.AddSingleton<IUserRegistrationService>(registration);

        var cut = RenderComponent<Register>();

        cut.Find("#name").Change("Ana Tester");
        cut.Find("#email").Change("ana@example.com");
        cut.Find("#password").Change("supersecret");
        cut.Find("#confirmPassword").Change("supersecret");
        cut.Find("form").Submit();

        Assert.Equal(1, registration.CallCount);
        Assert.Equal("Ana Tester", registration.LastName);
        Assert.Equal("ana@example.com", registration.LastEmail);
        Assert.Equal("supersecret", registration.LastPassword);
    }

    [Fact]
    public void Mismatched_passwords_block_submit()
    {
        var registration = new SpyRegistrationService();
        Services.AddSingleton<IUserRegistrationService>(registration);

        var cut = RenderComponent<Register>();

        cut.Find("#name").Change("Ana Tester");
        cut.Find("#email").Change("ana@example.com");
        cut.Find("#password").Change("supersecret");
        cut.Find("#confirmPassword").Change("does-not-match");
        cut.Find("form").Submit();

        Assert.Equal(0, registration.CallCount);
        Assert.Contains("Passwords do not match.", cut.Markup);
    }

    private sealed class SpyRegistrationService : IUserRegistrationService
    {
        public int CallCount { get; private set; }
        public string? LastName { get; private set; }
        public string? LastEmail { get; private set; }
        public string? LastPassword { get; private set; }

        public Task<User> RegisterAsync(string name, string email, string password, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastName = name;
            LastEmail = email;
            LastPassword = password;
            return Task.FromResult(new User { Id = Guid.NewGuid(), Name = name, Email = email, PasswordHash = "hash" });
        }
    }

    // Returns the key as its own value so assertions read against stable strings.
    private sealed class PassthroughLocalizer : IStringLocalizer<AppStrings>
    {
        public LocalizedString this[string name] => new(name, name);
        public LocalizedString this[string name, params object[] arguments] => new(name, name);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }
}
