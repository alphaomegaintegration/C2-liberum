using System.Net;
using Dapper;

namespace LiberumHelpDesk.Tests;

/// <summary>
/// Locks in the three divergences found by the live browser parity sweep (oracle vs .NET) that the
/// DOM-text and attribute harnesses structurally could not see:
///   1. user/print.asp "Assigned To" rep name is a mailto link (was rendered as plain text).
///   2. viewstatus.asp CloseStatus marker renders tight "100*" (ASP &lt;%= %&gt; whitespace quirk),
///      not "100 *" (a Razor markup block would emit the leading space).
///   3. viewlangstring.asp rows sort case-INSENSITIVELY (Access/SQL Server collation), so SQLite
///      must use COLLATE NOCASE instead of its default case-sensitive BINARY order.
/// </summary>
public class ParityFixesTests : IClassFixture<HelpdeskWebApp>
{
    private readonly HelpdeskWebApp _app;
    public ParityFixesTests(HelpdeskWebApp app) => _app = app;

    private static FormUrlEncodedContent Form(params (string, string)[] fields) =>
        new(fields.Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));

    private async Task<HttpClient> RepClient()
    {
        var client = _app.NewClient();
        var login = await client.PostAsync("/Logon?URL=default.asp",
            Form(("logon", "1"), ("uid", "admin"), ("password", "admin")));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        return client;
    }

    private async Task<HttpClient> AdminClient()
    {
        var client = _app.NewClient();
        var gate = await client.PostAsync("/Admin", Form(("password", "admin")));
        Assert.Contains("Administrative Menu", await gate.Content.ReadAsStringAsync());
        return client;
    }

    // ---- 1: user/print AssignedTo is a mailto link to the rep, with the whitespace-quirk subject ----
    [Fact]
    public async Task User_print_assignedTo_is_a_mailto_link_to_the_rep()
    {
        var client = await RepClient();
        using (var db = _app.OpenDb())
        {
            db.Execute("INSERT OR IGNORE INTO categories (category_id, cname, rep_id) VALUES (1, 'General', 1)");
            db.Execute(
                "INSERT OR IGNORE INTO problems (id, uid, uemail, rep, status, time_spent, category, priority, department, " +
                "title, description, solution, start_date, close_date, due_date, entered_by, kb) VALUES " +
                "(9, 'admin', 'admin@localhost', 1, 1, 0, 1, 1, 1, 'Linkable', 'desc', '', '2026-01-01 09:00:00', NULL, " +
                "'2026-01-08 17:00:00', 1, 0)");
        }
        var resp = await client.GetAsync("/User/Problem/Print?id=9");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();

        // rep sid=1 → email1 admin@localhost, fname Administrator; subject "Help Desk: Problem9" (no space
        // before the id — the ASP space between <%=lang("Problem")%> and <%=id%> is consumed).
        Assert.Contains("mailto:admin@localhost?Subject=Help Desk: Problem9\">Administrator</a>", body);
        Assert.DoesNotContain("Problem 9\">Administrator", body); // would mean the whitespace quirk wasn't reproduced
    }

    // ---- 2: viewstatus CloseStatus marker is tight against the id, no leading space ----
    [Fact]
    public async Task ViewStatus_close_marker_has_no_space_before_the_asterisk()
    {
        var client = await AdminClient();
        var resp = await client.GetAsync("/Admin/ViewStatus");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();

        Assert.Contains("100<em>*</em>", body);     // tight, matching the oracle
        Assert.DoesNotContain("100 <em>*</em>", body); // the Razor-markup-block leading-space regression
    }

    // ---- 3: viewlangstring rows sort case-insensitively (COLLATE NOCASE), like Access / SQL Server ----
    [Fact]
    public async Task ViewLangString_orders_variables_case_insensitively()
    {
        var client = await AdminClient();
        var resp = await client.GetAsync("/Admin/ViewLangString?lang_id=1");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();

        // Distinctive variable names (their values contain spaces, so these no-space tokens are the cells).
        var access = body.IndexOf("AccessDenied", StringComparison.Ordinal);
        var asql = body.IndexOf("ASQLqueryhasfailed", StringComparison.Ordinal);
        Assert.True(access >= 0 && asql >= 0, "both lang variables must be present");
        // Case-insensitive: "asql..." sorts after "access..."; SQLite's default BINARY would put the
        // uppercase-S "ASQL..." first (asql < access), so this ordering proves the NOCASE collation.
        Assert.True(access < asql, "AccessDenied must precede ASQLqueryhasfailed (case-insensitive order)");
    }
}
