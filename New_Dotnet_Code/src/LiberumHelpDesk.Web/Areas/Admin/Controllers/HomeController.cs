using LiberumHelpDesk.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LiberumHelpDesk.Web.Areas.Admin.Controllers;

/// <summary>Port of admin/default.asp — the admin password gate (sets lhd_IsAdmin) and the admin menu.</summary>
[Area("Admin")]
public sealed class HomeController : Controller
{
    private readonly IConfigService _config;
    private readonly ISessionContext _session;
    private readonly ILanguageService _lang;

    public HomeController(IConfigService config, ISessionContext session, ILanguageService lang)
    {
        _config = config;
        _session = session;
        _lang = lang;
    }

    [HttpGet("/Admin")]
    [HttpPost("/Admin")]
    public IActionResult Index()
    {
        ViewData["Title"] = _lang.Lang("HelpDesk") + "&nbsp;-&nbsp;" + _lang.Lang("AdministrativeMenu");

        if (!_session.IsAdmin)
        {
            var posted = Request.HasFormContentType ? Request.Form["password"].ToString() : "";
            if (posted.Trim() == _config.GetString("AdminPass"))
            {
                _session.IsAdmin = true;
            }
            else
            {
                ViewData["ShowGate"] = true;
                ViewData["WrongPassword"] = posted.Length > 0;
                return View();
            }
        }

        ViewData["ShowGate"] = false;
        return View();
    }
}
