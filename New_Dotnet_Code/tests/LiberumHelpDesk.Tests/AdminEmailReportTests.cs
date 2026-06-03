using Dapper;

namespace LiberumHelpDesk.Tests;

public class AdminEmailReportTests
{
    private static FormUrlEncodedContent Form(params (string, string)[] fields) =>
        new(fields.Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));

    private static async Task<HttpClient> AdminClient(HelpdeskWebApp app)
    {
        var client = app.NewClient();
        var gate = await client.PostAsync("/Admin", Form(("password", "admin")));
        Assert.Contains("Administrative Menu", await gate.Content.ReadAsStringAsync());
        return client;
    }

    [Fact]
    public async Task CfgEmail_edit_loads_and_saves_a_template()
    {
        using var app = new HelpdeskWebApp();
        var client = await AdminClient(app);

        // Choosing a type loads its current template.
        var edit = await client.PostAsync("/Admin/CfgEmail", Form(("type", "usernew")));
        edit.EnsureSuccessStatusCode();
        Assert.Contains("HELPDESK: Problem [problemid] Created", await edit.Content.ReadAsStringAsync());

        // Saving updates tblEmailMsg.
        var save = await client.PostAsync("/Admin/CfgEmail",
            Form(("save", "1"), ("type", "usernew"), ("subject", "Ticket [problemid] opened"), ("body", "Hello [uid]")));
        Assert.Contains("Message Saved", await save.Content.ReadAsStringAsync());

        using var db = app.OpenDb();
        Assert.Equal("Ticket [problemid] opened", db.ExecuteScalar<string>("SELECT subject FROM tblEmailMsg WHERE type='usernew'"));
        Assert.Equal("Hello [uid]", db.ExecuteScalar<string>("SELECT body FROM tblEmailMsg WHERE type='usernew'"));
    }

    [Fact]
    public async Task Department_report_groups_and_totals()
    {
        using var app = new HelpdeskWebApp();
        var client = await AdminClient(app);
        using (var db = app.OpenDb())
        {
            db.Execute("INSERT OR IGNORE INTO categories (category_id, cname, rep_id) VALUES (1, 'General', 1)");
            db.Execute(
                "INSERT OR IGNORE INTO problems (id, uid, uemail, rep, status, time_spent, category, priority, department, " +
                "title, description, start_date, due_date, entered_by, kb) VALUES " +
                "(11, 'admin', 'a@b.com', 1, 1, 10, 1, 1, 1, 'R', 'd', '2026-04-10 09:00:00', '2026-04-11 09:00:00', 1, 0)");
        }

        var report = await client.PostAsync("/Admin/ViewReports", Form(
            ("type", "0"), ("s_month", "1"), ("s_day", "1"), ("s_year", "2026"),
            ("e_month", "12"), ("e_day", "31"), ("e_year", "2026")));
        report.EnsureSuccessStatusCode();
        var body = await report.Content.ReadAsStringAsync();
        Assert.Contains("Dept1", body);   // department name (seeded department_id 1)
        Assert.Contains("Totals", body);
    }
}
