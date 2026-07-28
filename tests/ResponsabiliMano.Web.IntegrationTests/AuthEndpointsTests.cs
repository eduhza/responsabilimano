using System.Net;
using System.Net.Http.Json;
using ResponsabiliMano.Web.Services;

namespace ResponsabiliMano.Web.IntegrationTests;

[Collection("Integration")]
public class AuthEndpointsTests : IAsyncLifetime
{
    private readonly IntegrationFixture _fixture;
    public AuthEndpointsTests(IntegrationFixture fixture) => _fixture = fixture;
    public async Task InitializeAsync() => await _fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task RegisterAndLogin_Success_ReturnsRedirectWithCookie()
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Name"] = "Alice", ["Email"] = "alice@example.com",
            ["Password"] = "Password123!", ["ConfirmPassword"] = "Password123!"
        });
        var response = await _fixture.Client.PostAsync("/api/auth/register-and-login", form);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith("ResponsabiliMano.Auth="));
    }

    [Fact]
    public async Task RegisterAndLogin_DuplicateEmail_ReturnsConflictRedirect()
    {
        var form1 = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Name"] = "Charlie", ["Email"] = "charlie@example.com",
            ["Password"] = "Password123!", ["ConfirmPassword"] = "Password123!"
        });
        await _fixture.Client.PostAsync("/api/auth/register-and-login", form1);

        var form2 = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Name"] = "Charlie2", ["Email"] = "charlie@example.com",
            ["Password"] = "Password123!", ["ConfirmPassword"] = "Password123!"
        });
        var resp2 = await _fixture.Client.PostAsync("/api/auth/register-and-login", form2);
        Assert.Equal(HttpStatusCode.Redirect, resp2.StatusCode);
        Assert.Contains("error=Conflict", resp2.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Login_Success_ReturnsRedirectWithCookie()
    {
        var regForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Name"] = "Dave", ["Email"] = "dave@example.com",
            ["Password"] = "Password123!", ["ConfirmPassword"] = "Password123!"
        });
        await _fixture.Client.PostAsync("/api/auth/register-and-login", regForm);

        var loginForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "dave@example.com", ["Password"] = "Password123!"
        });
        var response = await _fixture.Client.PostAsync("/api/auth/login", loginForm);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith("ResponsabiliMano.Auth="));
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsErrorRedirect()
    {
        var loginForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "nobody@example.com", ["Password"] = "wrongpass"
        });
        var response = await _fixture.Client.PostAsync("/api/auth/login", loginForm);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("error=InvalidCredentials", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task ForgotPassword_RegisteredEmail_ReturnsOk()
    {
        var regForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Name"] = "Eve", ["Email"] = "eve@example.com",
            ["Password"] = "Password123!", ["ConfirmPassword"] = "Password123!"
        });
        await _fixture.Client.PostAsync("/api/auth/register-and-login", regForm);

        var response = await _fixture.Client.PostAsJsonAsync("/api/auth/forgot-password", new { Email = "eve@example.com" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEmpty(_fixture.Emails.GetEmails());
    }

    [Fact]
    public async Task ForgotPassword_NonExistentEmail_ReturnsOkNoLeak()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/auth/forgot-password", new { Email = "nonexistent@example.com" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(_fixture.Emails.GetEmails());
    }

    [Fact]
    public async Task ForgotPassword_InvalidEmail_ReturnsValidationProblem()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/auth/forgot-password", new { Email = "not-an-email" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_ValidToken_ReturnsOk()
    {
        var regForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Name"] = "Frank", ["Email"] = "frank@example.com",
            ["Password"] = "Password123!", ["ConfirmPassword"] = "Password123!"
        });
        await _fixture.Client.PostAsync("/api/auth/register-and-login", regForm);
        await _fixture.Client.PostAsJsonAsync("/api/auth/forgot-password", new { Email = "frank@example.com" });

        var email = Assert.Single(_fixture.Emails.GetEmails());
        var tokenMatch = System.Text.RegularExpressions.Regex.Match(email.HtmlBody, @"token=([^""]+)");
        Assert.True(tokenMatch.Success);
        var token = tokenMatch.Groups[1].Value;

        var response = await _fixture.Client.PostAsJsonAsync("/api/auth/reset-password",
            new { Token = token, Password = "NewPass123!", ConfirmPassword = "NewPass123!" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_ReturnsBadRequest()
    {
        var response = await _fixture.Client.PostAsJsonAsync("/api/auth/reset-password",
            new { Token = "invalid-token", Password = "NewPass123!", ConfirmPassword = "NewPass123!" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}