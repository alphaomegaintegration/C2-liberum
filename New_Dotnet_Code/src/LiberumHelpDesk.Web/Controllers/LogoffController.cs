using LiberumHelpDesk.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LiberumHelpDesk.Web.Controllers;

/// <summary>Port of logoff.asp — clears the session and shows the logged-off page.</summary>
[Route("Logoff")]
public sealed class LogoffController : Controller
{
    private readonly IConfigService _config;
    private readonly ILanguageService _lang;
    private readonly ISessionContext _session;

    public LogoffController(IConfigService config, ILanguageService lang, ISessionContext session)
    {
        _config = config;
        _lang = lang;
        _session = session;
    }

    [HttpGet]
    public IActionResult Index()
    {
        // The original renders the page, then clears Session and sets sid=0 before DisplayFooter,
        // so the footer is rendered for an anonymous user (copyright only). We clear first; the
        // footer view component reads the now-zero sid.
        _session.SignOut();
        ViewData["Title"] = _lang.Lang("HelpDesk") + "&nbsp;-&nbsp;" + _lang.Lang("LogOff");
        ViewData["SiteName"] = _config.GetString("SiteName");
        return View();
    }
}
