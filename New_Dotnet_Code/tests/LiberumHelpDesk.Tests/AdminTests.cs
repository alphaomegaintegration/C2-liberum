using System.Net;
using Dapper;

namespace LiberumHelpDesk.Tests;

public class AdminTests
{
    private static FormUrlEncodedContent Form(params (string, string)[] fields) =>
        new(fields.Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));

    private static async Task<HttpClient> AdminClient(HelpdeskWebApp app)
    {
        var client = app.NewClient();
        // The admin gate is independent of user login: just submit the AdminPass ('admin').
        var gate = await client.PostAsync("/Admin", Form(("password", "admin")));
        gate.EnsureSuccessStatusCode();
        Assert.Contains("Administrative Menu", await gate.Content.ReadAsStringAsync());
        return client;
    }

    [Fact]
    public async Task Gate_requires_the_admin_password()
    {
        using var app = new HelpdeskWebApp();
        var client = app.NewClient();

        var prompt = await client.GetAsync("/Admin");
        prompt.EnsureSuccessStatusCode();
        Assert.Contains("Administrative Logon", await prompt.Content.ReadAsStringAsync());

        var wrong = await client.PostAsync("/Admin", Form(("password", "nope")));
        Assert.Contains("Password is incorrect", await wrong.Content.ReadAsStringAsync());

        var right = await client.PostAsync("/Admin", Form(("password", "admin")));
        Assert.Contains("Administrative Menu", await right.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Lookup_pages_require_admin_session()
    {
        using var app = new HelpdeskWebApp();
        var client = app.NewClient();
        // No admin gate yet -> CheckAdmin denies.
        var denied = await client.GetAsync("/Admin/ViewCat");
        Assert.Contains("Access denied", await denied.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Add_category_then_it_appears_then_delete_removes_it()
    {
        using var app = new HelpdeskWebApp();
        var client = await AdminClient(app);

        // Add a category (rep_id 0 = unknown is fine for the dropdown default).
        var add = await client.PostAsync("/Admin/PostMods",
            Form(("mtype", "2"), ("data_id", "0"), ("numdatafields", "2"), ("data1", "Networking"), ("data2", "0")));
        add.EnsureSuccessStatusCode();
        Assert.Contains("Operation Complete", await add.Content.ReadAsStringAsync());

        using (var db = app.OpenDb())
            Assert.Equal(1L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM categories WHERE cname = 'Networking'"));

        var list = await client.GetAsync("/Admin/ViewCat");
        Assert.Contains("Networking", await list.Content.ReadAsStringAsync());

        int catId;
        using (var db = app.OpenDb())
            catId = (int)db.ExecuteScalar<long>("SELECT category_id FROM categories WHERE cname = 'Networking'");

        // Confirm-delete (no open problems use it) then delete.
        var conf = await client.GetAsync($"/Admin/ConfDelete?mtype=2&id={catId}");
        Assert.Contains("Are you sure", await conf.Content.ReadAsStringAsync());

        var del = await client.GetAsync($"/Admin/Delete?mtype=2&id={catId}");
        Assert.Contains("Operation Complete", await del.Content.ReadAsStringAsync());

        using (var db = app.OpenDb())
            Assert.Equal(0L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM categories WHERE cname = 'Networking'"));
    }

    [Fact]
    public async Task Add_priority_with_duplicate_number_is_rejected()
    {
        using var app = new HelpdeskWebApp();
        var client = await AdminClient(app);
        // priority_id 1 (LOW) is seeded -> duplicate number rejected.
        var dup = await client.PostAsync("/Admin/PostMods",
            Form(("mtype", "4"), ("data_id", "0"), ("numdatafields", "2"), ("data1", "1"), ("data2", "Medium")));
        Assert.Contains("unique priority number", await dup.Content.ReadAsStringAsync());
    }
}
