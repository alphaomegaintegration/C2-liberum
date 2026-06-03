using LiberumHelpDesk.Web.Filters;
using LiberumHelpDesk.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LiberumHelpDesk.Web.Controllers;

/// <summary>Port of default.asp — landing page that redirects to the rep or user menu.</summary>
public sealed class HomeController : Controller
{
    private readonly IUserService _users;
    private readonly ISessionContext _session;

    public HomeController(IUserService users, ISessionContext session)
    {
        _users = users;
        _session = session;
    }

    [HttpGet("/")]
    [CheckUser]
    public IActionResult Index()
    {
        // CheckUser has already redirected to logon if not authenticated.
        return _users.UsrInt(_session.Sid, "IsRep") == 1
            ? Redirect("/Rep")
            : Redirect("/User");
    }
}
