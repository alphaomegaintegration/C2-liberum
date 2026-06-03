using Dapper;
using LiberumHelpDesk.Web.Filters;
using LiberumHelpDesk.Web.Models.ViewModels;
using LiberumHelpDesk.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LiberumHelpDesk.Web.Areas.Admin.Controllers;

/// <summary>
/// Ports the admin lookup CRUD: viewcat/viewdep/viewpri/viewstatus (lists), modify (form),
/// postmods (save), confdelete (confirm), delete (delete + reassign to 0). mtype: 1=rep(dead),
/// 2=category, 3=department, 4=priority, 5=status, 6=language.
/// </summary>
[Area("Admin")]
public sealed class LookupController : Controller
{
    private readonly Db _db;
    private readonly IConfigService _config;
    private readonly ISessionContext _session;
    private readonly ILanguageService _lang;
    private readonly IKeyService _keys;
    private readonly IErrorService _error;

    public LookupController(Db db, IConfigService config, ISessionContext session, ILanguageService lang, IKeyService keys, IErrorService error)
    {
        _db = db; _config = config; _session = session; _lang = lang; _keys = keys; _error = error;
    }

    private string L(string k) => _lang.Lang(k);

    // ---- List pages ----
    [HttpGet("/Admin/ViewCat")]
    [CheckAdmin]
    public IActionResult ViewCat()
    {
        var rows = new List<LookupRowVm>();
        foreach (var r in _db.Connection.Query("SELECT category_id, cname, rep_id FROM categories WHERE category_id > 0 ORDER BY cname COLLATE NOCASE ASC"))
        {
            var d = (IDictionary<string, object>)r;
            var repUid = Vb.Str(_db.Connection.ExecuteScalar<object?>("SELECT uid FROM tblUsers WHERE sid = @s", new { s = Vb.CInt(d["rep_id"]) }));
            rows.Add(new LookupRowVm { Id = Vb.CInt(d["category_id"]), Cells = new[] { Vb.Str(d["cname"]), repUid } });
        }
        return ListView("Categories", new[] { L("Category"), L("Rep"), L("Modify") }, rows, 2, L("AddNew") + " " + L("Category"));
    }

    [HttpGet("/Admin/ViewDep")]
    [CheckAdmin]
    public IActionResult ViewDep()
    {
        var rows = _db.Connection.Query("SELECT department_id, dname FROM departments WHERE department_id > 0 ORDER BY dname COLLATE NOCASE ASC")
            .Select(r => { var d = (IDictionary<string, object>)r; return new LookupRowVm { Id = Vb.CInt(d["department_id"]), Cells = new[] { Vb.Str(d["dname"]) } }; }).ToList();
        // Faithful: viewdep.asp's <title> omits the " - " separator (original inconsistency).
        return ListView("Departments", new[] { L("Department"), L("Modify") }, rows, 3, L("AddNew") + " " + L("Department"),
            titleOverride: L("HelpDesk") + " " + L("Manage") + " " + L("Departments"));
    }

    [HttpGet("/Admin/ViewPri")]
    [CheckAdmin]
    public IActionResult ViewPri()
    {
        // First column header is the "ID" lang key (viewpri.asp:57), not "PriorityNumber".
        var rows = _db.Connection.Query("SELECT priority_id, pname FROM priority WHERE priority_id > 0 ORDER BY priority_id ASC")
            .Select(r => { var d = (IDictionary<string, object>)r; return new LookupRowVm { Id = Vb.CInt(d["priority_id"]), Cells = new[] { Vb.Str(d["priority_id"]), Vb.Str(d["pname"]) } }; }).ToList();
        return ListView("Priorities", new[] { L("ID"), L("Priority"), L("Modify") }, rows, 4, L("AddNew") + " " + L("Priority"));
    }

    [HttpGet("/Admin/ViewStatus")]
    [CheckAdmin]
    public IActionResult ViewStatus()
    {
        // First column header is "ID" (viewstatus.asp:57); the CloseStatus row is marked with "*" and the
        // "ClosedStatusDonotdelete." footnote is shown below the table.
        var closeStatus = _config.GetInt("CloseStatus");
        var rows = _db.Connection.Query("SELECT status_id, sname FROM status WHERE status_id > 0 ORDER BY status_id ASC")
            .Select(r => { var d = (IDictionary<string, object>)r; var sid = Vb.CInt(d["status_id"]); return new LookupRowVm { Id = sid, Cells = new[] { Vb.Str(d["status_id"]), Vb.Str(d["sname"]) }, Marked = sid == closeStatus }; }).ToList();
        // Faithful: viewstatus.asp's <title> reads "Manage Reports" (original copy-paste bug; the body
        // heading correctly says "Statuses").
        return ListView("Statuses", new[] { L("ID"), L("Status"), L("Modify") }, rows, 5, L("AddNew") + " " + L("Status"), L("ClosedStatusDonotdelete"),
            titleOverride: L("HelpDesk") + " - " + L("Manage") + " " + L("Reports"));
    }

    private IActionResult ListView(string headingKey, string[] headers, List<LookupRowVm> rows, int mtype, string addLabel, string? footnote = null, string? titleOverride = null)
    {
        ViewData["Title"] = titleOverride ?? (_lang.Lang("HelpDesk") + " - " + _lang.Lang("Manage") + " " + _lang.Lang(headingKey));
        return View("List", new LookupListVm { Heading = _lang.Lang(headingKey), Headers = headers, Rows = rows, Mtype = mtype, AddLabel = addLabel, Footnote = footnote });
    }

    // ---- modify.asp (add/edit form) ----
    [HttpGet("/Admin/Modify")]
    [HttpPost("/Admin/Modify")]
    [CheckAdmin]
    public IActionResult Modify()
    {
        var mtype = Vb.CInt(Request.Query["mtype"].ToString());
        // modify.asp:49-50 reads these from QueryString (empty for mtype 2-5) and re-emits them as
        // hidden fields so they survive the round-trip to postmods.asp.
        var mLangID = Request.Query["mLangID"].ToString();
        var mLanguage = Request.Query["mLanguage"].ToString();
        if (mtype == 1) throw _error.Error(3, "The reps table is not part of this build."); // dead path (C1)
        if (mtype < 1 || mtype > 6) throw _error.Error(3, "Invalid type to create/modify");

        var idStr = Request.Query["id"].ToString();
        var dataId = idStr.Length > 0 ? Vb.CInt(idStr) : 0;
        string data1 = "", data2 = "";

        if (idStr.Length > 0)
        {
            switch (mtype)
            {
                case 2: { var d = (IDictionary<string, object>)_db.Connection.QueryFirst("SELECT cname, rep_id FROM categories WHERE category_id=@id", new { id = dataId }); data1 = Vb.Str(d["cname"]); data2 = Vb.Str(d["rep_id"]); break; }
                case 3: data1 = Vb.Str(_db.Connection.ExecuteScalar<object?>("SELECT dname FROM departments WHERE department_id=@id", new { id = dataId })); break;
                case 4: { data1 = dataId.ToString(); data2 = Vb.Str(_db.Connection.ExecuteScalar<object?>("SELECT pname FROM priority WHERE priority_id=@id", new { id = dataId })); break; }
                case 5: { data1 = dataId.ToString(); data2 = Vb.Str(_db.Connection.ExecuteScalar<object?>("SELECT sname FROM status WHERE status_id=@id", new { id = dataId })); break; }
                case 6: { var d = (IDictionary<string, object>)_db.Connection.QueryFirst("SELECT LangName, Localized FROM tblLanguage WHERE id=@id", new { id = dataId }); data1 = Vb.Str(d["LangName"]); data2 = Vb.Str(d["Localized"]); break; }
            }
        }

        var (title, n1, n2, num) = mtype switch
        {
            2 => (L("Category"), L("CategoryName"), L("PrimaryRep"), 2),
            3 => (L("Department"), L("Department"), "", 1),
            4 => (L("Priority"), L("PriorityNumber"), L("PriorityName"), 2),
            5 => (L("Status"), L("StatusNumber"), L("StatusName"), 2),
            6 => (L("Language"), L("LanguageName"), L("LocalizedName"), 2),
            _ => ("", "", "", 0),
        };

        var reps = new List<Opt>();
        if (mtype == 2)
            foreach (var r in _db.Connection.Query("SELECT sid, uid FROM tblUsers WHERE IsRep=1 AND RepAccess <> 2 ORDER BY uid COLLATE NOCASE ASC"))
            { var d = (IDictionary<string, object>)r; reps.Add(new Opt(Vb.CInt(d["sid"]), Vb.Str(d["uid"]))); }

        ViewData["Title"] = _lang.Lang("HelpDesk") + " - " + _lang.Lang("CreateModify");
        return View(new ModifyVm
        {
            Mtype = mtype, DataId = dataId, NumDataFields = num, Title = title,
            Data1Name = n1, Data2Name = n2, Data1 = data1, Data2 = data2,
            CategoryRepDropdown = mtype == 2, Reps = reps, SelectedRep = Vb.CInt(data2),
            ShowNumberNote = mtype < 6 && dataId > 0,
            MLanguage = mLanguage, MLangID = mLangID,
        });
    }

    // ---- postmods.asp (save) ----
    [HttpPost("/Admin/PostMods")]
    [CheckAdmin]
    public IActionResult PostMods()
    {
        var f = Request.Form;
        var mtype = Vb.CInt(f["mtype"].ToString());
        if (mtype == 1) throw _error.Error(3, "The reps table is not part of this build.");
        var num = Vb.CInt(f["numdatafields"].ToString());
        var dataId = Vb.CInt(f["data_id"].ToString());
        var data1 = f["data1"].ToString();
        var data2 = f["data2"].ToString();

        if (num >= 1 && data1.Length == 0) throw _error.Error(1, L("Field") + " 1");
        if (num >= 2 && data2.Length == 0) throw _error.Error(1, L("Field") + " 2");

        var conn = _db.Connection;
        if (dataId == 0) // add
        {
            switch (mtype)
            {
                case 2:
                    conn.Execute("INSERT INTO categories (category_id, rep_id, cname) VALUES (@id, @rep, @name)",
                        new { id = _keys.GetUnique("categories"), rep = Vb.CInt(data2), name = data1 });
                    break;
                case 3:
                    conn.Execute("INSERT INTO departments (department_id, dname) VALUES (@id, @name)",
                        new { id = _keys.GetUnique("departments"), name = data1 });
                    break;
                case 4:
                {
                    var pnum = Vb.CInt(data1);
                    if (pnum < 0) throw _error.Error(3, "Enter a positive priority number.");
                    if (conn.ExecuteScalar<long>("SELECT COUNT(*) FROM priority WHERE priority_id=@n", new { n = pnum }) > 0)
                        throw _error.Error(3, "Enter a unique priority number.");
                    conn.Execute("INSERT INTO priority (priority_id, pname) VALUES (@n, @name)", new { n = pnum, name = data2 });
                    break;
                }
                case 5:
                {
                    var snum = Vb.CInt(data1);
                    if (snum < 0) throw _error.Error(3, L("Enterapositivestatusnumber") + ".");
                    if (conn.ExecuteScalar<long>("SELECT COUNT(*) FROM status WHERE status_id=@n", new { n = snum }) > 0)
                        throw _error.Error(3, L("Enterauniquestatusnumber") + ".");
                    conn.Execute("INSERT INTO status (status_id, sname) VALUES (@n, @name)", new { n = snum, name = data2 });
                    break;
                }
                case 6:
                    if (conn.ExecuteScalar<long>("SELECT COUNT(*) FROM tblLanguage WHERE LangName=@n AND Localized=@l", new { n = data1, l = data2 }) > 0)
                        throw _error.Error(3, L("Enterauniquelanguageandlocalizedname") + ".");
                    conn.Execute("INSERT INTO tblLanguage (id, LangName, Localized) VALUES (@id, @n, @l)",
                        new { id = _keys.GetUnique("lang"), n = data1, l = data2 });
                    break;
            }
        }
        else // update
        {
            switch (mtype)
            {
                case 2: conn.Execute("UPDATE categories SET cname=@name, rep_id=@rep WHERE category_id=@id", new { name = data1, rep = Vb.CInt(data2), id = dataId }); break;
                case 3: conn.Execute("UPDATE departments SET dname=@name WHERE department_id=@id", new { name = data1, id = dataId }); break;
                case 4: conn.Execute("UPDATE priority SET pname=@name WHERE priority_id=@id", new { name = data2, id = dataId }); break;
                case 5: conn.Execute("UPDATE status SET sname=@name WHERE status_id=@id", new { name = data2, id = dataId }); break;
                case 6: conn.Execute("UPDATE tblLanguage SET LangName=@n, Localized=@l WHERE id=@id", new { n = data1, l = data2, id = dataId }); break;
            }
            if (mtype == 6) _lang.ClearCache();
        }

        ViewData["Title"] = _lang.Lang("HelpDesk") + " - " + _lang.Lang("ModificationDone");
        ViewData["Mtype"] = mtype;
        return View();
    }

    // ---- confdelete.asp ----
    [HttpGet("/Admin/ConfDelete")]
    [CheckAdmin]
    public IActionResult ConfDelete()
    {
        var mtype = Vb.CInt(Request.Query["mtype"].ToString());
        var id = Vb.CInt(Request.Query["id"].ToString());
        if (mtype == 6 && id == 1) throw _error.Error(3, L("Sorryyoucannotdeletethislanguage"));
        // mtype=7 (language strings) is only deletable from the default (English) language — id>0 names a
        // non-English language here and is blocked. (Dead path: no UI links confdelete?mtype=7, matching the
        // original, but the guard is reproduced faithfully.)
        if (mtype == 7 && id > 0) throw _error.Error(3, L("Youmustdeletevariablesfromenglishlanguage"));
        if (mtype < 1 || mtype > 7) throw _error.Error(3, L("InvalidIDordatatype"));
        if (mtype < 7 && id == 0) throw _error.Error(3, L("InvalidIDordatatype"));

        var conn = _db.Connection;
        var cs = _config.GetInt("CloseStatus");
        var ok = true;
        var blockingProblems = new List<int>();
        if (mtype is >= 2 and <= 5)
        {
            var col = mtype switch { 2 => "category", 3 => "department", 4 => "priority", _ => "status" };
            if (mtype == 5 && id == cs) throw _error.Error(3, L("TheCLOSEDstatuscannotbedeleted"));
            blockingProblems = conn.Query<long>($"SELECT id FROM problems WHERE {col}=@id AND status<>@cs", new { id, cs }).Select(x => (int)x).ToList();
            if (blockingProblems.Count > 0) ok = false;
        }

        ViewData["Title"] = _lang.Lang("HelpDesk") + " - " + _lang.Lang("ConfirmDeletion");
        return View(new ConfDeleteVm { Mtype = mtype, Id = id, OkToDelete = ok, BlockingProblems = blockingProblems });
    }

    // ---- delete.asp ----
    [HttpGet("/Admin/Delete")]
    [CheckAdmin]
    public IActionResult Delete()
    {
        var mtype = Vb.CInt(Request.Query["mtype"].ToString());
        var id = Vb.CInt(Request.Query["id"].ToString());
        if (mtype < 1 || mtype > 6) throw _error.Error(3, L("InvalidIDordatatype"));

        var conn = _db.Connection;
        switch (mtype)
        {
            case 1: conn.Execute("UPDATE tblUsers SET IsRep=0 WHERE sid=@id", new { id }); conn.Execute("UPDATE problems SET rep=0 WHERE rep=@id", new { id }); break;
            case 2: conn.Execute("DELETE FROM categories WHERE category_id=@id", new { id }); conn.Execute("UPDATE problems SET category=0 WHERE category=@id", new { id }); break;
            case 3: conn.Execute("DELETE FROM departments WHERE department_id=@id", new { id }); conn.Execute("UPDATE problems SET department=0 WHERE department=@id", new { id }); break;
            case 4: conn.Execute("DELETE FROM priority WHERE priority_id=@id", new { id }); conn.Execute("UPDATE problems SET priority=0 WHERE priority=@id", new { id }); break;
            case 5: conn.Execute("DELETE FROM status WHERE status_id=@id", new { id }); conn.Execute("UPDATE problems SET status=0 WHERE status=@id", new { id }); break;
            case 6: conn.Execute("DELETE FROM tblLanguage WHERE id=@id", new { id }); conn.Execute("DELETE FROM tblLangStrings WHERE id=@id", new { id }); _lang.ClearCache(); break;
        }

        ViewData["Title"] = _lang.Lang("HelpDesk") + " - " + _lang.Lang("ItemDeleted");
        ViewData["Mtype"] = mtype;
        return View();
    }
}
