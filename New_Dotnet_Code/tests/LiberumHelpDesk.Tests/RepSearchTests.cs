using System.Net;
using Dapper;

namespace LiberumHelpDesk.Tests;

public class RepSearchTests : IClassFixture<HelpdeskWebApp>
{
    private readonly HelpdeskWebApp _app;
    public RepSearchTests(HelpdeskWebApp app) => _app = app;

    private static FormUrlEncodedContent Form(params (string, string)[] fields) =>
        new(fields.Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));

    private async Task<HttpClient> LoggedInWithTicket()
    {
        var client = _app.NewClient();
        using (var db = _app.OpenDb())
        {
            db.Execute("INSERT OR IGNORE INTO categories (category_id, cname, rep_id) VALUES (1, 'General', 1)");
            db.Execute(
                "INSERT OR IGNORE INTO problems (id, uid, uemail, rep, status, time_spent, category, priority, department, " +
                "title, description, solution, start_date, due_date, entered_by, kb) VALUES " +
                "(7, 'admin', 'admin@localhost', 1, 1, 0, 1, 1, 1, 'Search test ticket', 'find me', '', " +
                "'2026-03-15 10:00:00', '2026-03-20 09:00:00', 1, 0)");
        }
        var login = await client.PostAsync("/Logon?URL=default.asp",
            Form(("logon", "1"), ("uid", "admin"), ("password", "admin")));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        return client;
    }

    [Fact]
    public async Task Search_form_renders()
    {
        var client = await LoggedInWithTicket();
        var resp = await client.GetAsync("/Rep/Search");
        resp.EnsureSuccessStatusCode();
        Assert.Contains("Problem Search", await resp.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Search_by_date_range_finds_the_ticket()
    {
        var client = await LoggedInWithTicket();
        var resp = await client.PostAsync("/Rep/Results", Form(
            ("uid", ""), ("id", ""), ("rep", "0"), ("category", "0"), ("department", "0"),
            ("status", "0"), ("priority", "0"), ("order", "1"),
            ("keywords", ""), ("title", "on"), ("description", "on"), ("solution", "on"),
            ("s_month", "1"), ("s_day", "1"), ("s_year", "2026"),
            ("e_month", "12"), ("e_day", "31"), ("e_year", "2026")));
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Search Results", body);
        Assert.Contains("Search test ticket", body);
    }

    [Fact]
    public async Task Search_by_keyword_filters()
    {
        var client = await LoggedInWithTicket();
        // A keyword that doesn't appear in the ticket returns nothing.
        var resp = await client.PostAsync("/Rep/Results", Form(
            ("rep", "0"), ("category", "0"), ("department", "0"), ("status", "0"), ("priority", "0"), ("order", "1"),
            ("keywords", "nonexistentword"), ("title", "on"), ("description", "on"), ("solution", "on"),
            ("s_month", "1"), ("s_day", "1"), ("s_year", "2026"),
            ("e_month", "12"), ("e_day", "31"), ("e_year", "2026")));
        Assert.Contains("No results found", await resp.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task SelectUser_popup_finds_a_user_by_prefix()
    {
        var client = await LoggedInWithTicket();
        var resp = await client.PostAsync("/Rep/SelectUser", Form(("postform", "1"), ("searchname", "admin")));
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("admin", body);
        Assert.Contains("updateParent", body); // the picker JS is present
    }
}
