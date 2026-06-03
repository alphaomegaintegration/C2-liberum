using System.Net;
using Dapper;

namespace LiberumHelpDesk.Tests;

public class PrintTests : IClassFixture<HelpdeskWebApp>
{
    private readonly HelpdeskWebApp _app;
    public PrintTests(HelpdeskWebApp app) => _app = app;

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
                "(9, 'admin', 'admin@localhost', 1, 100, 0, 1, 1, 1, 'Printable', 'line one" + "\n" +
                "line two [bracketed]', 'the fix', '2026-01-01 09:00:00', '2026-01-02 17:00:00', '2026-01-02 09:00:00', 1, 0)");
        }
        var login = await client.PostAsync("/Logon?URL=default.asp",
            Form(("logon", "1"), ("uid", "admin"), ("password", "admin")));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        return client;
    }

    [Fact]
    public async Task User_print_shows_details_solution_and_bolds_brackets()
    {
        var client = await LoggedIn();
        var resp = await client.GetAsync("/User/Problem/Print?id=9");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Printable", body);
        Assert.Contains("the fix", body);              // solution shown (closed)
        Assert.Contains("Close This Window", body);
        Assert.Contains("<b>[bracketed]</b>", body);   // FormatBlock bolds bracketed sections
    }

    [Fact]
    public async Task Rep_print_shows_priority_field()
    {
        var client = await LoggedIn();
        var resp = await client.GetAsync("/Rep/Problem/Print?id=9");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Printable", body);
        Assert.Contains("Priority", body); // rep print includes the priority row
    }
}
