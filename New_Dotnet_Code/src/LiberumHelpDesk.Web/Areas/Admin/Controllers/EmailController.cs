using Dapper;
using LiberumHelpDesk.Web.Filters;
using LiberumHelpDesk.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LiberumHelpDesk.Web.Areas.Admin.Controllers;

/// <summary>Port of admin/cfgemail.asp — edit the tblEmailMsg templates.</summary>
[Area("Admin")]
public sealed class EmailController : Controller
{
    private readonly Db _db;
    private readonly ILanguageService _lang;
    private readonly IErrorService _error;

    public EmailController(Db db, ILanguageService lang, IErrorService error)
    {
        _db = db; _lang = lang; _error = error;
    }

    private static string Left(string s, int n) { s = s.Trim(); return s.Length > n ? s[..n] : s; }

    [HttpGet("/Admin/CfgEmail")]
    [HttpPost("/Admin/CfgEmail")]
    [CheckAdmin]
    public IActionResult Index()
    {
        var f = Request.HasFormContentType ? Request.Form : null;
        var eType = Left(f?["type"].ToString() ?? "", 50);
        var displayMenu = eType.Length == 0;
        var saved = false;

        if (f != null && f["save"].ToString() == "1")
        {
            _db.Connection.Execute("UPDATE tblEmailMsg SET subject=@s, body=@b WHERE type=@t",
                new { s = Left(f["subject"].ToString(), 50), b = f["body"].ToString().Trim(), t = eType });
            saved = true;
        }

        string subject = "", body = "";
        if (!displayMenu)
        {
            var row = _db.Connection.QueryFirstOrDefault("SELECT subject, body FROM tblEmailMsg WHERE type=@t", new { t = eType });
            if (row is null) throw _error.Error(3, "Unable to read message from the database.");
            var d = (IDictionary<string, object>)row;
            subject = Vb.Str(d["subject"]);
            body = Vb.Str(d["body"]);
        }

        ViewData["Title"] = _lang.Lang("HelpDesk") + " - " + _lang.Lang("MessageConfiguration");
        ViewData["DisplayMenu"] = displayMenu;
        ViewData["Saved"] = saved;
        ViewData["EType"] = eType;
        ViewData["Subject"] = subject;
        ViewData["Body"] = body;
        return View();
    }

    // admin/cfgemail_help.asp — popup listing the email template variables.
    [HttpGet("/Admin/CfgEmailHelp")]
    [CheckAdmin]
    public IActionResult CfgEmailHelp()
    {
        ViewData["Title"] = _lang.Lang("HelpDesk") + " - " + _lang.Lang("MessageConfigurationHelp");
        return View();
    }
}
