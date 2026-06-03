using System.Net;
using Dapper;
using LiberumHelpDesk.Web.Services;

namespace LiberumHelpDesk.Tests;

/// <summary>Locks in the behavioural fixes from the fidelity audit (A1–A4, B-series).</summary>
public class FidelityFixesTests
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

    private static async Task<HttpClient> RepClient(HelpdeskWebApp app)
    {
        var client = app.NewClient();
        var login = await client.PostAsync("/Logon?URL=default.asp",
            Form(("logon", "1"), ("uid", "admin"), ("password", "admin")));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        return client;
    }

    // ---- A1: a malformed due date is rejected, not silently coerced to tomorrow ----
    [Fact]
    public async Task PostNew_rejects_an_unparseable_due_date_instead_of_storing_tomorrow()
    {
        using var app = new HelpdeskWebApp();
        var client = await RepClient(app);
        using (var db = app.OpenDb())
            db.Execute("INSERT OR IGNORE INTO categories (category_id, cname, rep_id) VALUES (1, 'General', 1)");

        // 99/99 split into 3 numeric segments by the user's 'yyyy-mm-dd' format but not a real date.
        var post = await client.PostAsync("/User/Problem/PostNew", Form(
            ("uid", "admin"), ("uemail", "admin@localhost"), ("department", "1"), ("category", "1"),
            ("priority", "2"), ("duedate", "2026-99-99"), ("title", "Bad date ticket"),
            ("description", "Should be rejected.")));

        var body = await post.Content.ReadAsStringAsync();
        Assert.Contains("is a required field", body);   // DisplayError(1, "Due Date")
        using var verify = app.OpenDb();
        Assert.Equal(0L, verify.ExecuteScalar<long>("SELECT COUNT(*) FROM problems WHERE title = 'Bad date ticket'"));
    }

    // ---- A2: Cfg()/Usr() of an absent setting/user render the faithful red box, not a raw 500 ----
    [Fact]
    public void Usr_of_a_missing_sid_throws_the_faithful_error()
    {
        using var fx = new HelpdeskFixture();
        var ex = Assert.Throws<LhdException>(() => fx.Users.Usr(987654, "uid"));
        Assert.Contains("User not found.", ex.Html);
        // A present row reads normally (sid=0 'unknown' exists) ...
        Assert.Equal("unknown", fx.Users.Usr(0, "uid"));
        // ... and a present row with a NULL column returns null without the EOF error (statustext is unseeded).
        Assert.Null(fx.Users.Usr(0, "statustext"));
    }

    // ---- A3: admin/test.asp page restored (Send Test Email + System Information entry) ----
    [Fact]
    public async Task Admin_test_page_renders_and_sends_a_test_email()
    {
        using var app = new HelpdeskWebApp();
        var client = await AdminClient(app);

        var page = await client.GetAsync("/Admin/Test");
        page.EnsureSuccessStatusCode();
        var html = await page.Content.ReadAsStringAsync();
        Assert.Contains("Test Configuration", html);
        Assert.Contains("Show System Information", html);
        Assert.Contains("Send test e-mail", html);

        app.Emails.Sent.Clear();
        var doit = await client.GetAsync("/Admin/Test?doit=1");
        doit.EnsureSuccessStatusCode();
        Assert.Contains("Message sent to", await doit.Content.ReadAsStringAsync());
        Assert.Single(app.Emails.Sent);
        Assert.Equal("Test Message", app.Emails.Sent[0].Subject);
        Assert.Equal("This is a test message from Liberum Help Desk", app.Emails.Sent[0].Body);
    }

    // ---- A4 + B6: status list shows the CloseStatus "*" marker + footnote, header uses "ID" ----
    [Fact]
    public async Task Status_list_marks_the_close_status_and_uses_the_id_header()
    {
        using var app = new HelpdeskWebApp();
        var client = await AdminClient(app);

        var resp = await client.GetAsync("/Admin/ViewStatus");
        resp.EnsureSuccessStatusCode();
        var html = await resp.Content.ReadAsStringAsync();
        Assert.Contains("<em>*</em>", html);                       // CloseStatus row marker
        Assert.Contains("Closed Status. Do not delete", html);     // footnote
        // First header is the "ID" lang key, not "Status Number".
        Assert.DoesNotContain("Status Number", html);
    }

    // ---- B2: rep/print.asp with no id => "No valid problem ID was entered." ----
    [Fact]
    public async Task Rep_print_without_an_id_shows_the_faithful_error()
    {
        using var app = new HelpdeskWebApp();
        var client = await RepClient(app);
        var resp = await client.GetAsync("/Rep/Problem/Print");
        Assert.Contains("No valid problem ID was entered.", await resp.Content.ReadAsStringAsync());
    }
}
