using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ResponsabiliMano.Web.Components.Pages;

namespace ResponsabiliMano.Web.Tests;

// Regression guard for spec X1 / ADR-0003. bUnit always renders components
// interactively, so a bUnit test alone cannot detect a missing @rendermode. This
// asserts, by reflection, that the pages with C# interactivity actually carry the
// Interactive Server render mode (the invariant that makes their forms/bind work in
// the browser) — and, conversely, that the static pages stay static (so Login/Logout
// keep working over plain HTTP form posts and the auth cookie).
public class RenderModeTests
{
    // Every page with EditForm / @onclick / @bind must be interactive.
    public static TheoryData<Type> InteractivePages() =>
    [
        typeof(Register),
        typeof(CreateProject),
        typeof(InvitePartner),
        typeof(ForgotPassword),
        typeof(ResetPassword),
        typeof(ProjectDetail),
        typeof(InvitationAccept),
        typeof(CheckIn),
    ];

    // Pages that must remain static SSR (see ADR-0003).
    public static TheoryData<Type> StaticPages() =>
    [
        typeof(Login),
        typeof(Home),
    ];

    [Theory]
    [MemberData(nameof(InteractivePages))]
    public void Interactive_pages_declare_interactive_server_render_mode(Type pageType)
    {
        var renderMode = GetRenderMode(pageType);

        Assert.NotNull(renderMode);
        Assert.IsType<InteractiveServerRenderMode>(renderMode!.Mode);
    }

    [Theory]
    [MemberData(nameof(StaticPages))]
    public void Static_pages_declare_no_render_mode(Type pageType)
    {
        Assert.Null(GetRenderMode(pageType));
    }

    private static RenderModeAttribute? GetRenderMode(Type pageType) =>
        pageType.GetCustomAttributes(inherit: false)
            .OfType<RenderModeAttribute>()
            .SingleOrDefault();
}
