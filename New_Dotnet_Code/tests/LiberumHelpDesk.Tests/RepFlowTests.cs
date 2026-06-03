using System.Net;
using Dapper;

namespace LiberumHelpDesk.Tests;

// Each test gets its own app + DB (the close test mutates shared state, so no class fixture).
public class RepFlowTests
{
    private static FormUrlEncodedContent Form(params (string, string)[] fields) =>
        new(fields.Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));

    private static async Task<HttpClient> LoggedInClientWithProblem(HelpdeskWebApp app)
    {
        var client = app.NewClient();
        using (var db = app.OpenDb())
        {
            db.Execute("INSERT OR IGNORE INTO categories (category_id, cname, rep_id) VALUES (1, 'General', 1)");
            db.Execute(
                "INSERT OR IGNORE INTO problems (id, uid, uemail, uphone, ulocation, rep, status, time_spent, category, " +
                "priority, department, title, description, solution, start_date, due_date, entered_by, kb) VALUES " +
                "(1, 'admin', 'admin@localhost', '', '', 1, 1, 0, 1, 1, 1, 'Printer jam', 'It is jammed', '', " +
                "'2026-01-01 09:00:00', '2026-01-02 09:00:00', 1, 0)");
        }
        var login = await client.PostAsync("/Logon?URL=default.asp",
            Form(("logon", "1"), ("uid", "admin"), ("password", "admin")));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        return client;
    }

    [Fact]
    public async Task Rep_menu_and_open_list_render()
    {
        using var app = new HelpdeskWebApp();
        var client = await LoggedInClientWithProblem(app);

        var menu = await client.GetAsync("/Rep");
        menu.EnsureSuccessStatusCode();
        Assert.Contains("Submit New Problem", await menu.Content.ReadAsStringAsync());

        var list = await client.GetAsync("/Rep/Problem/View");
        list.EnsureSuccessStatusCode();
        var listBody = await list.Content.ReadAsStringAsync();
        Assert.Contains("Open Problems", listBody);
        Assert.Contains("Printer jam", listBody);
    }

    [Fact]
    public async Task Rep_can_open_the_edit_form()
    {
        using var app = new HelpdeskWebApp();
        var client = await LoggedInClientWithProblem(app);

        var details = await client.GetAsync("/Rep/Problem/Details?id=1");
        details.EnsureSuccessStatusCode();
        var body = await details.Content.ReadAsStringAsync();
        Assert.Contains("Edit Problem", body);
        Assert.Contains("Printer jam", body);
        Assert.Contains("Save Problem", body);
    }

    [Fact]
    public async Task Rep_closing_a_ticket_sets_close_date_logs_status_change_and_emails_user()
    {
        using var app = new HelpdeskWebApp();
        var client = await LoggedInClientWithProblem(app);
        app.Emails.Sent.Clear();

        var close = await client.PostAsync("/Rep/Problem/Details", Form(
            ("update", "1"), ("id", "1"), ("uid", "admin"), ("uemail", "admin@localhost"),
            ("uphone", ""), ("ulocation", ""), ("category", "1"), ("department", "1"),
            ("title", "Printer jam"), ("priority", "1"), ("status", "100"), ("rep", "1"), ("oldrep", "1"),
            ("time_spent", "15"), ("solution", "Cleared the jam"), ("notes", ""), ("duedate", "2027-01-01")));
        close.EnsureSuccessStatusCode();
        Assert.Contains("The problem has been saved", await close.Content.ReadAsStringAsync());

        Assert.Single(app.Emails.Sent);
        Assert.Contains(app.Emails.Sent, e => e.Subject.Contains("Closed"));

        using var db = app.OpenDb();
        Assert.Equal(100, db.ExecuteScalar<long>("SELECT status FROM problems WHERE id = 1"));
        Assert.Equal("Cleared the jam", db.ExecuteScalar<string>("SELECT solution FROM problems WHERE id = 1"));
        Assert.False(string.IsNullOrEmpty(db.ExecuteScalar<string?>("SELECT close_date FROM problems WHERE id = 1")));
        Assert.Equal(1L, db.ExecuteScalar<long>("SELECT emailsent FROM problems WHERE id = 1"));

        var changeNote = db.ExecuteScalar<string?>(
            "SELECT [note] FROM tblNotes WHERE id = 1 AND private = 1 AND [note] LIKE '%OPEN => CLOSED%'");
        Assert.False(string.IsNullOrEmpty(changeNote));
        Assert.False(string.IsNullOrEmpty(db.ExecuteScalar<string?>("SELECT first_response FROM problems WHERE id = 1")));
    }
}
