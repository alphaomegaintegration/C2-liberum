using Dapper;

namespace LiberumHelpDesk.Tests;

public class AdminUserTests
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

    private static (string, string)[] BaseUserFields() => new[]
    {
        ("firstname", "Bob"), ("lastname", "Jones"),
        ("pager", ""), ("phone", ""), ("phone_home", ""), ("phone_mobile", ""), ("location", ""),
        ("department", "0"), ("usrLanguage", "1"), ("repaccess", "0"), ("statuscode", "0"),
        ("statustext", ""), ("ListOnInoutBoard", "1"), ("jobfunction", ""), ("userresume", ""),
    };

    [Fact]
    public async Task Add_edit_and_delete_a_user()
    {
        using var app = new HelpdeskWebApp();
        var client = await AdminClient(app);

        // Add
        var add = await client.PostAsync("/Admin/AddUser",
            Form(BaseUserFields().Concat(new[] { ("save", "1"), ("uid", "bob"), ("email", "bob@x.com"), ("newpassword", "bobpass") }).ToArray()));
        add.EnsureSuccessStatusCode();
        Assert.Contains("Account Created", await add.Content.ReadAsStringAsync());

        int sid;
        using (var db = app.OpenDb())
        {
            Assert.Equal(1L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM tblUsers WHERE uid = 'bob'"));
            sid = (int)db.ExecuteScalar<long>("SELECT sid FROM tblUsers WHERE uid = 'bob'");
        }

        // The list shows the new user.
        var list = await client.GetAsync("/Admin/ViewUsers");
        Assert.Contains("bob", await list.Content.ReadAsStringAsync());

        // Edit (change email).
        var edit = await client.PostAsync("/Admin/ModUser",
            Form(BaseUserFields().Concat(new[] { ("save", "1"), ("usersid", sid.ToString()), ("email", "bob2@x.com"), ("newpassword", "") }).ToArray()));
        edit.EnsureSuccessStatusCode();
        Assert.Contains("Account Updated", await edit.Content.ReadAsStringAsync());
        using (var db = app.OpenDb())
            Assert.Equal("bob2@x.com", db.ExecuteScalar<string>("SELECT email1 FROM tblUsers WHERE sid = @s", new { s = sid }));

        // Delete.
        var del = await client.PostAsync("/Admin/ModUser", Form(("usersid", sid.ToString()), ("delete", "1")));
        del.EnsureSuccessStatusCode();
        Assert.Contains("Account Deleted", await del.Content.ReadAsStringAsync());
        using (var db = app.OpenDb())
            Assert.Equal(0L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM tblUsers WHERE sid = @s", new { s = sid }));
    }

    [Fact]
    public async Task Added_user_can_log_in()
    {
        using var app = new HelpdeskWebApp();
        var admin = await AdminClient(app);
        await admin.PostAsync("/Admin/AddUser",
            Form(BaseUserFields().Concat(new[] { ("save", "1"), ("uid", "carol"), ("email", "carol@x.com"), ("newpassword", "carolpw") }).ToArray()));

        // A fresh client logs in as the new account.
        var client = app.NewClient();
        var login = await client.PostAsync("/Logon?URL=default.asp",
            Form(("logon", "1"), ("uid", "carol"), ("password", "carolpw")));
        Assert.Equal(System.Net.HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal("/", login.Headers.Location!.OriginalString);
    }
}
