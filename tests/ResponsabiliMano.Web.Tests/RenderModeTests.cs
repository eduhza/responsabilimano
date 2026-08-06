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
        typeof(CreateProject),
        typeof(InvitePartner),
        typeof(ForgotPassword),
        typeof(ResetPassword),
        typeof(ProjectDetail),
        typeof(InvitationAccept),
        typeof(CheckIn),
        typeof(Dashboard),
        typeof(GlobalDashboard),
        // Home became interactive (spec RT1) so it re-queries on navigation and can
        // receive live updates (spec RT2). It only reads auth state — never sets the
        // cookie — so ADR-0003's static-page constraint does not apply to it.
        typeof(Home),
        typeof(Profile),
    ];

    // Pages that must remain static SSR (see ADR-0003): they POST the auth cookie over
    // plain HTTP and cannot run over a SignalR circuit.
    public static TheoryData<Type> StaticPages() =>
    [
        typeof(Register),
        typeof(Login),
        // The public landing page has no interactivity and is the first thing an
        // anonymous visitor loads, so it stays static too.
        typeof(Landing),
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
