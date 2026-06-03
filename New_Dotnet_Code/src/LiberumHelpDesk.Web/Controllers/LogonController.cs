using Dapper;
using LiberumHelpDesk.Web.Models.ViewModels;
using LiberumHelpDesk.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LiberumHelpDesk.Web.Controllers;

/// <summary>Port of logon.asp — the three AuthTypes (NT / Database / External).</summary>
[Route("Logon")]
public sealed class LogonController : Controller
{
    private readonly Db _db;
    private readonly IConfigService _config;
    private readonly ISessionContext _session;
    private readonly IKeyService _keys;
    private readonly ILanguageService _lang;
    private readonly IErrorService _error;

    public LogonController(Db db, IConfigService config, ISessionContext session,
        IKeyService keys, ILanguageService lang, IErrorService error)
    {
        _db = db;
        _config = config;
        _session = session;
        _keys = keys;
        _lang = lang;
        _error = error;
    }

    private static string Left(string s, int n) => s.Length > n ? s[..n] : s;

    private static string MapUrl(string url)
    {
        if (url == "default.asp") return "/";
        if (url.StartsWith("register.asp", StringComparison.OrdinalIgnoreCase))
            return "/Register" + url["register.asp".Length..];
        return url; // an MVC returnUrl path captured by CheckUser
    }

    [HttpGet]
    [HttpPost]
    public IActionResult Index()
    {
        var url = (Request.Query["URL"].ToString() ?? "").Trim();
        if (url.Length == 0) url = "default.asp";

        string? frmUrl = null;
        var invalid = false;
        var conn = _db.Connection;

        switch (_config.GetInt("AuthType"))
        {
            case 1: // NT Authentication (Linux fallback: trust an upstream proxy header)
            {
                var username = Request.Headers["X-Forwarded-User"].ToString();
                if (string.IsNullOrEmpty(username)) username = HttpContext.User.Identity?.Name ?? "";
                if (username.Length == 0)
                    throw _error.Error(3, _lang.Lang("UnabletoobtainusernamewithNTauthentication"));

                var bs = username.IndexOf('\\');
                if (bs >= 0) username = username[(bs + 1)..];
                username = username.ToLowerInvariant();
                if (username.Contains('\''))
                    throw _error.Error(3, _lang.Lang("Username") + "&nbsp;" + _lang.Lang("containsinvalidcharacters") + ".");

                // Verbatim existence-check semantics (operator-precedence quirk preserved): a freshly
                // provisioned user (fname & email1 NULL) does NOT match, so it redirects to register.
                var sid = conn.ExecuteScalar<long?>(
                    "SELECT sid FROM tblUsers WHERE (uid=@u COLLATE NOCASE AND fname IS NOT NULL) " +
                    "OR (uid=@u COLLATE NOCASE AND email1 IS NOT NULL)", new { u = username });
                if (sid is null)
                {
                    var newSid = _keys.GetUnique("users");
                    conn.Execute("INSERT INTO tblUsers (sid, uid) VALUES (@s, @u)", new { s = newSid, u = username });
                    _session.Sid = (int)conn.ExecuteScalar<long>(
                        "SELECT sid FROM tblUsers WHERE uid=@u COLLATE NOCASE", new { u = username });
                    url = "register.asp?edit=1&new=1";
                }
                else _session.Sid = (int)sid.Value;
                break;
            }

            case 2: // Database Authentication (plain-text compare, faithful)
            {
                if (Request.HasFormContentType && Vb.CInt(Request.Form["logon"].ToString()) == 1)
                {
                    var username = Left(Request.Form["uid"].ToString().Trim().ToLowerInvariant(), 50);
                    var password = Left(Request.Form["password"].ToString().Trim(), 50);

                    var row = conn.QueryFirstOrDefault(
                        "SELECT sid, password FROM tblUsers WHERE uid=@u COLLATE NOCASE", new { u = username });
                    if (row is null)
                    {
                        invalid = true; url = "";
                    }
                    else
                    {
                        var d = (IDictionary<string, object>)row;
                        if (Vb.Str(d["password"]) != password) // case-sensitive, like VBScript <>
                        {
                            invalid = true; frmUrl = url; url = "";
                        }
                        else _session.Sid = Convert.ToInt32(d["sid"]);
                    }
                }
                else { frmUrl = url; url = ""; }
                break;
            }

            case 3: // External Authentication
            {
                string username;
                if (!string.IsNullOrEmpty(_session.ExtUid)) username = _session.ExtUid!.Trim().ToLowerInvariant();
                else if (!string.IsNullOrEmpty(Request.Form["lhd_ext_uid"])) username = Request.Form["lhd_ext_uid"].ToString().Trim().ToLowerInvariant();
                else if (!string.IsNullOrEmpty(Request.Query["lhd_ext_uid"])) username = Request.Query["lhd_ext_uid"].ToString().Trim().ToLowerInvariant();
                else throw _error.Error(3, _lang.Lang("Nousernamewasspecifiedbytheexternalauthenication") + ".");

                if (username.Contains('\''))
                    throw _error.Error(3, _lang.Lang("Username") + "&nbsp;" + _lang.Lang("containsinvalidcharacters") + ".");

                var sid = conn.ExecuteScalar<long?>(
                    "SELECT sid FROM tblUsers WHERE uid=@u COLLATE NOCASE", new { u = username });
                if (sid is null)
                {
                    var newSid = _keys.GetUnique("users");
                    conn.Execute("INSERT INTO tblUsers (sid, uid) VALUES (@s, @u)", new { s = newSid, u = username });
                    _session.Sid = (int)conn.ExecuteScalar<long>(
                        "SELECT sid FROM tblUsers WHERE uid=@u COLLATE NOCASE", new { u = username });
                    url = "register.asp?edit=1&new=1";
                }
                else _session.Sid = (int)sid.Value;
                break;
            }
        }

        _session.IsAdmin = false;

        if (url.Length > 0)
        {
            conn.Execute("UPDATE tblUsers SET dtLastAccess = @now WHERE sid = @sid",
                new { now = DateTime.Now, sid = _session.Sid });
            return Redirect(MapUrl(url));
        }

        ViewData["Title"] = _config.GetString("SiteName") + "&nbsp;" + _lang.Lang("HelpDesk");
        return View(new LogonViewModel
        {
            SiteName = _config.GetString("SiteName"),
            Invalid = invalid,
            FrmUrl = frmUrl ?? "",
            EnableKB = _config.GetInt("EnableKB"),
            EmailType = _config.GetInt("EmailType"),
        });
    }
}
