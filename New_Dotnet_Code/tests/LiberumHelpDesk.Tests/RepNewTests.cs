using System.Net;
using Dapper;

namespace LiberumHelpDesk.Tests;

public class RepNewTests
{
    private static FormUrlEncodedContent Form(params (string, string)[] fields) =>
        new(fields.Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));

    [Fact]
    public async Task Rep_creates_an_open_ticket_on_behalf_of_a_user()
    {
        using var app = new HelpdeskWebApp();
        var client = app.NewClient();
        using (var db = app.OpenDb())
            db.Execute("INSERT OR IGNORE INTO categories (category_id, cname, rep_id) VALUES (1, 'General', 1)");

        var login = await client.PostAsync("/Logon?URL=default.asp",
            Form(("logon", "1"), ("uid", "admin"), ("password", "admin")));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);

        app.Emails.Sent.Clear();
        var resp = await client.PostAsync("/Rep/Problem/New", Form(
            ("save", "1"), ("uselectid", "0"), ("uid", "jane"), ("uemail", "jane@x.com"),
            ("uphone", "555"), ("ulocation", "Floor 2"), ("department", "1"), ("category", "1"),
            ("status", "1"), ("priority", "1"), ("rep", "1"), ("time_spent", "5"), ("solution", ""),
            ("duedate", "2027-01-01"), ("title", "Laptop wont boot"), ("description", "It is dead")));
        resp.EnsureSuccessStatusCode();
        Assert.Contains("has been entered", await resp.Content.ReadAsStringAsync());

        using (var db = app.OpenDb())
        {
            Assert.Equal(1L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM problems WHERE title = 'Laptop wont boot'"));
            Assert.Equal("jane", db.ExecuteScalar<string>("SELECT uid FROM problems WHERE title = 'Laptop wont boot'"));
            Assert.Equal(1L, db.ExecuteScalar<long>("SELECT rep FROM problems WHERE title = 'Laptop wont boot'"));
        }

        // usernew + repnew (open ticket).
        Assert.Equal(2, app.Emails.Sent.Count);
    }

    [Fact]
    public async Task Rep_new_form_renders()
    {
        using var app = new HelpdeskWebApp();
        var client = app.NewClient();
        var login = await client.PostAsync("/Logon?URL=default.asp",
            Form(("logon", "1"), ("uid", "admin"), ("password", "admin")));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);

        var resp = await client.GetAsync("/Rep/Problem/New");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("Submit A New Problem", body);
        Assert.Contains("newProbForm", body);
    }
}
