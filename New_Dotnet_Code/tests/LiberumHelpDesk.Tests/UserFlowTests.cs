using System.Net;
using Dapper;

namespace LiberumHelpDesk.Tests;

public class UserFlowTests : IClassFixture<HelpdeskWebApp>
{
    private readonly HelpdeskWebApp _app;
    public UserFlowTests(HelpdeskWebApp app) => _app = app;

    private static FormUrlEncodedContent Form(params (string, string)[] fields) =>
        new(fields.Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));

    [Fact]
    public async Task Full_user_ticket_lifecycle()
    {
        var client = _app.NewClient();

        // Trigger startup seeding, then add a category (the stock seed ships none) pointing at the admin rep.
        using (var db = _app.OpenDb())
        {
            db.Execute("INSERT OR IGNORE INTO categories (category_id, cname, rep_id) VALUES (1, 'General', 1)");
        }

        // 1. Log in (DB auth) as the seeded sample rep admin/admin.
        var login = await client.PostAsync("/Logon?URL=default.asp",
            Form(("logon", "1"), ("uid", "admin"), ("password", "admin")));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/", login.Headers.Location!.OriginalString);

        // 2. The user menu renders.
        var menu = await client.GetAsync("/User");
        menu.EnsureSuccessStatusCode();
        Assert.Contains("Submit New Problem", await menu.Content.ReadAsStringAsync());

        // 3. Submit a new problem.
        _app.Emails.Sent.Clear();
        var post = await client.PostAsync("/User/Problem/PostNew", Form(
            ("uid", "admin"), ("uemail", "admin@localhost"), ("uphone", "555-1212"), ("ulocation", "HQ"),
            ("department", "1"), ("category", "1"), ("priority", "2"),
            ("duedate", "2027-01-01"), ("title", "Printer is broken"),
            ("description", "The third floor printer will not print.")));
        post.EnsureSuccessStatusCode();
        var postBody = await post.Content.ReadAsStringAsync();
        Assert.Contains("Printer is broken", postBody);
        Assert.Contains("Submitted", postBody);

        // usernew + repnew were sent (no pager: rep has no email2).
        Assert.Equal(2, _app.Emails.Sent.Count);
        Assert.Contains(_app.Emails.Sent, e => e.Subject.Contains("Created"));   // usernew
        Assert.Contains(_app.Emails.Sent, e => e.Subject.Contains("Assigned")); // repnew
        Assert.Contains(_app.Emails.Sent, e => e.Body.Contains("The third floor printer will not print."));

        // The problem got id=1 (db_keys.problems started at 1).
        int problemId;
        using (var db = _app.OpenDb())
            problemId = (int)db.ExecuteScalar<long>("SELECT id FROM problems WHERE title = 'Printer is broken'");
        Assert.Equal(1, problemId);

        // 4. Details renders the problem.
        var details = await client.GetAsync($"/User/Problem/Details?id={problemId}");
        details.EnsureSuccessStatusCode();
        var detailsBody = await details.Content.ReadAsStringAsync();
        Assert.Contains("Printer is broken", detailsBody);
        Assert.Contains("The third floor printer will not print.", detailsBody);

        // 5. Add a note (update).
        _app.Emails.Sent.Clear();
        var update = await client.PostAsync("/User/Problem/Update",
            Form(("id", problemId.ToString()), ("notes", "Any update on this?")));
        update.EnsureSuccessStatusCode();
        Assert.Contains("Updated", await update.Content.ReadAsStringAsync());
        Assert.Single(_app.Emails.Sent); // repupdate
        Assert.Contains(_app.Emails.Sent, e => e.Subject.Contains("Updated"));

        // 6. The note now shows on the details page.
        var details2 = await client.GetAsync($"/User/Problem/Details?id={problemId}");
        Assert.Contains("Any update on this?", await details2.Content.ReadAsStringAsync());

        // 7. The problem list shows the ticket.
        var list = await client.GetAsync("/User/Problem/View");
        list.EnsureSuccessStatusCode();
        var listBody = await list.Content.ReadAsStringAsync();
        Assert.Contains("Printer is broken", listBody);
        Assert.DoesNotContain("No results found", listBody);
    }

    [Fact]
    public async Task Unauthenticated_user_is_redirected_to_logon()
    {
        var client = _app.NewClient();
        var resp = await client.GetAsync("/User");
        Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);
        Assert.StartsWith("/Logon", resp.Headers.Location!.OriginalString);
    }
}
