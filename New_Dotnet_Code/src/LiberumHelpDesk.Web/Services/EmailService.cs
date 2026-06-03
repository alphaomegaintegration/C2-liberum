using System.Text;
using Dapper;
using MailKit.Net.Smtp;
using MimeKit;

namespace LiberumHelpDesk.Web.Services;

/// <summary>Low-level transport (ports SendMail/SendMailHTML). EmailType 0 = no-op; 1–5 collapse to SMTP.</summary>
public interface IEmailSender
{
    void Send(string toAddr, string fromAddr, string fromName, string subject, string body);
}

/// <summary>
/// All five original EmailType providers (CDONTS/JMail/ASPEmail/ASPMail/CDOSYS) map to one MailKit SMTP
/// send. Errors are swallowed (the original wrapped sends in On Error Resume Next). Parity scope is the
/// subject + token-substituted body, not SMTP wire format.
/// </summary>
public sealed class MailKitEmailSender : IEmailSender
{
    private readonly IConfigService _config;
    public MailKitEmailSender(IConfigService config) => _config = config;

    public void Send(string toAddr, string fromAddr, string fromName, string subject, string body)
    {
        if (_config.GetInt("EmailType") == 0) return; // disabled
        var server = _config.GetString("SMTPServer");
        if (string.IsNullOrEmpty(server)) return;

        try
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(fromName, string.IsNullOrEmpty(fromAddr) ? "noreply@localhost" : fromAddr));
            msg.To.Add(MailboxAddress.Parse(string.IsNullOrEmpty(toAddr) ? "unknown@localhost" : toAddr));
            msg.Subject = subject;
            msg.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient { Timeout = 5000 };
            client.Connect(server, 25, MailKit.Security.SecureSocketOptions.None);
            client.Send(msg);
            client.Disconnect(true);
        }
        catch
        {
            // Swallowed, matching the original On Error Resume Next behaviour.
        }
    }
}

/// <summary>Port of eMessage (public.asp): loads a template, substitutes [tokens], sends.</summary>
public interface IEmailService
{
    void EMessage(string eType, int id, string toAddr);
}

public sealed class EmailService : IEmailService
{
    private readonly Db _db;
    private readonly IConfigService _config;
    private readonly IDateService _dates;
    private readonly IErrorService _error;
    private readonly ILanguageService _lang;
    private readonly IEmailSender _sender;

    public EmailService(Db db, IConfigService config, IDateService dates, IErrorService error,
        ILanguageService lang, IEmailSender sender)
    {
        _db = db;
        _config = config;
        _dates = dates;
        _error = error;
        _lang = lang;
        _sender = sender;
    }

    public void EMessage(string eType, int id, string toAddr)
    {
        var tmpl = _db.Connection.QueryFirstOrDefault(
            "SELECT subject, body FROM tblEmailMsg WHERE type = @t", new { t = eType });
        if (tmpl is null)
            throw _error.Error(3, _lang.Lang("Nomessageoftype") + " " + eType + " " + _lang.Lang("wasfoundinthedatabase") + ".");

        var td = (IDictionary<string, object>)tmpl;
        var subject = Vb.Str(td["subject"]);
        var body = Vb.Str(td["body"]);

        // Flattened JOINs (Access nested parens removed) — same semantics.
        var prob = _db.Connection.QueryFirstOrDefault(
            "SELECT p.id, p.uid, p.uemail, p.uphone, p.ulocation, d.dname, p.start_date, p.due_date, " +
            "p.status, s.sname, p.close_date, pri.pname, c.cname, p.rep, p.title, p.solution, p.description " +
            "FROM problems p " +
            "JOIN departments d ON p.department = d.department_id " +
            "JOIN status s ON p.status = s.status_id " +
            "JOIN priority pri ON p.priority = pri.priority_id " +
            "JOIN categories c ON p.category = c.category_id " +
            "WHERE p.id = @id", new { id });

        if (prob is null)
            throw _error.Error(3, _lang.Lang("Problem") + " " + id + " " + _lang.Lang("doesnotexist") + ". " + _lang.Lang("Cannotsendmail") + ".");

        var p = (IDictionary<string, object>)prob;
        var repId = Vb.CInt(p["rep"]);

        var rep = _db.Connection.QueryFirstOrDefault(
            "SELECT uid, email1, fname FROM tblUsers WHERE sid = @rep", new { rep = repId });
        var rd = rep is null ? new Dictionary<string, object>() : (IDictionary<string, object>)rep;

        var user = _db.Connection.QueryFirstOrDefault(
            "SELECT fname FROM tblUsers WHERE uid = @uid", new { uid = Vb.Str(p["uid"]) });
        var ud = user is null ? new Dictionary<string, object>() : (IDictionary<string, object>)user;

        var notesSql = eType.Contains("rep")
            ? "SELECT * FROM tblNotes WHERE id = @id ORDER BY addDate ASC"
            : "SELECT * FROM tblNotes WHERE id = @id AND private = 0 ORDER BY addDate ASC";
        var noteRows = _db.Connection.Query(notesSql, new { id }).ToList();

        string fromAddr, fromName;
        if (eType.Contains("user"))
        {
            fromAddr = rd.TryGetValue("email1", out var ea) ? Vb.Str(ea) : "";
            fromName = rd.TryGetValue("fname", out var fn) ? Vb.Str(fn) : "";
        }
        else
        {
            fromAddr = _config.GetString("HDReply");
            fromName = _config.GetString("HDName");
        }

        var notes = new StringBuilder();
        if (noteRows.Count > 0)
        {
            foreach (var r in noteRows)
            {
                var nd = (IDictionary<string, object>)r;
                if (notes.Length > 0) notes.Append('\n');
                notes.Append('[').Append(Vb.Str(nd["addDate"])).Append(" - ").Append(Vb.Str(nd["uid"])).Append(']').Append('\n');
                notes.Append(Vb.Str(nd["note"])).Append('\n');
            }
        }
        else notes.Append(' ');

        var baseUrl = _config.GetString("BaseURL");
        string Get(string k) => p.TryGetValue(k, out var v) ? Vb.Str(v) : "";

        var tokens = new (string Token, string Value)[]
        {
            ("[problemid]", Get("id")),
            ("[title]", Get("title")),
            ("[description]", Get("description")),
            ("[status]", Get("sname")),
            ("[priority]", Get("pname")),
            ("[startdate]", _dates.DisplayDate(p["start_date"], true)),
            ("[closedate]", _dates.DisplayDate(p.TryGetValue("close_date", out var cd) ? cd : null, true)),
            ("[duedate]", _dates.DisplayDate(p.TryGetValue("due_date", out var dd) ? dd : null, false)),
            ("[category]", Get("cname")),
            ("[department]", Get("dname")),
            ("[phone]", Get("uphone")),
            ("[location]", Get("ulocation")),
            ("[solution]", Get("solution")),
            ("[baseurl]", baseUrl),
            ("[uid]", Get("uid")),
            ("[ufname]", ud.TryGetValue("fname", out var ufn) ? Vb.Str(ufn) : ""),
            ("[uemail]", Get("uemail")),
            ("[rid]", rd.TryGetValue("uid", out var ru) ? Vb.Str(ru) : ""),
            ("[rfname]", rd.TryGetValue("fname", out var rfn) ? Vb.Str(rfn) : ""),
            ("[remail]", rd.TryGetValue("email1", out var re) ? Vb.Str(re) : ""),
            ("[uurl]", baseUrl + "/user/view.asp?id=" + id),
            ("[rurl]", baseUrl + "/rep/view.asp?id=" + id),
            ("[notes]", notes.ToString()),
            ("[u_title]", Uri.EscapeDataString(Get("title"))),
            ("[u_rfname]", Uri.EscapeDataString(rd.TryGetValue("fname", out var rfn2) ? Vb.Str(rfn2) : "")),
        };

        foreach (var (token, value) in tokens)
        {
            body = body.Replace(token, value);
            subject = subject.Replace(token, value);
        }

        _sender.Send(toAddr, fromAddr, fromName, subject, body);
    }
}
