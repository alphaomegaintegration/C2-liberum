using LiberumHelpDesk.Web.Filters;
using LiberumHelpDesk.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LiberumHelpDesk.Web.Areas.User.Controllers;

/// <summary>Port of user/default.asp — the user main menu.</summary>
[Area("User")]
public sealed class HomeController : Controller
{
    private readonly IConfigService _config;
    private readonly ILanguageService _lang;
    public HomeController(IConfigService config, ILanguageService lang) { _config = config; _lang = lang; }

    [HttpGet]
    [CheckUser]
    public IActionResult Index()
    {
        ViewData["Title"] = _config.GetString("SiteName") + "&nbsp;" + _lang.Lang("HelpDesk");
        ViewData["SiteName"] = _config.GetString("SiteName");
        ViewData["EnableKB"] = _config.GetInt("EnableKB");
        ViewData["UseInoutBoard"] = _config.GetInt("UseInoutBoard");
        return View();
    }
}
