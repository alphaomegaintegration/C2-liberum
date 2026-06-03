using Dapper;
using LiberumHelpDesk.Web.Filters;
using LiberumHelpDesk.Web.Models.ViewModels;
using LiberumHelpDesk.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LiberumHelpDesk.Web.Areas.Admin.Controllers;

/// <summary>Ports admin/config.asp, admin/adminpass.asp, admin/sysinfo.asp.</summary>
[Area("Admin")]
public sealed class ConfigController : Controller
{
    private readonly Db _db;
    private readonly IConfigService _config;
    private readonly ILanguageService _lang;
    private readonly IEmailSender _sender;

    public ConfigController(Db db, IConfigService config, ILanguageService lang, IEmailSender sender)
    {
        _db = db; _config = config; _lang = lang; _sender = sender;
    }

    private static string Left(string s, int n) { s = s.Trim(); return s.Length > n ? s[..n] : s; }
    private List<Opt> Opts(string sql)
    {
        var list = new List<Opt>();
        foreach (var r in _db.Connection.Query(sql))
        { var d = (IDictionary<string, object>)r; list.Add(new Opt(Vb.CInt(d.Values.First()), Vb.Str(d.Values.Skip(1).First()))); }
        return list;
    }

    // admin/config.asp
    [HttpGet("/Admin/Config")]
    [HttpPost("/Admin/Config")]
    [CheckAdmin]
    public IActionResult Config()
    {
        var saved = false;
        if (Request.HasFormContentType && Request.Form["save"].ToString() == "1")
        {
            var f = Request.Form;
            _config.Update(new Dictionary<string, object?>
            {
                ["SiteName"] = Left(f["sitename"].ToString(), 50),
                ["BaseURL"] = Left(f["baseurl"].ToString(), 50),
                ["HDName"] = Left(f["hdname"].ToString(), 50),
                ["HDReply"] = Left(f["hdreply"].ToString(), 50),
                ["BaseEmail"] = Left(f["baseemail"].ToString(), 50),
                ["NotifyUser"] = Vb.CInt(f["notifyuser"].ToString()),
                ["EmailType"] = Vb.CInt(f["emailtype"].ToString()),
                ["EnableKB"] = Vb.CInt(f["enablekb"].ToString()),
                ["KBFreeText"] = Vb.CInt(f["kbfreetext"].ToString()),
                ["DefaultPriority"] = Vb.CInt(f["defaultpriority"].ToString()),
                ["DefaultStatus"] = Vb.CInt(f["defaultstatus"].ToString()),
                ["CloseStatus"] = Vb.CInt(f["closestatus"].ToString()),
                ["AuthType"] = Vb.CInt(f["authtype"].ToString()),
                ["SMTPServer"] = Left(f["smtpserver"].ToString(), 50),
                ["UseSelectUser"] = Vb.CInt(f["useSelectUser"].ToString()),
                ["UseInoutBoard"] = Vb.CInt(f["useInoutBoard"].ToString()),
                ["DefaultLanguage"] = Vb.CInt(f["DefaultLanguage"].ToString()),
                ["AllowImageUpload"] = Vb.CInt(f["AllowImageUpload"].ToString()),
                ["MaxImageSize"] = Left(f["MaxImageSize"].ToString(), 20),
                ["EnablePager"] = Vb.CInt(f["enablepager"].ToString()),
            });
            _lang.ClearCache(); // DefaultLanguage may have changed
            saved = true;
        }

        var langs = new List<LangOpt>();
        foreach (var r in _db.Connection.Query("SELECT id, LangName, Localized FROM tblLanguage"))
        { var d = (IDictionary<string, object>)r; langs.Add(new LangOpt(Vb.CInt(d["id"]), Vb.Str(d["LangName"]), Vb.Str(d["Localized"]))); }

        var closeStatus = _config.GetInt("CloseStatus");
        ViewData["Title"] = _lang.Lang("HelpDesk") + " - " + _lang.Lang("Configuration");
        return View(new ConfigVm
        {
            Saved = saved,
            SiteName = _config.GetString("SiteName"), BaseURL = _config.GetString("BaseURL"),
            HDName = _config.GetString("HDName"), HDReply = _config.GetString("HDReply"),
            BaseEmail = _config.GetString("BaseEmail"), SMTPServer = _config.GetString("SMTPServer"),
            MaxImageSize = _config.GetString("MaxImageSize"),
            EmailType = _config.GetInt("EmailType"), EnablePager = _config.GetInt("EnablePager"),
            NotifyUser = _config.GetInt("NotifyUser"), EnableKB = _config.GetInt("EnableKB"),
            KBFreeText = _config.GetInt("KBFreeText"), DefaultPriority = _config.GetInt("DefaultPriority"),
            DefaultStatus = _config.GetInt("DefaultStatus"), CloseStatus = closeStatus, AuthType = _config.GetInt("AuthType"),
            UseSelectUser = _config.GetInt("UseSelectUser"), UseInoutBoard = _config.GetInt("UseInoutBoard"),
            AllowImageUpload = _config.GetInt("AllowImageUpload"), DefaultLanguage = _config.GetInt("DefaultLanguage"),
            EmailTypes = Opts("SELECT id, type FROM tblConfig_Email"),
            Priorities = Opts("SELECT priority_id, pname FROM priority WHERE priority_id > 0"),
            Statuses = Opts("SELECT status_id, sname FROM status WHERE status_id > 0 ORDER BY status_id ASC"),
            StatusesNonClose = Opts($"SELECT status_id, sname FROM status WHERE status_id > 0 AND status_id <> {closeStatus} ORDER BY status_id ASC"),
            AuthTypes = Opts("SELECT id, type FROM tblConfig_Auth"),
            Languages = langs,
        });
    }

    // admin/adminpass.asp
    [HttpGet("/Admin/AdminPass")]
    [HttpPost("/Admin/AdminPass")]
    [CheckAdmin]
    public IActionResult AdminPass()
    {
        string message = "";
        var posted = Request.HasFormContentType && Request.Form["save"].ToString() == "1";
        if (posted)
        {
            var p1 = Left(Request.Form["AdminPass1"].ToString(), 50);
            var p2 = Left(Request.Form["AdminPass2"].ToString(), 50);
            var curr = Request.Form["CurrPass"].ToString().Trim();
            if (p1 == p2 && curr == _config.GetString("AdminPass"))
            {
                _config.Update(new Dictionary<string, object?> { ["AdminPass"] = p1 });
                message = _lang.Lang("PasswordChanged");
            }
            else message = _lang.Lang("PasswordChangeFailed");
        }
        ViewData["Title"] = _lang.Lang("HelpDesk") + " - " + _lang.Lang("ChangeAdminPassword");
        ViewData["Posted"] = posted;
        ViewData["Message"] = message;
        return View();
    }

    // admin/test.asp — misc test commands: send a test email + entry to System Information.
    [HttpGet("/Admin/Test")]
    [CheckAdmin]
    public IActionResult Test()
    {
        var sentTo = "";
        if (Request.Query["doit"].ToString() == "1")
        {
            // Original reads tblConfig (EOF -> "Unable to read configuration..."); Cfg() now surfaces that
            // faithfully on a missing row. SendMail(strTo, Cfg("HDReply"), Cfg("HDName"), subject, body).
            sentTo = _config.GetString("HDReply");
            _sender.Send(sentTo, _config.GetString("HDReply"), _config.GetString("HDName"),
                "Test Message", "This is a test message from Liberum Help Desk");
        }
        ViewData["Title"] = _lang.Lang("HelpDesk") + " - " + _lang.Lang("TestConfiguration");
        ViewData["SentTo"] = sentTo;
        return View();
    }

    // admin/config_help.asp — popup describing each configuration setting.
    [HttpGet("/Admin/ConfigHelp")]
    [CheckAdmin]
    public IActionResult ConfigHelp()
    {
        ViewData["Title"] = _lang.Lang("HelpDesk") + " - " + _lang.Lang("ConfigurationHelp");
        return View();
    }

    // admin/sysinfo.asp (adapted for the SQLite/.NET host)
    [HttpGet("/Admin/SysInfo")]
    [CheckAdmin]
    public IActionResult SysInfo([FromServices] IConfiguration configuration)
    {
        var rows = new List<(string Key, string Value)>
        {
            ("Database", "SQLite (Microsoft.Data.Sqlite)"),
            ("ConnectionString", configuration.GetConnectionString("HelpDesk") ?? ""),
            ("BaseURL", _config.GetString("BaseURL")),
            ("EmailType", _config.GetString("EmailType")),
            ("SMTPServer", _config.GetString("SMTPServer")),
            ("EnablePager", _config.GetString("EnablePager")),
            ("AuthType", _config.GetString("AuthType")),
            ("Version", _config.GetString("Version")),
            ("UseInOutBoard", _config.GetString("UseInoutBoard")),
            ("KBFreeText", _config.GetString("KBFreeText")),
            ("Runtime", Environment.Version.ToString()),
            ("OS", Environment.OSVersion.ToString()),
            ("Host", Request.Host.ToString()),
        };
        ViewData["Title"] = "System Information";
        return View(rows);
    }
}
