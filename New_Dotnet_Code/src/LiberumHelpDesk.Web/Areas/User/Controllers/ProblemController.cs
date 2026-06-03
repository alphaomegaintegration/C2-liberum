using Dapper;
using LiberumHelpDesk.Web.Filters;
using LiberumHelpDesk.Web.Models.ViewModels;
using LiberumHelpDesk.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LiberumHelpDesk.Web.Areas.User.Controllers;

/// <summary>Ports user/new.asp, postnew.asp, details.asp, update.asp, view.asp.</summary>
[Area("User")]
[Route("User/Problem")]
public sealed class ProblemController : Controller
{
    private readonly Db _db;
    private readonly IConfigService _config;
    private readonly ISessionContext _session;
    private readonly IUserService _users;
    private readonly ILanguageService _lang;
    private readonly IDateService _dates;
    private readonly IKeyService _keys;
    private readonly IErrorService _error;
    private readonly IEmailService _email;

    public ProblemController(Db db, IConfigService config, ISessionContext session, IUserService users,
        ILanguageService lang, IDateService dates, IKeyService keys, IErrorService error, IEmailService email)
    {
        _db = db; _config = config; _session = session; _users = users; _lang = lang;
        _dates = dates; _keys = keys; _error = error; _email = email;
    }

    private void SetChrome() => ViewData["Title"] = _lang.Lang("HelpDesk");

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

    // user/new.asp
    [HttpGet("New")]
    [CheckUser]
    public IActionResult New()
    {
        SetChrome();
        ViewData["Title"] = _lang.Lang("HelpDesk") + " - " + _lang.Lang("NewProblem"); // new.asp title
        var sid = _session.Sid;
        return View(new ProblemNewVm
        {
            Uid = _users.UsrString(sid, "uid"),
            Uemail = _users.UsrString(sid, "email1"),
            Ulocation = _users.UsrString(sid, "location1"),
            Uphone = _users.UsrString(sid, "phone"),
            UserDepartment = _users.UsrInt(sid, "department"),
            DefaultPriority = _config.GetInt("DefaultPriority"),
            DueDate = _dates.DisplayDate(DateTime.Now.AddDays(1), false),
            DateFormat = _users.UsrString(sid, "dateformat"),
            Departments = Opts("SELECT department_id, dname FROM departments WHERE department_id > 0 ORDER BY dname COLLATE NOCASE ASC"),
            Categories = Opts("SELECT category_id, cname FROM categories WHERE category_id > 0 ORDER BY cname COLLATE NOCASE ASC"),
            Priorities = Opts("SELECT priority_id, pname FROM priority WHERE priority_id > 0 ORDER BY priority_id ASC"),
        });
    }

    // user/postnew.asp
    [HttpPost("PostNew")]
    [CheckUser]
    public IActionResult PostNew()
    {
        SetChrome();
        var sid = _session.Sid;
        var f = Request.Form;

        var uid = f["uid"].ToString();
        var uemail = f["uemail"].ToString();
        var uphone = f["uphone"].ToString();
        var ulocation = f["ulocation"].ToString();
        var category = Vb.CInt(f["category"].ToString());
        var department = Vb.CInt(f["department"].ToString());
        var title = f["title"].ToString();
        var description = f["description"].ToString();
        var kb = 0;
        var duedate = _dates.ConvertFormattedDate(f["duedate"].ToString());
        var priority = Vb.CInt(f["priority"].ToString());

        if (!uemail.Contains('@')) throw _error.Error(1, _lang.Lang("Emailaddress"));
        if (category == 0) throw _error.Error(1, _lang.Lang("Category"));
        if (department == 0) throw _error.Error(1, _lang.Lang("Department"));
        if (priority == 0) throw _error.Error(1, _lang.Lang("Priority"));
        if (duedate is null) throw _error.Error(1, _lang.Lang("DueDate"));
        if (title.Length == 0) throw _error.Error(1, _lang.Lang("Title"));
        if (title.Length > 255) title = title.Trim()[..255];
        if (description.Length == 0) throw _error.Error(1, _lang.Lang("Description"));

        var status = _config.GetInt("DefaultStatus");
        var startDate = DateTime.Now;

        var dname = _db.Connection.ExecuteScalar<string>(
            "SELECT dname FROM departments WHERE department_id = @id", new { id = department });
        var cat = (IDictionary<string, object>)_db.Connection.QueryFirst(
            "SELECT cname, rep_id FROM categories WHERE category_id = @id", new { id = category });
        var cname = Vb.Str(cat["cname"]);
        var rep = Vb.CInt(cat["rep_id"]);
        var pname = _db.Connection.ExecuteScalar<string>(
            "SELECT pname FROM priority WHERE priority_id = @id", new { id = priority });

        var id = _keys.GetUnique("problems");

        uemail = uemail.Trim(); if (uemail.Length > 50) uemail = uemail[..50];
        uphone = uphone.Trim(); if (uphone.Length > 50) uphone = uphone[..50];
        ulocation = ulocation.Trim(); if (ulocation.Length > 50) ulocation = ulocation[..50];

        _db.Connection.Execute(
            "INSERT INTO problems (id, uid, uemail, uphone, ulocation, entered_by, category, department, " +
            "title, description, priority, status, start_date, due_date, rep, time_spent, kb) VALUES " +
            "(@id, @uid, @uemail, @uphone, @ulocation, @entered_by, @category, @department, @title, " +
            "@description, @priority, @status, @start_date, @due_date, @rep, 0, @kb)",
            new
            {
                id, uid, uemail, uphone, ulocation, entered_by = sid, category, department, title, description,
                priority, status, start_date = startDate, due_date = duedate, rep, kb
            });

        _email.EMessage("usernew", id, uemail);
        _email.EMessage("repnew", id, _users.UsrString(rep, "email1"));
        if (priority >= _config.GetInt("EnablePager") && _users.UsrString(rep, "email2").Length > 0)
            _email.EMessage("reppager", id, _users.UsrString(rep, "email2"));

        return View(new ProblemSubmittedVm
        {
            Id = id, Uid = uid, Uemail = uemail, Uphone = uphone, Ulocation = ulocation,
            StartDate = _dates.DisplayDate(startDate, true), DueDate = _dates.DisplayDate(duedate, false),
            Dname = dname ?? "", Cname = cname, Pname = pname ?? "",
            RepEmail = _users.UsrString(rep, "email1"), RepFname = _users.UsrString(rep, "fname"),
            Title = title, Description = description,
        });
    }

    // user/details.asp
    [HttpGet("Details")]
    [HttpPost("Details")]
    [CheckUser]
    public IActionResult Details()
    {
        SetChrome();
        ViewData["Title"] = _lang.Lang("HelpDesk") + " - " + _lang.Lang("ProblemDetails"); // details.asp title
        var sid = _session.Sid;
        var uid = _users.UsrString(sid, "uid");

        var id = Vb.CInt(Request.Query["id"].ToString());
        if (id == 0)
        {
            var formId = Request.HasFormContentType ? Request.Form["id"].ToString() : "";
            if (formId.Length == 0) throw _error.Error(3, _lang.Lang("AproblemIDnumberisrequired"));
            id = Vb.CInt(formId);
        }

        var prob = _db.Connection.QueryFirstOrDefault(
            "SELECT p.id, p.uid, p.uemail, p.uphone, p.ulocation, d.dname, p.start_date, p.due_date, p.status, " +
            "s.sname, p.close_date, c.cname, pri.pname, r.uid AS ruid, r.email1 AS remail, r.fname, p.title, " +
            "p.solution, p.description FROM problems p " +
            "JOIN departments d ON p.department = d.department_id " +
            "JOIN status s ON p.status = s.status_id " +
            "JOIN priority pri ON p.priority = pri.priority_id " +
            "JOIN tblUsers r ON p.rep = r.sid " +
            "JOIN categories c ON p.category = c.category_id " +
            "WHERE p.id = @id AND p.uid = @uid COLLATE NOCASE", new { id, uid });

        if (prob is null)
            throw _error.Error(3, _lang.Lang("ProblemID") + "&nbsp;" + id + "&nbsp;" + _lang.Lang("wasnotfoundinthedatabase") + ".");

        var p = (IDictionary<string, object>)prob;
        var closeStatus = _config.GetInt("CloseStatus");
        var isClosed = Vb.CInt(p["status"]) == closeStatus;

        var notes = new List<ProblemNoteVm>();
        foreach (var r in _db.Connection.Query(
            "SELECT addDate, uid, [note] FROM tblNotes WHERE id = @id AND private = 0 ORDER BY addDate ASC", new { id }))
        {
            var nd = (IDictionary<string, object>)r;
            notes.Add(new ProblemNoteVm
            {
                DateDisplay = _dates.DisplayDate(nd["addDate"], true),
                Uid = Vb.Str(nd["uid"]),
                NoteHtml = Vb.Str(nd["note"]).Replace("\r\n", "<br />").Replace("\n", "<br />"),
            });
        }

        return View(new ProblemDetailsVm
        {
            Id = id, Uid = uid, Uemail = Vb.Str(p["uemail"]), Uphone = Vb.Str(p["uphone"]), Ulocation = Vb.Str(p["ulocation"]),
            StartDate = _dates.DisplayDate(p["start_date"], true), DueDate = _dates.DisplayDate(p["due_date"], false),
            CloseDate = _dates.DisplayDate(p["close_date"], true), IsClosed = isClosed,
            Dname = Vb.Str(p["dname"]), Cname = Vb.Str(p["cname"]), Pname = Vb.Str(p["pname"]),
            RepEmail = Vb.Str(p["remail"]), RepFname = Vb.Str(p["fname"]), Sname = Vb.Str(p["sname"]),
            Title = Vb.Str(p["title"]), Description = Vb.Str(p["description"]), Solution = Vb.Str(p["solution"]),
            Notes = notes,
        });
    }

    // user/update.asp
    [HttpPost("Update")]
    [CheckUser]
    public IActionResult Update()
    {
        SetChrome();
        var sid = _session.Sid;
        var id = Vb.CInt(Request.Form["id"].ToString());
        var notes = Request.Form["notes"].ToString();
        if (notes.Length == 0) throw _error.Error(1, _lang.Lang("AdditionalNotes"));

        var rep = Vb.CInt(_db.Connection.ExecuteScalar<object?>(
            "SELECT rep FROM problems WHERE id = @id", new { id }));

        _db.Connection.Execute(
            "INSERT INTO tblNotes (id, [note], addDate, uid, private) VALUES (@id, @note, @addDate, @uid, 0)",
            new { id, note = notes, addDate = DateTime.Now, uid = _users.UsrString(sid, "uid") });

        _email.EMessage("repupdate", id, _users.UsrString(rep, "email1"));

        ViewData["Id"] = id;
        return View();
    }

    // user/print.asp — printer-friendly view (no auth filter in the original; any id by number).
    [HttpGet("Print")]
    public IActionResult Print()
    {
        var id = Vb.CInt(Request.Query["id"].ToString());
        var prob = _db.Connection.QueryFirstOrDefault(
            "SELECT p.id, p.uid, p.uemail, p.uphone, p.ulocation, d.dname, p.start_date, p.status, s.sname, " +
            "p.close_date, c.cname, r.uid AS ruid, r.email1 AS remail, r.fname AS rname, p.title, p.solution, p.description, pri.pname " +
            "FROM problems p JOIN departments d ON p.department=d.department_id JOIN status s ON p.status=s.status_id " +
            "JOIN tblUsers r ON p.rep=r.sid JOIN priority pri ON p.priority=pri.priority_id JOIN categories c ON p.category=c.category_id " +
            "WHERE p.id=@id", new { id });
        if (prob is null)
            throw _error.Error(3, _lang.Lang("ProblemID") + "&nbsp;" + id + "&nbsp;" + _lang.Lang("wasnotfoundinthedatabase") + ".");

        var pr = (IDictionary<string, object>)prob;
        var isClosed = Vb.CInt(pr["status"]) == _config.GetInt("CloseStatus");
        var notes = new List<PrintNote>();
        foreach (var r in _db.Connection.Query("SELECT addDate, uid, [note] FROM tblNotes WHERE id=@id AND private=0 ORDER BY addDate ASC", new { id }))
        {
            var nd = (IDictionary<string, object>)r;
            notes.Add(new PrintNote { Header = "[" + _dates.DisplayDate(nd["addDate"], true) + " - " + Vb.Str(nd["uid"]) + "]", NoteHtml = Vb.FormatBlock(nd["note"]) });
        }

        ViewData["Title"] = _lang.Lang("HelpDesk") + " - " + _lang.Lang("ProblemDetails");
        return View(new PrintProblemVm
        {
            Id = id, Uid = Vb.Str(pr["uid"]), Uemail = Vb.Str(pr["uemail"]), Uphone = Vb.Str(pr["uphone"]), Ulocation = Vb.Str(pr["ulocation"]),
            Dname = Vb.Str(pr["dname"]), Cname = Vb.Str(pr["cname"]), Sname = Vb.Str(pr["sname"]), Pname = Vb.Str(pr["pname"]), Rname = Vb.Str(pr["rname"]), Remail = Vb.Str(pr["remail"]),
            Title = Vb.Str(pr["title"]), StartDate = _dates.DisplayDate(pr["start_date"], true), CloseDate = _dates.DisplayDate(pr["close_date"], true),
            IsClosed = isClosed, DescriptionHtml = Vb.FormatBlock(pr["description"]), SolutionHtml = isClosed ? Vb.FormatBlock(pr["solution"]) : "",
            Notes = notes,
        });
    }

    // user/view.asp
    [HttpGet("View")]
    [CheckUser]
    public new IActionResult View()
    {
        SetChrome();
        ViewData["Title"] = _lang.Lang("HelpDesk") + " - " + _lang.Lang("ProblemList"); // view.asp title
        var sid = _session.Sid;
        var uid = _users.UsrString(sid, "uid");

        var id = Vb.CInt(Request.Query["id"].ToString());
        var sort = Vb.CInt(Request.Query["sort"].ToString());
        var order = Request.Query["order"].ToString().Length > 0 ? Vb.CInt(Request.Query["order"].ToString()) : 0;

        var where = "WHERE p.uid = @uid COLLATE NOCASE" + (id != 0 ? " AND p.id = @id" : "");

        // Sort + next-click toggle flags (faithful to view.asp).
        string orderBy;
        int idOrder = 0, titleOrder = 0, repOrder = 0, dateOrder = 0, statusOrder = 0;
        switch (sort)
        {
            case 2: orderBy = "p.title " + (order == 0 ? "ASC" : "DESC"); titleOrder = order == 0 ? 1 : 0; break;
            case 3: orderBy = "r.fname " + (order == 0 ? "ASC" : "DESC"); repOrder = order == 0 ? 1 : 0; break;
            case 4: orderBy = "p.start_date " + (order == 0 ? "DESC" : "ASC"); dateOrder = order == 0 ? 1 : 0; break;
            case 5: orderBy = "p.status " + (order == 0 ? "DESC" : "ASC"); statusOrder = order == 0 ? 1 : 0; break;
            default: orderBy = "p.id " + (order == 0 ? "DESC" : "ASC"); idOrder = order == 0 ? 1 : 0; break;
        }

        var rows = _db.Connection.Query(
            "SELECT p.id, p.title, p.start_date, r.fname, r.email1 AS remail, s.sname FROM problems p " +
            "JOIN tblUsers r ON p.rep = r.sid JOIN status s ON p.status = s.status_id " +
            where + " ORDER BY " + orderBy, new { uid, id }).ToList();

        var num = Request.Query["num"].ToString().Length > 0 ? Vb.CInt(Request.Query["num"].ToString()) : 25;
        var start = Request.Query["start"].ToString().Length > 0 ? Vb.CInt(Request.Query["start"].ToString()) : 1;
        if (num <= 0) num = 25;
        if (start <= 0) start = 1;

        var windowEnd = start + num - 1;
        var window = rows.Skip(start - 1).Take(num).Select(r =>
        {
            var d = (IDictionary<string, object>)r;
            return new ProblemListRow
            {
                Id = Vb.CInt(d["id"]), Title = Vb.Str(d["title"]),
                StartDate = _dates.DisplayDate(d["start_date"], false),
                Fname = Vb.Str(d["fname"]), Remail = Vb.Str(d["remail"]), Sname = Vb.Str(d["sname"]),
            };
        }).ToList();

        var showNext = rows.Count > windowEnd;
        return View(new ProblemListVm
        {
            Uid = uid, Rows = window, Sort = sort, Order = order, Start = start, NumToDisplay = num,
            IdOrder = idOrder, TitleOrder = titleOrder, RepOrder = repOrder, DateOrder = dateOrder, StatusOrder = statusOrder,
            ShowPager = start > 1 || showNext, ShowPrev = start > 1, ShowNext = showNext,
            StartP = Math.Max(1, start - num), StartN = start + num,
        });
    }
}
