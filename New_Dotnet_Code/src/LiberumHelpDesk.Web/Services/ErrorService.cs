using System.Text;

namespace LiberumHelpDesk.Web.Services;

/// <summary>Thrown to short-circuit a request with a faithful DisplayError page (mirrors Response.End).</summary>
public sealed class LhdException : Exception
{
    public string Html { get; }
    public LhdException(string html) : base("LHD error page") => Html = html;
}

/// <summary>Ports DisplayError / TrapError (public.asp). Builds the exact red error-box markup.</summary>
public interface IErrorService
{
    /// <summary>DisplayError(eType, component). Returns an exception to throw (mirrors Response.End).</summary>
    LhdException Error(int eType, string component);

    string RenderTrap(int number, string description, string source);
}

public sealed class ErrorService : IErrorService
{
    private readonly ILanguageService _lang;
    public ErrorService(ILanguageService lang) => _lang = lang;

    public LhdException Error(int eType, string component)
    {
        var sb = new StringBuilder();
        // Non-debug wrapper (we always run Debug=false for parity).
        sb.Append("<html><head><title>ERROR</title></head><body>");
        sb.Append("<p><center><table width=\"200\"><tr><td bgcolor=\"red\" align=\"center\">")
          .Append("<b>ERROR</b></tr></td><tr><td bgcolor=\"#eeeeee\" align=\"center\">");

        switch (eType)
        {
            case 1: // Missing required field
                sb.Append("<b>").Append(component).Append("</b> ")
                  .Append("&nbsp;").Append(_lang.Lang("isarequiredfield")).Append(".<p>")
                  .Append("<i>").Append(_lang.Lang("PleasepresstheBACKbutton")).Append("</i></p>");
                break;
            case 2: // SQL error
                sb.Append(_lang.Lang("ASQLqueryhasfailed")).Append(". ");
                break;
            case 3: // Generic
                sb.Append(component);
                break;
        }

        sb.Append("</tr></td></table></center><p>&nbsp;</p></body></html>");
        return new LhdException(sb.ToString());
    }

    /// <summary>
    /// Builds the same DisplayError(3, component) red-box page as <see cref="Error"/> but without any
    /// dependency on <see cref="ILanguageService"/>. Used by Cfg/Usr, whose error path would otherwise
    /// form a ConfigService -> ErrorService -> LanguageService -> ConfigService DI cycle. The component
    /// here is the already-composed English message (the localized lang string is unavailable on this
    /// low-level path, an accepted minor deviation for an effectively unreachable condition).
    /// </summary>
    public static LhdException Generic(string component)
    {
        var sb = new StringBuilder();
        sb.Append("<html><head><title>ERROR</title></head><body>");
        sb.Append("<p><center><table width=\"200\"><tr><td bgcolor=\"red\" align=\"center\">")
          .Append("<b>ERROR</b></tr></td><tr><td bgcolor=\"#eeeeee\" align=\"center\">");
        sb.Append(component);
        sb.Append("</tr></td></table></center><p>&nbsp;</p></body></html>");
        return new LhdException(sb.ToString());
    }

    public string RenderTrap(int number, string description, string source)
    {
        var hex = number.ToString("X8");
        var sb = new StringBuilder();
        sb.Append("<p><center><table width=\"300\">");
        sb.Append("<tr><td bgcolor=\"red\" align=\"center\">");
        sb.Append("<B>Application Error</b></td></tr>");
        sb.Append("<tr><td bgcolor=\"#EEEEEE\" align=\"left\">");
        sb.Append("<b>Number: </b>").Append(number).Append(" (0x").Append(hex).Append(")<br />");
        sb.Append("<b>Source: </b>").Append(source).Append("<br />");
        sb.Append("<b>Description: </b>").Append(description).Append("<hr />");
        sb.Append("No more information is available.");
        sb.Append("<p>Contact your administrator or visit the Liberum Help Desk ");
        sb.Append("<a href=\"http://www.liberum.org\">website</a>.");
        sb.Append("</td></tr></table></center>");
        return sb.ToString();
    }
}
