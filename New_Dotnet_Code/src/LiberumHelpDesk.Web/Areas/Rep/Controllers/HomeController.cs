using Dapper;
using LiberumHelpDesk.Web.Filters;
using LiberumHelpDesk.Web.Models.ViewModels;
using LiberumHelpDesk.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LiberumHelpDesk.Web.Areas.Rep.Controllers;

/// <summary>Port of rep/default.asp — the support rep main menu.</summary>
[Area("Rep")]
public sealed class HomeController : Controller
{
    private readonly Db _db;
    private readonly IConfigService _config;
    private readonly ISessionContext _session;
    private readonly IUserService _users;
    private readonly ILanguageService _lang;

    public HomeController(Db db, IConfigService config, ISessionContext session, IUserService users, ILanguageService lang)
    {
        _db = db; _config = config; _session = session; _users = users; _lang = lang;
    }

    [HttpGet]
    [CheckRep]
    public IActionResult Index()
    {
        var sid = _session.Sid;
        var repAccess = _users.UsrInt(sid, "RepAccess");
        var closeStatus = _config.GetInt("CloseStatus");

        var reps = new List<RepOption>();
        if (repAccess != 1)
        {
            foreach (var r in _db.Connection.Query(
                "SELECT sid, uid FROM tblUsers WHERE IsRep = 1 AND RepAccess <> 2 ORDER BY uid COLLATE NOCASE ASC"))
            {
                var d = (IDictionary<string, object>)r;
                var rsid = Vb.CInt(d["sid"]);
                var count = (int)_db.Connection.ExecuteScalar<long>(
                    "SELECT COUNT(id) FROM problems WHERE rep = @rep AND status <> @cs", new { rep = rsid, cs = closeStatus });
                reps.Add(new RepOption(rsid, Vb.Str(d["uid"]), count, rsid == sid));
            }
        }

        // rep/default.asp title: <% = Cfg("SiteName") %> <%=lang("HelpDesk")%>. The Classic ASP
        // <% = expr %> output form (space after <%) consumes the following literal space, so the live
        // original renders "Company NameHelp Desk" (no space). Reproduce that faithfully.
        ViewData["Title"] = _config.GetString("SiteName") + _lang.Lang("HelpDesk");
        return View(new RepMenuVm
        {
            SiteName = _config.GetString("SiteName"),
            RepAccess = repAccess,
            EnableKB = _config.GetInt("EnableKB"),
            UseInout = _config.GetInt("UseInoutBoard"),
            Reps = reps,
        });
    }
}
