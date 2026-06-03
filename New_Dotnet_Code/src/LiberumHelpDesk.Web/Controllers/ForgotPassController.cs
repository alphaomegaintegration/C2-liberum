using Dapper;
using LiberumHelpDesk.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LiberumHelpDesk.Web.Controllers;

/// <summary>Port of forgotpass.asp — emails the user their (plain-text) password.</summary>
[Route("ForgotPass")]
public sealed class ForgotPassController : Controller
{
    private readonly Db _db;
    private readonly IConfigService _config;
    private readonly ILanguageService _lang;
    private readonly IEmailSender _sender;

    public ForgotPassController(Db db, IConfigService config, ILanguageService lang, IEmailSender sender)
    {
        _db = db; _config = config; _lang = lang; _sender = sender;
    }

    [HttpGet]
    [HttpPost]
    public IActionResult Index()
    {
        var success = false;
        var invalidUid = false;

        if (Request.HasFormContentType && Request.Form["email"].ToString() == "1")
        {
            var uid = Request.Form["uid"].ToString().Trim().ToLowerInvariant();
            var row = _db.Connection.QueryFirstOrDefault(
                "SELECT email1, password FROM tblUsers WHERE uid=@uid COLLATE NOCASE", new { uid });
            if (row is null)
            {
                invalidUid = true;
            }
            else
            {
                var d = (IDictionary<string, object>)row;
                var subject = _lang.Lang("HELPDESK") + " : " + _lang.Lang("password");
                var body = "Username: " + uid + "\n" +
                           "Password: " + Vb.Str(d["password"]) + "\n" + "\n" +
                           "Log in to the help desk @ " + _config.GetString("BaseURL");
                _sender.Send(Vb.Str(d["email1"]), _config.GetString("HDReply"), _config.GetString("HDName"), subject, body);
                success = true;
            }
        }

        ViewData["Title"] = _lang.Lang("HelpDesk") + " - " + _lang.Lang("EmailPassword");
        ViewData["Success"] = success;
        ViewData["InvalidUid"] = invalidUid;
        return View();
    }
}
