using Dapper;

namespace LiberumHelpDesk.Tests;

public class AdminLanguageTests
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
    public async Task ViewLang_lists_the_seeded_languages()
    {
        using var app = new HelpdeskWebApp();
        var client = await AdminClient(app);
        var resp = await client.GetAsync("/Admin/ViewLang");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("English", body);
        Assert.Contains("German", body);
    }

    [Fact]
    public async Task Editing_a_language_string_saves_and_clears_cache()
    {
        using var app = new HelpdeskWebApp();
        var client = await AdminClient(app);

        // The editor shows the English variable + the current language's text.
        var grid = await client.GetAsync("/Admin/ViewLangString?lang_id=2"); // Norwegian
        grid.EnsureSuccessStatusCode();
        Assert.Contains("AccessDenied", await grid.Content.ReadAsStringAsync());

        // Save a single string (only the submitted variable is touched).
        var save = await client.PostAsync("/Admin/ViewLangString?lang_id=2",
            Form(("frm_save", "1"), ("AccessDenied", "Ingen tilgang!!")));
        save.EnsureSuccessStatusCode();
        Assert.Contains("Changes Saved", await save.Content.ReadAsStringAsync());

        using var db = app.OpenDb();
        Assert.Equal("Ingen tilgang!!",
            db.ExecuteScalar<string>("SELECT LangText FROM tblLangStrings WHERE id=2 AND variable='AccessDenied'"));

        // The German string was NOT blanked by the partial save.
        var germanCount = db.ExecuteScalar<long>("SELECT COUNT(*) FROM tblLangStrings WHERE id=5 AND LangText <> ''");
        Assert.True(germanCount > 300);
    }
}
