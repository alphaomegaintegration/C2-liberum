using System.Net;

namespace LiberumHelpDesk.Tests;

public class RegisterFlowTests : IClassFixture<HelpdeskWebApp>
{
    private readonly HelpdeskWebApp _app;
    public RegisterFlowTests(HelpdeskWebApp app) => _app = app;

    private static FormUrlEncodedContent Form(params (string, string)[] fields) =>
        new(fields.Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));

    [Fact]
    public async Task Register_new_user_then_log_in()
    {
        var client = _app.NewClient();

        // Register a brand-new account (AuthType=2 => password required).
        var reg = await client.PostAsync("/Register", Form(
            ("create", "1"), ("uid", "newuser1"),
            ("firstname", "New"), ("lastname", "User"), ("email", "newuser1@localhost"),
            ("pager", ""), ("phone", ""), ("phone_home", ""), ("phone_mobile", ""), ("location", ""),
            ("department", "0"), ("usrLanguage", "1"), ("dateformat", "yyyy-mm-dd"),
            ("oldpassword", ""), ("password1", "secret"), ("password2", "secret")));
        reg.EnsureSuccessStatusCode();
        Assert.Contains("Account Created", await reg.Content.ReadAsStringAsync());

        // The new account can authenticate and is routed to the user menu (not a rep).
        var login = await client.PostAsync("/Logon?URL=default.asp",
            Form(("logon", "1"), ("uid", "newuser1"), ("password", "secret")));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/", login.Headers.Location!.OriginalString);

        var landing = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, landing.StatusCode);
        Assert.Equal("/User", landing.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Register_rejects_mismatched_passwords()
    {
        var client = _app.NewClient();
        var reg = await client.PostAsync("/Register", Form(
            ("create", "1"), ("uid", "mismatch"),
            ("firstname", "A"), ("lastname", "B"), ("email", "a@b.com"),
            ("pager", ""), ("phone", ""), ("phone_home", ""), ("phone_mobile", ""), ("location", ""),
            ("department", "0"), ("usrLanguage", "1"), ("dateformat", "yyyy-mm-dd"),
            ("oldpassword", ""), ("password1", "one"), ("password2", "two")));
        Assert.Contains("Passwords do not match", await reg.Content.ReadAsStringAsync());
    }
}
