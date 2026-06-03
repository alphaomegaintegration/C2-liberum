using System.Net;
using Dapper;

namespace LiberumHelpDesk.Tests;

public class KbAndForgotPassTests : IClassFixture<HelpdeskWebApp>
{
    private readonly HelpdeskWebApp _app;
    public KbAndForgotPassTests(HelpdeskWebApp app) => _app = app;

    private static FormUrlEncodedContent Form(params (string, string)[] fields) =>
        new(fields.Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));

    private async Task<HttpClient> LoggedIn()
    {
        var client = _app.NewClient();
        using (var db = _app.OpenDb())
        {
            db.Execute("INSERT OR IGNORE INTO categories (category_id, cname, rep_id) VALUES (1, 'General', 1)");
            db.Execute(
                "INSERT OR IGNORE INTO problems (id, uid, uemail, rep, status, time_spent, category, priority, department, " +
                "title, description, solution, start_date, close_date, due_date, entered_by, kb) VALUES " +
                "(5, 'admin', 'admin@localhost', 1, 100, 0, 1, 1, 1, 'Network outage resolved', " +
                "'the network was down', 'rebooted the switch', '2026-01-01 09:00:00', '2026-01-03 17:00:00', " +
                "'2026-01-02 09:00:00', 1, 1)");
        }
        var login = await client.PostAsync("/Logon?URL=default.asp",
            Form(("logon", "1"), ("uid", "admin"), ("password", "admin")));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        return client;
    }

    [Fact]
    public async Task Kb_search_finds_a_closed_kb_article_and_details_render()
    {
        var client = await LoggedIn();

        var search = await client.PostAsync("/Kb", Form(
            ("search", "1"), ("keywords", "network"), ("title", "on"), ("description", "on"), ("solution", "on")));
        search.EnsureSuccessStatusCode();
        var searchBody = await search.Content.ReadAsStringAsync();
        Assert.Contains("Network outage resolved", searchBody);
        Assert.Contains("/Kb/Details?id=5", searchBody);

        var details = await client.GetAsync("/Kb/Details?id=5");
        details.EnsureSuccessStatusCode();
        var detailsBody = await details.Content.ReadAsStringAsync();
        Assert.Contains("Network outage resolved", detailsBody);
        Assert.Contains("rebooted the switch", detailsBody);  // solution shown
    }

    [Fact]
    public async Task Kb_search_with_no_match_reports_no_results()
    {
        var client = await LoggedIn();
        var search = await client.PostAsync("/Kb", Form(
            ("search", "1"), ("keywords", "zzzznomatch"), ("title", "on"), ("description", "on"), ("solution", "on")));
        Assert.Contains("No results found", await search.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ForgotPass_emails_the_password_for_a_known_user()
    {
        var client = _app.NewClient();
        _app.Emails.Sent.Clear();

        var resp = await client.PostAsync("/ForgotPass", Form(("email", "1"), ("uid", "admin")));
        resp.EnsureSuccessStatusCode();
        Assert.Contains("Password Sent", await resp.Content.ReadAsStringAsync());

        Assert.Single(_app.Emails.Sent);
        Assert.Contains(_app.Emails.Sent, e => e.Body.Contains("Password: admin"));
    }

    [Fact]
    public async Task ForgotPass_reports_invalid_username()
    {
        var client = _app.NewClient();
        _app.Emails.Sent.Clear();

        var resp = await client.PostAsync("/ForgotPass", Form(("email", "1"), ("uid", "nobody")));
        Assert.Contains("Invalid username", await resp.Content.ReadAsStringAsync());
        Assert.Empty(_app.Emails.Sent);
    }
}
