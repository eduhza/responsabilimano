using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using ResponsabiliMano.Web;
using ResponsabiliMano.Web.Components.Pages;

namespace ResponsabiliMano.Web.Tests.Pages;

// Behavior guard for the refactored Register page: it is a static SSR plain HTML
// form posted to the combined register-and-login endpoint. No Blazor interactivity
// or IUserRegistrationService is used on this page anymore.
public class RegisterTests : TestContext
{
    public RegisterTests()
    {
        Services.AddSingleton<IStringLocalizer<AppStrings>>(new PassthroughLocalizer());
    }

    [Fact]
    public void Renders_plain_html_form_posting_to_register_and_login_endpoint()
    {
        var cut = RenderComponent<Register>();

        var form = cut.Find("form");
        Assert.Equal("post", form.GetAttribute("method"));
        Assert.Equal("/api/auth/register-and-login", form.GetAttribute("action"));

        Assert.NotNull(cut.Find("input#name[name='Name']"));
        Assert.NotNull(cut.Find("input#email[name='Email']"));
        Assert.NotNull(cut.Find("input#password[name='Password']"));
        Assert.NotNull(cut.Find("input#confirmPassword[name='ConfirmPassword']"));
        Assert.Equal("RegisterButton", cut.Find("button[type='submit']").TextContent.Trim());
    }

    // Returns the key as its own value so assertions read against stable strings.
    private sealed class PassthroughLocalizer : IStringLocalizer<AppStrings>
    {
        public LocalizedString this[string name] => new(name, name);
        public LocalizedString this[string name, params object[] arguments] => new(name, name);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }
}
