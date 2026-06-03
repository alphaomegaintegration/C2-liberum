using LiberumHelpDesk.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LiberumHelpDesk.Web.Filters;

// Helper to build the logon redirect like CheckUser/CheckRep (logon.asp?URL=<path>[?<query>]).
internal static class AuthRedirect
{
    public static RedirectResult ToLogon(HttpContext ctx)
    {
        var path = ctx.Request.Path.Value ?? "/";
        var url = path;
        if (ctx.Request.QueryString.HasValue)
            url += ctx.Request.QueryString.Value;
        return new RedirectResult("/Logon?URL=" + Uri.EscapeDataString(url));
    }
}

/// <summary>CheckUser: redirect to logon when not authenticated.</summary>
public sealed class CheckUserAttribute : TypeFilterAttribute
{
    public CheckUserAttribute() : base(typeof(Filter)) { }

    private sealed class Filter : IActionFilter
    {
        private readonly ISessionContext _session;
        private readonly IUserService _users;
        public Filter(ISessionContext session, IUserService users) { _session = session; _users = users; }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var sid = _session.Sid;
            if (sid == 0 || !_users.Exists(sid))
                context.Result = AuthRedirect.ToLogon(context.HttpContext);
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}

/// <summary>CheckRep: redirect when not logged in, then deny when IsRep &lt;&gt; 1.</summary>
public sealed class CheckRepAttribute : TypeFilterAttribute
{
    public CheckRepAttribute() : base(typeof(Filter)) { }

    private sealed class Filter : IActionFilter
    {
        private readonly ISessionContext _session;
        private readonly IUserService _users;
        private readonly IErrorService _error;
        public Filter(ISessionContext session, IUserService users, IErrorService error)
        { _session = session; _users = users; _error = error; }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var sid = _session.Sid;
            if (sid == 0 || !_users.Exists(sid))
            {
                context.Result = AuthRedirect.ToLogon(context.HttpContext);
                return;
            }
            // CheckRep error message is a literal English string in the original (not localized).
            if (_users.UsrInt(sid, "IsRep") != 1)
                throw _error.Error(3, "Access denied.  You do not have permission to view this page.");
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}

/// <summary>CheckKB: permission by EnableKB (0 disabled, 1 rep, 2 user/rep, 3 anyone).</summary>
public sealed class CheckKbAttribute : TypeFilterAttribute
{
    public CheckKbAttribute() : base(typeof(Filter)) { }

    private sealed class Filter : IActionFilter
    {
        private readonly ISessionContext _session;
        private readonly IUserService _users;
        private readonly IConfigService _config;
        private readonly ILanguageService _lang;
        private readonly IErrorService _error;
        public Filter(ISessionContext session, IUserService users, IConfigService config, ILanguageService lang, IErrorService error)
        { _session = session; _users = users; _config = config; _lang = lang; _error = error; }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var sid = _session.Sid;
            var ok = _config.GetInt("EnableKB") switch
            {
                1 => _users.UsrInt(sid, "IsRep") == 1,
                2 => sid > 0,
                3 => true,
                _ => false,
            };
            // CheckKB: DisplayError(3, lang("Accessdenied") & " " & lang("NoPermission") & ".")
            if (!ok)
                throw _error.Error(3, _lang.Lang("Accessdenied") + " " + _lang.Lang("NoPermission") + ".");
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}

/// <summary>CheckAdmin: requires the lhd_IsAdmin session flag (set after the admin password gate).</summary>
public sealed class CheckAdminAttribute : TypeFilterAttribute
{
    public CheckAdminAttribute() : base(typeof(Filter)) { }

    private sealed class Filter : IActionFilter
    {
        private readonly ISessionContext _session;
        private readonly ILanguageService _lang;
        private readonly IErrorService _error;
        public Filter(ISessionContext session, ILanguageService lang, IErrorService error)
        { _session = session; _lang = lang; _error = error; }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!_session.IsAdmin)
                throw _error.Error(3, _lang.Lang("Accessdenied") + " " + _lang.Lang("NoPermission") + ".");
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
