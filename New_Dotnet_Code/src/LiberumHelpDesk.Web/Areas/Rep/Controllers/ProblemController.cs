using Dapper;
using LiberumHelpDesk.Web.Filters;
using LiberumHelpDesk.Web.Models.ViewModels;
using LiberumHelpDesk.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LiberumHelpDesk.Web.Areas.Rep.Controllers;

/// <summary>Ports rep/details.asp (display + update + reopen) and rep/view.asp.</summary>
[Area("Rep")]
[Route("Rep/Problem")]
public sealed class ProblemController : Controller
{
    private readonly Db _db;
    private readonly IConfigService _config;
    private readonly ISessionContext _session;
    private readonly IUserService _users;
    private readonly ILanguageService _lang;
    private readonly IDateService _dates;
    private readonly IErrorService _error;
    private readonly IEmailService _email;
    private readonly IKeyService _keys;

    public ProblemController(Db db, IConfigService config, ISessionContext session, IUserService users,
        ILanguageService lang, IDateService dates, IErrorService error, IEmailService email, IKeyService keys)
    {
        _db = db; _config = config; _session = session; _users = users; _lang = lang;
        _dates = dates; _error = error; _email = email; _keys = keys;
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

    // rep/details.asp — display, update (update=1) and reopen (reopen=1).
    [HttpGet("Details")]
    [HttpPost("Details")]
    public IActionResult Details()
    {
        var conn = _db.Connection;
        var sid = _session.Sid;

        var reopen = Vb.CInt(Request.Query["reopen"].ToString()) == 1;

        // CheckRep unless using the reopen link.
        if (!reopen)
        {
            if (sid == 0 || !_users.Exists(sid))
                return AuthRedirectToLogon();
            if (_users.UsrInt(sid, "IsRep") != 1)
                throw _error.Error(3, "Access denied.  You do not have permission to view this page.");
        }

        var id = Vb.CInt(Request.Query["id"].ToString());
        var blnUpdate = Request.HasFormContentType && Vb.CInt(Request.Form["update"].ToString()) == 1;
        var updateMessage = "";

        if (blnUpdate)
        {
            var f = Request.Form;
            id = Vb.CInt(f["id"].ToString());
            var uid = f["uid"].ToString();
            var uemail = f["uemail"].ToString();
            var uphone = f["uphone"].ToString();
            var ulocation = f["ulocation"].ToString();
            var category = Vb.CInt(f["category"].ToString());
            var department = Vb.CInt(f["department"].ToString());
            var title = f["title"].ToString();
            var priority = Vb.CInt(f["priority"].ToString());
            var status = Vb.CInt(f["status"].ToString());
            var rep = Vb.CInt(f["rep"].ToString());
            var oldrep = Vb.CInt(f["oldrep"].ToString());
            var solution = f["solution"].ToString();
            var notes = f["notes"].ToString();
            var duedate = _dates.ConvertFormattedDate(f["duedate"].ToString());
            var kb = f["kb"].ToString() == "on" ? 1 : 0;
            var closeStatus = _config.GetInt("CloseStatus");

            if (uid.Length == 0) throw _error.Error(1, _lang.Lang("UserName"));
            if (uemail == _config.GetString("BaseEmail") || !uemail.Contains('@')) throw _error.Error(1, _lang.Lang("EMailAddress"));
            if (title.Length == 0) throw _error.Error(1, _lang.Lang("Title"));
            if (duedate is null) throw _error.Error(1, _lang.Lang("DueDate"));
            if (status == closeStatus && solution.Length == 0) throw _error.Error(1, _lang.Lang("Solution"));

            uemail = Left(uemail.Trim(), 50);
            uphone = Left(uphone.Trim(), 50);
            ulocation = Left(ulocation.Trim(), 50);
            title = Left(title.Trim(), 50); // NB: rep truncates title to 50 (quirk vs user's 255)
            var timeSpent = Vb.CInt(f["time_spent"].ToString().Trim());

            var hideNotes = f["hidenotes"].ToString() == "on";
            var blnFirstResponse = (notes.Length > 0 && !hideNotes) || status == closeStatus;
            var blnNotifyRep = notes.Length > 0 && sid != rep && rep == oldrep;

            var old = (IDictionary<string, object>)conn.QueryFirst(
                "SELECT category, department, rep, status, priority, first_response FROM problems WHERE id = @id", new { id });

            var change = BuildChangeNotes(old, category, department, rep, status, priority);

            var noteAuthor = _users.UsrString(sid, "uid");
            var blnSendUpdateMsg = false;
            if (notes.Length > 0)
            {
                var intPrivate = hideNotes ? 1 : 0;
                if (!hideNotes) blnSendUpdateMsg = true;
                conn.Execute("INSERT INTO tblNotes (id, [note], addDate, uid, private) VALUES (@id, @n, @d, @u, @p)",
                    new { id, n = notes, d = DateTime.Now, u = noteAuthor, p = intPrivate });
            }
            if (change.Length > 0)
                conn.Execute("INSERT INTO tblNotes (id, [note], addDate, uid, private) VALUES (@id, @n, @d, @u, 1)",
                    new { id, n = change, d = DateTime.Now, u = noteAuthor });

            var oldPriority = Vb.CInt(conn.ExecuteScalar<object?>("SELECT priority FROM problems WHERE id = @id", new { id }));

            var p2 = new DynamicParameters();
            p2.Add("id", id); p2.Add("uid", uid); p2.Add("uemail", uemail); p2.Add("uphone", uphone);
            p2.Add("ulocation", ulocation); p2.Add("category", category); p2.Add("department", department);
            p2.Add("title", title); p2.Add("priority", priority); p2.Add("status", status); p2.Add("rep", rep);
            p2.Add("kb", kb); p2.Add("due_date", duedate); p2.Add("time_spent", timeSpent); p2.Add("solution", solution);

            var sql = "UPDATE problems SET uid=@uid, uemail=@uemail, uphone=@uphone, ulocation=@ulocation, " +
                      "category=@category, department=@department, title=@title, priority=@priority, status=@status, " +
                      "rep=@rep, kb=@kb, due_date=@due_date, time_spent=@time_spent, solution=@solution";
            var noemail = Request.Form["noemail"].ToString() == "on";
            if (status == closeStatus)
            {
                p2.Add("close_date", DateTime.Now);
                sql += ", close_date=@close_date";
                if (!noemail) sql += ", emailsent=1";
            }
            sql += " WHERE id=@id";
            conn.Execute(sql, p2);
            updateMessage = _lang.Lang("Theproblemhasbeensaved") + ".";

            // first_response captured once.
            var firstResponse = old["first_response"];
            var hasFirstResponse = firstResponse is not (null or DBNull) && !string.IsNullOrEmpty(Vb.Str(firstResponse));
            if (blnFirstResponse && !hasFirstResponse)
                conn.Execute("UPDATE problems SET first_response=@d WHERE id=@id", new { d = DateTime.Now, id });

            if (status == closeStatus)
            {
                if (!noemail) _email.EMessage("userclose", id, uemail);
            }
            else
            {
                if (_config.GetInt("Notifyuser") == 1 && !noemail && blnSendUpdateMsg)
                    _email.EMessage("userupdate", id, uemail);
                if (blnNotifyRep)
                    _email.EMessage("repupdate", id, _users.UsrString(rep, "email1"));
                if (rep != oldrep)
                {
                    _email.EMessage("repnew", id, _users.UsrString(rep, "email1"));
                    if (priority >= _config.GetInt("EnablePager") && _users.UsrString(rep, "email2").Length > 0)
                        _email.EMessage("reppager", id, _users.UsrString(rep, "email2"));
                }
                else if (priority != oldPriority)
                {
                    if (priority >= _config.GetInt("EnablePager") && _users.UsrString(rep, "email2").Length > 0)
                        _email.EMessage("reppager", id, _users.UsrString(rep, "email2"));
                }
            }
        }

        if (reopen)
        {
            var defaultStatus = _config.GetInt("DefaultStatus");
            var closeStatus = _config.GetInt("CloseStatus");
            var isRep = _users.UsrInt(sid, "IsRep") == 1;

            var sqlOpen = "UPDATE problems SET status=@ds, close_date=NULL" + (isRep ? ", rep=@sid" : "") + " WHERE id=@id";
            conn.Execute(sqlOpen, new { ds = defaultStatus, sid, id });

            var newStat = conn.ExecuteScalar<string>("SELECT sname FROM status WHERE status_id=@s", new { s = defaultStatus });
            var oldStat = conn.ExecuteScalar<string>("SELECT sname FROM status WHERE status_id=@s", new { s = closeStatus });
            var openNote = _lang.Lang("STATUS_2") + ": " + oldStat + " => " + newStat + "\n";
            conn.Execute("INSERT INTO tblNotes (id, [note], addDate, uid, private) VALUES (@id, @n, @d, @u, 1)",
                new { id, n = openNote, d = DateTime.Now, u = _users.UsrString(sid, "uid") });

            var userEmail = conn.ExecuteScalar<string>("SELECT uemail FROM problems WHERE id=@id", new { id });
            _email.EMessage("usernew", id, userEmail ?? "");

            if (!isRep)
            {
                var repEmail = conn.ExecuteScalar<string>(
                    "SELECT r.email1 FROM problems p JOIN tblUsers r ON r.sid = p.rep WHERE p.id=@id", new { id });
                _email.EMessage("repupdate", id, repEmail ?? "");
                return Redirect("/User/Problem/Details?id=" + id);
            }
        }

        // ----- display -----
        var probQuery = "SELECT uid, uemail, uphone, ulocation, time_spent, department, category, status, priority, " +
                        "entered_by, rep, kb, start_date, due_date, close_date, title, description FROM problems WHERE id=@id";
        if (_users.UsrInt(sid, "RepAccess") == 1) probQuery += " AND rep=@sid";
        var prob = conn.QueryFirstOrDefault(probQuery, new { id, sid });
        if (prob is null)
            throw _error.Error(3, "Problem " + id + " could not be found in the database.");

        var pr = (IDictionary<string, object>)prob;
        var solutionVal = Vb.Str(conn.ExecuteScalar<object?>("SELECT solution FROM problems WHERE id=@id", new { id }));
        var closeStatusD = _config.GetInt("CloseStatus");
        var statusD = Vb.CInt(pr["status"]);
        var isClosed = statusD == closeStatusD;
        var repAccess = _users.UsrInt(sid, "RepAccess");
        var disable = repAccess == 2 || isClosed;

        var notesList = new List<RepNoteVm>();
        foreach (var r in conn.Query("SELECT addDate, uid, private, [note] FROM tblNotes WHERE id=@id ORDER BY addDate ASC", new { id }))
        {
            var nd = (IDictionary<string, object>)r;
            notesList.Add(new RepNoteVm
            {
                DateDisplay = _dates.DisplayDate(nd["addDate"], true),
                Uid = Vb.Str(nd["uid"]),
                Private = Vb.CInt(nd["private"]) == 1,
                NoteHtml = Vb.Str(nd["note"]).Replace("\r\n", "<br />").Replace("\n", "<br />"),
            });
        }

        ViewData["Title"] = _lang.Lang("HelpDesk") + " - " + _lang.Lang("EditProblem");
        return View(new RepDetailsVm
        {
            Id = id, JustUpdated = blnUpdate, UpdateMessage = updateMessage,
            Uid = Vb.Str(pr["uid"]), Uemail = Vb.Str(pr["uemail"]), Uphone = Vb.Str(pr["uphone"]), Ulocation = Vb.Str(pr["ulocation"]),
            TimeSpent = Vb.CInt(pr["time_spent"]), Department = Vb.CInt(pr["department"]), Category = Vb.CInt(pr["category"]),
            Status = statusD, Priority = Vb.CInt(pr["priority"]), Rep = Vb.CInt(pr["rep"]), Kb = Vb.CInt(pr["kb"]),
            EnteredByUid = _users.UsrString(Vb.CInt(pr["entered_by"]), "uid"),
            StartDate = _dates.DisplayDate(pr["start_date"], true),
            CloseDate = _dates.DisplayDate(pr["close_date"], true),
            DueDate = _dates.DisplayDate(pr["due_date"], false),
            DateFormat = _users.UsrString(sid, "dateformat"),
            Title = Vb.Str(pr["title"]), Description = Vb.Str(pr["description"]), Solution = solutionVal,
            IsClosed = isClosed, ReadonlyText = disable, DisabledList = disable,
            EnableKB = _config.GetInt("EnableKB"), EmailType = _config.GetInt("EmailType"),
            ShowSave = !isClosed && repAccess != 2,
            Departments = Opts("SELECT department_id, dname FROM departments WHERE department_id > 0 ORDER BY dname COLLATE NOCASE ASC"),
            Categories = Opts("SELECT category_id, cname FROM categories WHERE category_id > 0 ORDER BY cname COLLATE NOCASE ASC"),
            Statuses = Opts("SELECT status_id, sname FROM status WHERE status_id > 0 ORDER BY status_id ASC"),
            Priorities = Opts("SELECT priority_id, pname FROM priority WHERE priority_id > 0 ORDER BY priority_id ASC"),
            Reps = Opts("SELECT sid, uid FROM tblUsers WHERE IsRep=1 AND RepAccess<>2 ORDER BY uid COLLATE NOCASE ASC"),
            Notes = notesList,
        });
    }

    private string BuildChangeNotes(IDictionary<string, object> old, int category, int department, int rep, int status, int priority)
    {
        var oldCategory = Vb.CInt(old["category"]); var oldDepartment = Vb.CInt(old["department"]);
        var oldRep = Vb.CInt(old["rep"]); var oldStatus = Vb.CInt(old["status"]); var oldPriority = Vb.CInt(old["priority"]);
        if (category == oldCategory && department == oldDepartment && rep == oldRep && status == oldStatus && priority == oldPriority)
            return "";

        var conn = _db.Connection;
        var sb = new System.Text.StringBuilder();
        string Name(string sql, object p) => Vb.Str(conn.ExecuteScalar<object?>(sql, p));

        if (priority != oldPriority)
            sb.Append(_lang.Lang("PRIORITY_2")).Append(": ")
              .Append(Name("SELECT pname FROM priority WHERE priority_id=@x", new { x = oldPriority })).Append(" => ")
              .Append(Name("SELECT pname FROM priority WHERE priority_id=@x", new { x = priority })).Append('\n');
        if (rep != oldRep)
            sb.Append(_lang.Lang("TRANSFERREPS")).Append(": ")
              .Append(Name("SELECT uid FROM tblUsers WHERE sid=@x", new { x = oldRep })).Append(" => ")
              .Append(Name("SELECT uid FROM tblUsers WHERE sid=@x", new { x = rep })).Append('\n');
        if (category != oldCategory)
            sb.Append(_lang.Lang("CATEGORY_2")).Append(": ")
              .Append(Name("SELECT cname FROM categories WHERE category_id=@x", new { x = oldCategory })).Append(" => ")
              .Append(Name("SELECT cname FROM categories WHERE category_id=@x", new { x = category })).Append('\n');
        if (department != oldDepartment)
            sb.Append(_lang.Lang("DEPARTMENT_2")).Append(": ")
              .Append(Name("SELECT dname FROM departments WHERE department_id=@x", new { x = oldDepartment })).Append(" => ")
              .Append(Name("SELECT dname FROM departments WHERE department_id=@x", new { x = department })).Append('\n');
        if (status != oldStatus)
            sb.Append(_lang.Lang("STATUS_2")).Append(": ")
              .Append(Name("SELECT sname FROM status WHERE status_id=@x", new { x = oldStatus })).Append(" => ")
              .Append(Name("SELECT sname FROM status WHERE status_id=@x", new { x = status })).Append('\n');
        return sb.ToString();
    }

    // rep/print.asp — printer-friendly view (all notes, incl. private).
    [HttpGet("Print")]
    [CheckRep]
    public IActionResult Print()
    {
        // rep/print.asp: empty id => faithful "No valid problem ID was entered." (literal, not a lang key).
        if (Request.Query["id"].ToString().Length == 0)
            throw _error.Error(3, "No valid problem ID was entered.");
        var id = Vb.CInt(Request.Query["id"].ToString());
        var prob = _db.Connection.QueryFirstOrDefault(
            "SELECT p.id, p.uid, p.uemail, p.uphone, p.ulocation, p.entered_by, d.dname, p.start_date, p.status, s.sname, " +
            "p.close_date, c.cname, r.uid AS ruid, r.fname AS rname, p.title, p.solution, p.description, pri.pname " +
            "FROM problems p JOIN departments d ON p.department=d.department_id JOIN status s ON p.status=s.status_id " +
            "JOIN tblUsers r ON p.rep=r.sid JOIN priority pri ON p.priority=pri.priority_id JOIN categories c ON p.category=c.category_id " +
            "WHERE p.id=@id", new { id });
        if (prob is null)
            throw _error.Error(3, _lang.Lang("ProblemID") + " " + id + " " + _lang.Lang("wasfoundinthedatabase") + ".");

        var pr = (IDictionary<string, object>)prob;
        var isClosed = Vb.CInt(pr["status"]) == _config.GetInt("CloseStatus");
        var notes = new List<PrintNote>();
        foreach (var r in _db.Connection.Query("SELECT addDate, uid, private, [note] FROM tblNotes WHERE id=@id ORDER BY addDate ASC", new { id }))
        {
            var nd = (IDictionary<string, object>)r;
            var header = "[" + _dates.DisplayDate(nd["addDate"], true) + " - " + Vb.Str(nd["uid"]) + "]";
            if (Vb.CInt(nd["private"]) == 1) header += " - " + _lang.Lang("PRIVATE");
            notes.Add(new PrintNote { Header = header, NoteHtml = Vb.FormatBlock(nd["note"]) });
        }

        // rep/print.asp title: ...<%=lang("Problem")%> <% = Request.QueryString("id") %> <%=lang("Details")%>.
        // The <% = %> form around the id consumes its adjacent literal spaces, so the live original renders
        // "...Problem1Details" (no spaces around the id). Reproduce that faithfully.
        ViewData["Title"] = _lang.Lang("HelpDesk") + " - " + _lang.Lang("Problem") + id + _lang.Lang("Details");
        return View(new PrintProblemVm
        {
            Id = id, Uid = Vb.Str(pr["uid"]), Uemail = Vb.Str(pr["uemail"]), Uphone = Vb.Str(pr["uphone"]), Ulocation = Vb.Str(pr["ulocation"]),
            EnteredByUid = _users.UsrString(Vb.CInt(pr["entered_by"]), "uid"),
            Dname = Vb.Str(pr["dname"]), Cname = Vb.Str(pr["cname"]), Sname = Vb.Str(pr["sname"]), Pname = Vb.Str(pr["pname"]), Rname = Vb.Str(pr["rname"]),
            Title = Vb.Str(pr["title"]), StartDate = _dates.DisplayDate(pr["start_date"], true), CloseDate = _dates.DisplayDate(pr["close_date"], true),
            IsClosed = isClosed, DescriptionHtml = Vb.FormatBlock(pr["description"]), SolutionHtml = isClosed ? Vb.FormatBlock(pr["solution"]) : "",
            Notes = notes,
        });
    }

    // rep/new.asp — create a problem (optionally on behalf of a selected user).
    [HttpGet("New")]
    [HttpPost("New")]
    [CheckRep]
    public IActionResult New()
    {
        var conn = _db.Connection;
        var sid = _session.Sid;
        var closeStatus = _config.GetInt("CloseStatus");
        var submitted = Request.HasFormContentType && Vb.CInt(Request.Form["save"].ToString()) == 1;
        var submitResults = "";

        if (submitted)
        {
            var f = Request.Form;
            var uselectid = Vb.CInt(f["uselectid"].ToString());
            var uid = f["uid"].ToString();
            var uemail = f["uemail"].ToString();
            var uphone = f["uphone"].ToString();
            var ulocation = f["ulocation"].ToString();
            var category = Vb.CInt(f["category"].ToString());
            var department = Vb.CInt(f["department"].ToString());
            var title = f["title"].ToString();
            var description = f["description"].ToString();
            var priority = Vb.CInt(f["priority"].ToString());
            var status = Vb.CInt(f["status"].ToString());
            var rep = Vb.CInt(f["rep"].ToString());
            var timeSpent = Vb.CInt(f["time_spent"].ToString());
            var solution = f["solution"].ToString();
            var duedate = _dates.ConvertFormattedDate(f["duedate"].ToString());
            var kb = f["kb"].ToString() == "on" ? 1 : 0;
            var noemail = f["noemail"].ToString() == "on";
            var intEmailSent = (!noemail && status == closeStatus) ? 1 : 0;

            if (uselectid != 0)
            {
                uid = _users.UsrString(uselectid, "uid");
                uemail = _users.UsrString(uselectid, "email1");
                uphone = _users.UsrString(uselectid, "phone");
                ulocation = _users.UsrString(uselectid, "location1");
                department = _users.UsrInt(uselectid, "department");
            }
            else
            {
                if (uid.Length == 0) throw _error.Error(1, _lang.Lang("UserName"));
                if (uemail == _config.GetString("BaseEmail")) throw _error.Error(1, _lang.Lang("EMailAddress"));
            }
            if (category == 0) throw _error.Error(1, _lang.Lang("Category"));
            if (department == 0 && uselectid == 0) throw _error.Error(1, _lang.Lang("Department"));
            if (title.Length == 0) throw _error.Error(1, _lang.Lang("Title"));
            if (title.Length > 255) title = title.Trim()[..255]; // rep/new truncates to 255 (unlike rep/details=50)
            if (duedate is null) throw _error.Error(1, _lang.Lang("DueDate"));
            if (description.Length == 0) throw _error.Error(1, _lang.Lang("Description"));
            if (status == closeStatus && solution.Length == 0) throw _error.Error(1, _lang.Lang("Solution"));

            var id = _keys.GetUnique("problems");
            var now = DateTime.Now;
            var p = new DynamicParameters();
            p.Add("id", id); p.Add("uid", uid); p.Add("uemail", uemail); p.Add("uphone", uphone);
            p.Add("ulocation", ulocation); p.Add("category", category); p.Add("department", department);
            p.Add("title", title); p.Add("description", description); p.Add("priority", priority);
            p.Add("status", status); p.Add("start_date", now); p.Add("due_date", duedate); p.Add("rep", rep);
            p.Add("time_spent", timeSpent); p.Add("entered_by", sid); p.Add("solution", solution); p.Add("kb", kb);

            if (status == closeStatus)
            {
                p.Add("emailsent", intEmailSent);
                conn.Execute(
                    "INSERT INTO problems (id, uid, uemail, uphone, ulocation, category, department, title, description, " +
                    "priority, status, start_date, due_date, rep, time_spent, close_date, first_response, entered_by, solution, kb, emailsent) " +
                    "VALUES (@id,@uid,@uemail,@uphone,@ulocation,@category,@department,@title,@description,@priority,@status," +
                    "@start_date,@due_date,@rep,@time_spent,@start_date,@start_date,@entered_by,@solution,@kb,@emailsent)", p);
            }
            else
            {
                conn.Execute(
                    "INSERT INTO problems (id, uid, uemail, uphone, ulocation, category, department, title, description, " +
                    "priority, status, start_date, due_date, rep, time_spent, entered_by, solution, kb) " +
                    "VALUES (@id,@uid,@uemail,@uphone,@ulocation,@category,@department,@title,@description,@priority,@status," +
                    "@start_date,@due_date,@rep,@time_spent,@entered_by,@solution,@kb)", p);
            }

            var remail = _users.UsrString(rep, "email1");
            if (status != closeStatus)
            {
                if (!noemail) _email.EMessage("usernew", id, uemail);
                _email.EMessage("repnew", id, remail);
                if (priority >= _config.GetInt("EnablePager") && _users.UsrString(rep, "email2").Length > 0)
                    _email.EMessage("reppager", id, _users.UsrString(rep, "email2"));
            }
            else if (!noemail)
            {
                _email.EMessage("userclose", id, uemail);
            }
            submitResults = _lang.Lang("Problem") + " " + id + " " + _lang.Lang("hasbeenentered") + ".";
        }

        ViewData["Title"] = _lang.Lang("HelpDesk") + " - " + _lang.Lang("NewProblem");
        return View(new RepNewVm
        {
            JustSubmitted = submitted, SubmitResults = submitResults,
            BaseEmail = _config.GetString("BaseEmail"), UseSelectUser = _config.GetInt("useSelectUser") == 1,
            DefaultStatus = _config.GetInt("DefaultStatus"), DefaultPriority = _config.GetInt("DefaultPriority"),
            SelfSid = sid, DueDate = _dates.DisplayDate(DateTime.Now.AddDays(1), false), DateFormat = _users.UsrString(sid, "dateformat"),
            EnableKB = _config.GetInt("EnableKB"), EmailType = _config.GetInt("EmailType"),
            Departments = Opts("SELECT department_id, dname FROM departments WHERE department_id > 0 ORDER BY dname COLLATE NOCASE ASC"),
            Categories = Opts("SELECT category_id, cname FROM categories WHERE category_id > 0 ORDER BY cname COLLATE NOCASE ASC"),
            Statuses = Opts("SELECT status_id, sname FROM status WHERE status_id > 0 ORDER BY status_id ASC"),
            Priorities = Opts("SELECT priority_id, pname FROM priority WHERE priority_id > 0 ORDER BY priority_id ASC"),
            Reps = Opts("SELECT sid, uid FROM tblUsers WHERE IsRep = 1 AND RepAccess <> 2 AND sid > 0 ORDER BY uid COLLATE NOCASE ASC"),
        });
    }

    // rep/view.asp
    [HttpGet("View")]
    [HttpPost("View")]
    [CheckRep]
    public new IActionResult View()
    {
        var conn = _db.Connection;
        var sid = _session.Sid;

        int repId;
        if (Request.Query["rep_id"].ToString().Length > 0) repId = Vb.CInt(Request.Query["rep_id"].ToString());
        else if (Request.HasFormContentType && Request.Form["rep_id"].ToString().Length > 0) repId = Vb.CInt(Request.Form["rep_id"].ToString());
        else repId = sid;

        var ruid = _users.UsrString(repId, "uid");
        var closeStatus = _config.GetInt("CloseStatus");

        var sort = Vb.CInt(Request.Query["sort"].ToString());
        var order = Request.Query["order"].ToString().Length > 0 ? Vb.CInt(Request.Query["order"].ToString()) : 0;

        string where; bool dispTotal; var qid = Vb.CInt(Request.Query["id"].ToString());
        if (Request.Query["id"].ToString().Length > 0) { where = "p.id = @qid"; dispTotal = false; }
        else { where = "p.status <> @cs AND p.rep = @repId"; dispTotal = true; }

        string orderBy;
        int idOrder = 0, titleOrder = 0, uidOrder = 0, dateOrder = 0, priOrder = 0, statusOrder = 0;
        switch (sort)
        {
            case 2: orderBy = "p.title " + (order == 0 ? "ASC" : "DESC"); titleOrder = order == 0 ? 1 : 0; break;
            case 3: orderBy = "p.uid " + (order == 0 ? "ASC" : "DESC"); uidOrder = order == 0 ? 1 : 0; break;
            case 4: orderBy = "p.start_date " + (order == 0 ? "DESC" : "ASC"); dateOrder = order == 0 ? 1 : 0; break;
            case 5: orderBy = "p.priority " + (order == 0 ? "DESC" : "ASC"); priOrder = order == 0 ? 1 : 0; break;
            case 6: orderBy = "p.status " + (order == 0 ? "DESC" : "ASC"); statusOrder = order == 0 ? 1 : 0; break;
            default: orderBy = "p.id " + (order == 0 ? "DESC" : "ASC"); idOrder = order == 0 ? 1 : 0; break;
        }

        var rows = conn.Query(
            "SELECT p.id, p.title, p.start_date, p.uid, p.uemail, r.uid AS ruid, pri.pname, s.sname FROM problems p " +
            "JOIN tblUsers r ON p.rep = r.sid JOIN priority pri ON p.priority = pri.priority_id " +
            "JOIN status s ON p.status = s.status_id WHERE " + where + " ORDER BY " + orderBy + " LIMIT 100",
            new { qid, cs = closeStatus, repId }).ToList();

        var total = dispTotal
            ? (int)conn.ExecuteScalar<long>("SELECT count(*) FROM problems WHERE status <> @cs AND rep = @repId", new { cs = closeStatus, repId })
            : 0;

        // Faithful view.asp pager: CInt(num)/CInt(start) passed through verbatim, default only when the
        // posted field is absent (NOT clamped to >0) — a row at 1-based position i shows iff
        // start <= i <= start + num - 1 (the original "Counter >= start AND Counter <= num+start-1" loop).
        var num = Request.Query["num"].ToString().Length > 0 ? Vb.CInt(Request.Query["num"].ToString()) : 25;
        var start = Request.Query["start"].ToString().Length > 0 ? Vb.CInt(Request.Query["start"].ToString()) : 1;
        var windowEnd = start + num - 1;
        var useInout = _config.GetInt("UseInoutBoard");

        var window = rows.Where((_, idx) => idx + 1 >= start && idx + 1 <= windowEnd).Select(r =>
        {
            var d = (IDictionary<string, object>)r;
            int? inoutSid = null;
            if (useInout == 1)
            {
                var s = conn.ExecuteScalar<object?>("SELECT sid FROM tblUsers WHERE uid = @uid COLLATE NOCASE", new { uid = Vb.Str(d["uid"]) });
                if (s is not (null or DBNull)) inoutSid = Vb.CInt(s);
            }
            return new RepListRow
            {
                Id = Vb.CInt(d["id"]), Title = Vb.Str(d["title"]), Uid = Vb.Str(d["uid"]), Uemail = Vb.Str(d["uemail"]),
                StartDate = _dates.DisplayDate(d["start_date"], false), Pname = Vb.Str(d["pname"]), Sname = Vb.Str(d["sname"]),
                InoutSid = inoutSid,
            };
        }).ToList();

        ViewData["Title"] = _lang.Lang("HelpDesk") + " - " + _lang.Lang("OpenProblems");
        return View(new RepListVm
        {
            Ruid = ruid, RepId = repId, DisplayTotal = dispTotal, Total = total, UseInout = useInout,
            Rows = window, Sort = sort, Order = order, Start = start, NumToDisplay = num,
            IdOrder = idOrder, TitleOrder = titleOrder, UidOrder = uidOrder, DateOrder = dateOrder, PriOrder = priOrder, StatusOrder = statusOrder,
            ShowPrev = start > 1, ShowNext = rows.Count > windowEnd, StartP = Math.Max(1, start - num), StartN = start + num,
            HasResults = window.Count > 0,
        });
    }

    private static string Left(string s, int n) => s.Length > n ? s[..n] : s;

    private RedirectResult AuthRedirectToLogon()
    {
        var path = Request.Path.Value ?? "/";
        if (Request.QueryString.HasValue) path += Request.QueryString.Value;
        return new RedirectResult("/Logon?URL=" + Uri.EscapeDataString(path));
    }
}
