using System.Text;
using Dapper;

namespace LiberumHelpDesk.Web.Services;

/// <summary>
/// Ports DisplayHeader / DisplayFooter (public.asp). Returns raw HTML strings that views emit with
/// @Html.Raw, mirroring the original Response.Write subs. Output is intentionally unencoded for parity.
/// </summary>
public interface IChromeService
{
    string Header();
    string Footer();
}

public sealed class ChromeService : IChromeService
{
    private readonly Db _db;
    private readonly ISessionContext _session;
    private readonly IConfigService _config;
    private readonly IUserService _users;
    private readonly ILanguageService _lang;

    public ChromeService(Db db, ISessionContext session, IConfigService config, IUserService users, ILanguageService lang)
    {
        _db = db;
        _session = session;
        _config = config;
        _users = users;
        _lang = lang;
    }

    private string L(string key) => _lang.Lang(key);

    // DisplayHeader(cnnDB, sid)
    public string Header()
    {
        var sid = _session.Sid;
        var sb = new StringBuilder();
        sb.Append("<center><table width=\"500\"><tr><td valign=\"top\" align=\"left\"><font size=\"-1\">");

        var isRep = _users.UsrInt(sid, "IsRep") == 1;
        if (isRep)
        {
            var closeStatus = _config.GetInt("CloseStatus");
            var total = _db.Connection.ExecuteScalar<long>(
                "SELECT count(*) AS total FROM problems WHERE rep = @sid AND status <> @cs",
                new { sid, cs = closeStatus });
            sb.Append("<b>").Append(L("UserName")).Append(":</b> ").Append(_users.UsrString(sid, "uid"))
              .Append("<br /><b>").Append(L("Problems")).Append(":</b> <a href=\"view.asp\">").Append(total).Append("</a>");
        }
        else
        {
            var uid = _users.UsrString(sid, "uid");
            sb.Append("<b>").Append(L("UserName")).Append(":</b> ").Append(uid);
            var recent = _db.Connection.QueryFirstOrDefault(
                "SELECT id, title FROM problems WHERE uid = @uid ORDER BY id DESC LIMIT 1", new { uid });
            if (recent != null)
            {
                var d = (IDictionary<string, object>)recent;
                sb.Append("<br /><b>").Append(L("MostRecent")).Append(":</b> <a href=\"details.asp?id=")
                  .Append(Vb.Str(d["id"])).Append("\">").Append(Vb.Str(d["title"])).Append("</a>");
            }
        }
        sb.Append("</font></td><td valign=\"top\" align=\"right\"><font size=\"-1\">");

        // Session("IsRep")/Session("IsAdmin") (non-lhd keys) are never set in the original => always false.
        if (isRep)
        {
            sb.Append(L("Supportreploggedin")).Append(".<br />");
            sb.Append("<i><a href=\"../admin/\">").Append(L("HelpDeskAdministration")).Append("</a></i>");
        }
        else
        {
            sb.Append(L("NormalUser"));
        }
        sb.Append("</font></td></tr></table></center>");
        return sb.ToString();
    }

    // DisplayFooter(cnnDB, sid)
    public string Footer()
    {
        var sid = _session.Sid;
        var baseUrl = _config.GetString("BaseURL");
        var sb = new StringBuilder();

        if (sid != 0 && _users.Exists(sid))
        {
            sb.Append("<p><div align=\"center\">");
            if (_users.UsrInt(sid, "IsRep") > 0)
            {
                sb.Append("<a href=\"").Append(baseUrl).Append("/user\">").Append(L("UserMenu")).Append("</a> | ")
                  .Append("<a href=\"").Append(baseUrl).Append("/rep\">").Append(L("RepMenu")).Append("</a> | ");
            }
            else
            {
                sb.Append("<a href=\"").Append(baseUrl).Append("/user\">").Append(L("Menu")).Append("</a> | ");
            }
            if (_config.GetInt("UseInoutBoard") == 1)
                sb.Append("<a href=\"").Append(baseUrl).Append("/inout/default.asp\">").Append(L("InOutBoard")).Append("</a> | ");
            sb.Append("<a href=\"").Append(baseUrl).Append("/logoff.asp\">").Append(L("LogOff")).Append("</a></div></p>");
        }

        sb.Append("<p><hr width=\"500\">\r\n<div align=\"center\"><font size=\"-1\">")
          .Append("<a href=\"http://www.liberum.org\">").Append(L("LiberumHelpDesk"))
          .Append("</a>, Copyright &copy; 2014 Doug Luxem. ").Append(L("Pleaseviewthe"))
          .Append(" <a href=\"").Append(baseUrl).Append("/license.html\">").Append(L("license")).Append("</a>.")
          .Append("</font></div></p>");
        return sb.ToString();
    }
}
