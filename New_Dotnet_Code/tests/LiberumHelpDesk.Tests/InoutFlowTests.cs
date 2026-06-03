using System.Net;
using System.Net.Http.Headers;
using Dapper;

namespace LiberumHelpDesk.Tests;

// Own app per test (status update mutates the shared admin row).
public class InoutFlowTests
{
    private static FormUrlEncodedContent Form(params (string, string)[] fields) =>
        new(fields.Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));

    private static async Task<HttpClient> LoggedIn(HelpdeskWebApp app)
    {
        var client = app.NewClient();
        var login = await client.PostAsync("/Logon?URL=default.asp",
            Form(("logon", "1"), ("uid", "admin"), ("password", "admin")));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        return client;
    }

    [Fact]
    public async Task Board_renders_and_lists_the_admin_user()
    {
        using var app = new HelpdeskWebApp();
        var client = await LoggedIn(app);

        var board = await client.GetAsync("/Inout");
        board.EnsureSuccessStatusCode();
        var body = await board.Content.ReadAsStringAsync();
        Assert.Contains("In/Out Board", body);
        Assert.Contains("admin", body); // the sample user is listed (ListOnInoutBoard=1)
    }

    [Fact]
    public async Task Updating_status_to_out_persists_and_shows_on_details()
    {
        using var app = new HelpdeskWebApp();
        var client = await LoggedIn(app);

        var status = await client.PostAsync("/Inout/Status?id=1",
            Form(("save", "1"), ("frm_status", "on"), ("frm_statustext", "Out to lunch")));
        status.EnsureSuccessStatusCode();
        Assert.Contains("is Updated", await status.Content.ReadAsStringAsync());

        using (var db = app.OpenDb())
        {
            Assert.Equal(1L, db.ExecuteScalar<long>("SELECT statuscode FROM tblUsers WHERE sid = 1"));
            Assert.Equal("Out to lunch", db.ExecuteScalar<string>("SELECT statustext FROM tblUsers WHERE sid = 1"));
        }

        var details = await client.GetAsync("/Inout/Details?id=1");
        details.EnsureSuccessStatusCode();
        var body = await details.Content.ReadAsStringAsync();
        Assert.Contains("Out to lunch", body);
        Assert.Contains("red_pin.gif", body); // status code 1 -> red pin
    }

    [Fact]
    public async Task Image_upload_rejects_an_undersized_file()
    {
        using var app = new HelpdeskWebApp();
        var client = await LoggedIn(app);

        using var content = new MultipartFormDataContent();
        var bytes = new byte[10]; // below the 1000-byte minimum
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(file, "blob", "tiny.jpg");

        var resp = await client.PostAsync("/Inout/SaveFile?uid=1", content);
        resp.EnsureSuccessStatusCode();
        Assert.Contains("An error occurred uploading a file", await resp.Content.ReadAsStringAsync());
    }
}
