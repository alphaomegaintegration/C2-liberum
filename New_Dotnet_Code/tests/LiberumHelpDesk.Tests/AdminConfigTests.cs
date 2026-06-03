using Dapper;

namespace LiberumHelpDesk.Tests;

public class AdminConfigTests
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
    public async Task Config_save_updates_tblConfig()
    {
        using var app = new HelpdeskWebApp();
        var client = await AdminClient(app);

        var get = await client.GetAsync("/Admin/Config");
        get.EnsureSuccessStatusCode();
        Assert.Contains("Company Name", await get.Content.ReadAsStringAsync()); // seeded SiteName

        var post = await client.PostAsync("/Admin/Config", Form(
            ("save", "1"), ("sitename", "Acme Helpdesk"), ("baseurl", "http://acme/helpdesk"),
            ("hdname", "Consultant"), ("hdreply", "help@acme.com"), ("baseemail", "@acme.com"),
            ("emailtype", "1"), ("notifyuser", "1"), ("defaultpriority", "1"), ("defaultstatus", "1"),
            ("closestatus", "100"), ("authtype", "2"), ("enablekb", "2"), ("smtpserver", "smtp.acme.com"),
            ("enablepager", "0"), ("useSelectUser", "1"), ("useInoutBoard", "1"), ("kbfreetext", "0"),
            ("DefaultLanguage", "1"), ("AllowImageUpload", "0"), ("MaxImageSize", "200000")));
        post.EnsureSuccessStatusCode();
        Assert.Contains("Configuration Saved", await post.Content.ReadAsStringAsync());

        using var db = app.OpenDb();
        Assert.Equal("Acme Helpdesk", db.ExecuteScalar<string>("SELECT SiteName FROM tblConfig"));
        Assert.Equal(1L, db.ExecuteScalar<long>("SELECT NotifyUser FROM tblConfig"));
        Assert.Equal(1L, db.ExecuteScalar<long>("SELECT UseInoutBoard FROM tblConfig"));
    }

    [Fact]
    public async Task Admin_password_change_succeeds_with_correct_current_password()
    {
        using var app = new HelpdeskWebApp();
        var client = await AdminClient(app);

        var ok = await client.PostAsync("/Admin/AdminPass",
            Form(("save", "1"), ("CurrPass", "admin"), ("AdminPass1", "newsecret"), ("AdminPass2", "newsecret")));
        Assert.Contains("Password Changed", await ok.Content.ReadAsStringAsync());
        using (var db = app.OpenDb())
            Assert.Equal("newsecret", db.ExecuteScalar<string>("SELECT AdminPass FROM tblConfig"));
    }

    [Fact]
    public async Task Admin_password_change_fails_with_wrong_current_password()
    {
        using var app = new HelpdeskWebApp();
        var client = await AdminClient(app);

        var bad = await client.PostAsync("/Admin/AdminPass",
            Form(("save", "1"), ("CurrPass", "wrong"), ("AdminPass1", "x"), ("AdminPass2", "x")));
        Assert.Contains("Password Change Failed", await bad.Content.ReadAsStringAsync());
        using (var db = app.OpenDb())
            Assert.Equal("admin", db.ExecuteScalar<string>("SELECT AdminPass FROM tblConfig")); // unchanged
    }

    [Fact]
    public async Task SysInfo_renders()
    {
        using var app = new HelpdeskWebApp();
        var client = await AdminClient(app);
        var resp = await client.GetAsync("/Admin/SysInfo");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("System Information", body);
        Assert.Contains("SQLite", body);
    }
}
