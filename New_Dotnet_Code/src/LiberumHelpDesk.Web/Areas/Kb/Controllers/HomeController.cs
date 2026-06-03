using Dapper;
using LiberumHelpDesk.Web.Filters;
using LiberumHelpDesk.Web.Models.ViewModels;
using LiberumHelpDesk.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LiberumHelpDesk.Web.Areas.Kb.Controllers;

/// <summary>Ports kb/default.asp (search) and kb/details.asp.</summary>
[Area("Kb")]
public sealed class HomeController : Controller
{
    private readonly Db _db;
    private readonly IConfigService _config;
    private readonly ISessionContext _session;
    private readonly IDateService _dates;
    private readonly ILanguageService _lang;
    private readonly IErrorService _error;

    public HomeController(Db db, IConfigService config, ISessionContext session, IDateService dates, ILanguageService lang, IErrorService error)
    {
        _db = db; _config = config; _session = session; _dates = dates; _lang = lang; _error = error;
    }

    [HttpGet("/Kb")]
    [HttpPost("/Kb")]
    [CheckKb]
    public IActionResult Index()
    {
        ViewData["Title"] = _lang.Lang("HelpDesk") + " - " + _lang.Lang("KnowledgeBase");

        var f = Request.HasFormContentType ? Request.Form : null;
        var searched = f != null && f["search"].ToString() == "1" && f["keywords"].ToString().Length > 0;
        if (!searched)
            return View(new KbSearchVm { Searched = false });

        var keywords = f!["keywords"].ToString().Trim();
        var words = keywords.Split(' ');
        var titleOn = f["title"].ToString() == "on";
        var descOn = f["description"].ToString() == "on";
        var solOn = f["solution"].ToString() == "on";
        var allOff = !titleOn && !descOn && !solOn;

        var p = new DynamicParameters();
        p.Add("cs", _config.GetInt("CloseStatus"));
        for (var i = 0; i < words.Length; i++) p.Add("kw" + i, "%" + words[i] + "%");

        string Field(string col) => "(" + string.Join(" AND ", words.Select((_, i) => col + " LIKE @kw" + i)) + ")";

        string where2;
        if (_config.GetInt("KBFreeText") == 1)
        {
            // Faithful KBFreeText=1 branch: OR of FREETEXT(col,'keywords') across the selected fields.
            // SQLite has no FREETEXT, so this path errors (caught below and rendered as the trapped-error box),
            // matching the accepted "faithful error" envelope (the branch exists on a SQL Server backend).
            p.Add("ft", keywords);
            where2 = "";
            if (titleOn || allOff)
                where2 = where2.Length < 1 ? " AND (FREETEXT(title, @ft)" : where2 + " OR FREETEXT(title, @ft)";
            if (descOn || allOff)
                where2 = where2.Length < 1 ? " AND (FREETEXT(description, @ft)" : where2 + " OR FREETEXT(description, @ft)";
            if (solOn || allOff)
                where2 = where2.Length < 1 ? " AND (FREETEXT(solution, @ft)" : where2 + " OR FREETEXT(solution, @ft)";
            where2 += ")";
        }
        else
        {
            where2 = "";
            if (titleOn || allOff)
            {
                if (where2.Length < 1) where2 = " AND (" + Field("title"); // faithful quirk: no else branch for title
            }
            if (descOn || allOff)
            {
                var d = Field("description");
                where2 = where2.Length < 1 ? " AND (" + d : where2 + " OR " + d;
            }
            if (solOn || allOff)
            {
                var s = Field("solution");
                where2 = where2.Length < 1 ? " AND (" + s : where2 + " OR " + s;
            }
            where2 += ")";
        }

        var sql = "SELECT id, title, start_date, close_date FROM problems WHERE (kb=1 AND status=@cs)" + where2 + " ORDER BY start_date ASC";

        var results = new List<KbRow>();
        try
        {
            foreach (var r in _db.Connection.Query(sql, p))
            {
                var d = (IDictionary<string, object>)r;
                results.Add(new KbRow
                {
                    Id = Vb.CInt(d["id"]), Title = Vb.Str(d["title"]),
                    StartDate = _dates.DisplayDate(d["start_date"], false),
                    CloseDate = _dates.DisplayDate(d["close_date"], false),
                });
            }
        }
        catch (Microsoft.Data.Sqlite.SqliteException ex)
        {
            // Mirrors SQLQuery()'s On Error -> TrapError red box when a query fails (e.g. FREETEXT on SQLite).
            throw new LhdException(_error.RenderTrap(ex.SqliteErrorCode, ex.Message, "Microsoft.Data.Sqlite"));
        }
        return View(new KbSearchVm { Searched = true, Results = results });
    }

    [HttpGet("/Kb/Details")]
    [HttpPost("/Kb/Details")]
    [CheckKb]
    public IActionResult Details()
    {
        var id = Vb.CInt(Request.Query["id"].ToString());
        if (id == 0)
        {
            var formId = Request.HasFormContentType ? Request.Form["id"].ToString() : "";
            if (formId.Length == 0) throw _error.Error(3, "A problem ID number is required.");
            id = Vb.CInt(formId);
        }

        var cs = _config.GetInt("CloseStatus");
        var prob = _db.Connection.QueryFirstOrDefault(
            "SELECT p.id, d.dname, p.start_date, s.sname, p.close_date, c.cname, p.title, p.solution, p.description " +
            "FROM problems p JOIN departments d ON p.department=d.department_id JOIN status s ON p.status=s.status_id " +
            "JOIN tblUsers r ON p.rep=r.sid JOIN categories c ON p.category=c.category_id " +
            "WHERE p.id=@id AND p.kb=1 AND p.status=@cs", new { id, cs });

        if (prob is null)
            throw _error.Error(3, "Problem ID " + id + " was not found in the database.");

        var pr = (IDictionary<string, object>)prob;
        var solution = Vb.Str(_db.Connection.ExecuteScalar<object?>("SELECT solution FROM problems WHERE id=@id", new { id }));

        var notes = new List<KbNote>();
        foreach (var r in _db.Connection.Query("SELECT addDate, [note] FROM tblNotes WHERE id=@id AND private=0 ORDER BY addDate ASC", new { id }))
        {
            var nd = (IDictionary<string, object>)r;
            notes.Add(new KbNote { DateRaw = Vb.Str(nd["addDate"]), NoteHtml = Vb.Str(nd["note"]).Replace("\r\n", "<br />").Replace("\n", "<br />") });
        }

        ViewData["Title"] = _lang.Lang("HelpDesk") + " - " + _lang.Lang("ProblemDetails");
        return View(new KbDetailsVm
        {
            Id = id, StartDate = _dates.DisplayDate(pr["start_date"], true), CloseDate = _dates.DisplayDate(pr["close_date"], true),
            Cname = Vb.Str(pr["cname"]), Title = Vb.Str(pr["title"]), Description = Vb.Str(pr["description"]), Solution = solution,
            Notes = notes,
        });
    }

    // kb/print.asp — printer-friendly KB article.
    [HttpGet("/Kb/Print")]
    [CheckKb]
    public IActionResult Print()
    {
        var id = Vb.CInt(Request.Query["id"].ToString());
        var cs = _config.GetInt("CloseStatus");
        var prob = _db.Connection.QueryFirstOrDefault(
            "SELECT p.id, d.dname, p.start_date, s.sname, p.close_date, c.cname, p.title, p.solution, p.description " +
            "FROM problems p JOIN departments d ON p.department=d.department_id JOIN status s ON p.status=s.status_id " +
            "JOIN tblUsers r ON p.rep=r.sid JOIN categories c ON p.category=c.category_id " +
            "WHERE p.id=@id AND p.kb=1 AND p.status=@cs", new { id, cs });
        if (prob is null)
            throw _error.Error(3, "Problem ID " + id + " was not found in the database.");

        var pr = (IDictionary<string, object>)prob;
        var notes = new List<PrintNote>();
        foreach (var r in _db.Connection.Query("SELECT addDate, uid, [note] FROM tblNotes WHERE id=@id AND private=0 ORDER BY addDate ASC", new { id }))
        {
            var nd = (IDictionary<string, object>)r;
            // kb/print uses the raw stored addDate in the header (not DisplayDate).
            notes.Add(new PrintNote { Header = "[" + Vb.Str(nd["addDate"]) + " - " + Vb.Str(nd["uid"]) + "]", NoteHtml = Vb.FormatBlock(nd["note"]) });
        }

        ViewData["Title"] = _lang.Lang("HelpDesk") + " - " + _lang.Lang("ProblemDetails");
        return View(new PrintProblemVm
        {
            Id = id, Title = Vb.Str(pr["title"]), Cname = Vb.Str(pr["cname"]),
            StartDate = _dates.DisplayDate(pr["start_date"], true), CloseDate = _dates.DisplayDate(pr["close_date"], true),
            IsClosed = true, DescriptionHtml = Vb.FormatBlock(pr["description"]), SolutionHtml = Vb.FormatBlock(pr["solution"]),
            Notes = notes,
        });
    }
}
