using System.Globalization;
using Dapper;
using LiberumHelpDesk.Web.Filters;
using LiberumHelpDesk.Web.Models.ViewModels;
using LiberumHelpDesk.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LiberumHelpDesk.Web.Areas.Rep.Controllers;

/// <summary>Ports rep/search.asp, rep/results.asp, rep/selectuser.asp.</summary>
[Area("Rep")]
public sealed class SearchController : Controller
{
    private readonly Db _db;
    private readonly IConfigService _config;
    private readonly ISessionContext _session;
    private readonly IUserService _users;
    private readonly ILanguageService _lang;
    private readonly IDateService _dates;

    public SearchController(Db db, IConfigService config, ISessionContext session, IUserService users, ILanguageService lang, IDateService dates)
    {
        _db = db; _config = config; _session = session; _users = users; _lang = lang; _dates = dates;
    }

    private List<Opt> Opts(string sql)
    {
        var list = new List<Opt>();
        foreach (var r in _db.Connection.Query(sql))
        {
            var d = (IDictionary<string, object>)r;
            list.Add(new Opt(Vb.CInt(d.Values.First()), Vb.Str(d.Values.Skip(1).First())));
        }
        return list;
    }

    // rep/search.asp
    [HttpGet("/Rep/Search")]
    [CheckRep]
    public IActionResult Index()
    {
        var now = DateTime.Now;
        var startMonth = now.Month - 1;
        var startYear = now.Year;
        if (startMonth == 0) { startMonth = 12; startYear -= 1; }

        ViewData["Title"] = _lang.Lang("HelpDesk") + " - " + _lang.Lang("ProblemSearch");
        return View(new RepSearchVm
        {
            Reps = Opts("SELECT sid, uid FROM tblUsers WHERE IsRep = 1 AND RepAccess <> 2 AND sid > 0 ORDER BY uid COLLATE NOCASE ASC"),
            Categories = Opts("SELECT category_id, cname FROM categories WHERE category_id > 0 ORDER BY cname COLLATE NOCASE ASC"),
            Departments = Opts("SELECT department_id, dname FROM departments WHERE department_id > 0 ORDER BY dname COLLATE NOCASE ASC"),
            Statuses = Opts("SELECT status_id, sname FROM status WHERE status_id > 0 ORDER BY status_id ASC"),
            Priorities = Opts("SELECT priority_id, pname FROM priority WHERE priority_id > 0 ORDER BY priority_id ASC"),
            SMonth = startMonth, SDay = now.Day, SYear = startYear,
            EMonth = now.Month, EDay = now.Day, EYear = now.Year, CurrentYear = now.Year,
        });
    }

    // rep/results.asp
    [HttpPost("/Rep/Results")]
    [CheckRep]
    public IActionResult Results()
    {
        var f = Request.Form;
        var uid = f["uid"].ToString().Trim();
        var rep = Vb.CInt(f["rep"].ToString());
        var category = Vb.CInt(f["category"].ToString());
        var department = Vb.CInt(f["department"].ToString());
        var status = Vb.CInt(f["status"].ToString());
        var priority = Vb.CInt(f["priority"].ToString());
        var order = Vb.CInt(f["order"].ToString());
        var idStr = f["id"].ToString().Trim();
        var id = int.TryParse(idStr, out var pid) ? pid : 0;

        // Date range: hidden start_date/end_date (paging) or the dropdowns.
        DateTime start = ParseRange(f["start_date"].ToString(), f["s_month"], f["s_day"], f["s_year"], false);
        DateTime end = ParseRange(f["end_date"].ToString(), f["e_month"], f["e_day"], f["e_year"], true);

        var p = new DynamicParameters();
        p.Add("start", start); p.Add("end", end);
        var where = "p.start_date > @start AND p.start_date < @end";
        if (uid.Length > 0) { where += " AND p.uid = @uid COLLATE NOCASE"; p.Add("uid", uid); }
        if (id != 0) { where += " AND p.id = @id"; p.Add("id", id); }
        if (rep != 0) { where += " AND p.rep = @rep"; p.Add("rep", rep); }
        if (category != 0) { where += " AND p.category = @category"; p.Add("category", category); }
        if (department != 0) { where += " AND p.department = @department"; p.Add("department", department); }
        if (status != 0)
        {
            if (status > 0) { where += " AND p.status = @status"; p.Add("status", status); }
            else { where += " AND p.status <> @cs"; p.Add("cs", _config.GetInt("CloseStatus")); }
        }
        if (priority != 0) { where += " AND p.priority = @priority"; p.Add("priority", priority); }

        // Keywords (non-FreeText LIKE path, same as KB).
        if (f["keywords"].ToString().Length > 0)
        {
            var words = f["keywords"].ToString().Trim().Split(' ');
            for (var i = 0; i < words.Length; i++) p.Add("kw" + i, "%" + words[i] + "%");
            var titleOn = f["title"].ToString() == "on";
            var descOn = f["description"].ToString() == "on";
            var solOn = f["solution"].ToString() == "on";
            var allOff = !titleOn && !descOn && !solOn;
            string Field(string col) => "(" + string.Join(" AND ", words.Select((_, i) => col + " LIKE @kw" + i)) + ")";
            var w2 = "";
            if (titleOn || allOff) { if (w2.Length < 1) w2 = " AND (" + Field("title"); }
            if (descOn || allOff) { var d = Field("description"); w2 = w2.Length < 1 ? " AND (" + d : w2 + " OR " + d; }
            if (solOn || allOff) { var s = Field("solution"); w2 = w2.Length < 1 ? " AND (" + s : w2 + " OR " + s; }
            w2 += ")";
            where += w2;
        }

        var orderBy = order switch { 2 => "p.uid ASC", 3 => "r.uid ASC", 4 => "p.status ASC", _ => "p.id ASC" };
        var rows = _db.Connection.Query(
            "SELECT p.id, p.title, p.start_date, p.uid, p.uemail, r.uid AS ruid, s.sname FROM problems p " +
            "JOIN tblUsers r ON p.rep = r.sid JOIN status s ON p.status = s.status_id WHERE " + where +
            " ORDER BY " + orderBy + " LIMIT 100", p).ToList();

        // Faithful results.asp pager: CInt passed through verbatim, default only when the field is absent
        // (no >0 clamp); a row at 1-based position i shows iff start <= i <= start + num - 1.
        var num = f["num"].ToString().Length > 0 ? Vb.CInt(f["num"].ToString()) : 25;
        var start1 = f["start"].ToString().Length > 0 ? Vb.CInt(f["start"].ToString()) : 1;
        var windowEnd = start1 + num - 1;

        var window = rows.Where((_, idx) => idx + 1 >= start1 && idx + 1 <= windowEnd).Select(r =>
        {
            var d = (IDictionary<string, object>)r;
            return new RepResultRow
            {
                Id = Vb.CInt(d["id"]), Title = Vb.Str(d["title"]), Uid = Vb.Str(d["uid"]), Uemail = Vb.Str(d["uemail"]),
                Ruid = Vb.Str(d["ruid"]), StartDate = _dates.DisplayDate(d["start_date"], false), Sname = Vb.Str(d["sname"]),
            };
        }).ToList();

        var echo = new Dictionary<string, string>
        {
            ["uid"] = uid, ["id"] = idStr, ["rep"] = rep.ToString(), ["category"] = category.ToString(),
            ["department"] = department.ToString(), ["start_date"] = start.ToString("yyyy-MM-dd HH:mm:ss"),
            ["end_date"] = end.ToString("yyyy-MM-dd HH:mm:ss"), ["keywords"] = f["keywords"].ToString(),
            ["title"] = f["title"].ToString(), ["description"] = f["description"].ToString(), ["solution"] = f["solution"].ToString(),
            ["status"] = status.ToString(), ["priority"] = priority.ToString(), ["order"] = order.ToString(),
        };

        ViewData["Title"] = _lang.Lang("HelpDesk") + " - " + _lang.Lang("SearchResults");
        return View(new RepResultsVm
        {
            Rows = window, HasResults = window.Count > 0, Start = start1, Num = num,
            ShowPrev = start1 > 1, ShowNext = rows.Count > windowEnd, StartP = Math.Max(1, start1 - num), StartN = start1 + num,
            Echo = echo,
        });
    }

    private DateTime ParseRange(string hidden, Microsoft.Extensions.Primitives.StringValues m, Microsoft.Extensions.Primitives.StringValues day, Microsoft.Extensions.Primitives.StringValues y, bool endOfDay)
    {
        if (hidden.Length > 0 && DateTime.TryParse(hidden, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;
        var month = Vb.CInt(m.ToString());
        var year = Vb.CInt(y.ToString());
        var d = _dates.FixDay(month, Vb.CInt(day.ToString()), year);
        if (month < 1 || month > 12) month = 1;
        if (d < 1) d = 1;
        if (year < 1) year = DateTime.Now.Year;
        return endOfDay ? new DateTime(year, month, d, 23, 59, 59) : new DateTime(year, month, d, 0, 0, 0);
    }

    // rep/selectuser.asp — popup user picker.
    [HttpGet("/Rep/SelectUser")]
    [HttpPost("/Rep/SelectUser")]
    [CheckRep]
    public IActionResult SelectUser()
    {
        var searchName = (Request.HasFormContentType ? Request.Form["searchname"].ToString() : "").Trim();
        var posted = Request.HasFormContentType && Vb.CInt(Request.Form["postform"].ToString()) == 1 && searchName.Length >= 1;

        var users = new List<SelectUserRow>();
        if (posted)
        {
            foreach (var r in _db.Connection.Query(
                "SELECT sid, uid, firstname, lastname, location1 FROM tblUsers WHERE " +
                "uid LIKE @s OR firstname LIKE @s OR lastname LIKE @s ORDER BY lastname ASC",
                new { s = searchName + "%" }))
            {
                var d = (IDictionary<string, object>)r;
                users.Add(new SelectUserRow
                {
                    Sid = Vb.CInt(d["sid"]), Uid = Vb.Str(d["uid"]), Firstname = Vb.Str(d["firstname"]),
                    Lastname = Vb.Str(d["lastname"]), Location1 = Vb.Str(d["location1"]),
                });
            }
        }

        ViewData["Title"] = _lang.Lang("HelpDesk") + " - " + _lang.Lang("SelectUser");
        return View(new SelectUserVm { SearchName = searchName, Posted = posted, Users = users });
    }
}
