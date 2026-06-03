using Dapper;
using LiberumHelpDesk.Web.Filters;
using LiberumHelpDesk.Web.Models.ViewModels;
using LiberumHelpDesk.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LiberumHelpDesk.Web.Areas.Admin.Controllers;

/// <summary>Ports admin/viewlang.asp (language list) and admin/viewlangstring.asp (string editor).</summary>
[Area("Admin")]
public sealed class LanguageController : Controller
{
    private const int EnglishId = 1;

    private readonly Db _db;
    private readonly ILanguageService _lang;
    private readonly IErrorService _error;

    public LanguageController(Db db, ILanguageService lang, IErrorService error)
    {
        _db = db; _lang = lang; _error = error;
    }

    // admin/viewlang.asp
    [HttpGet("/Admin/ViewLang")]
    [CheckAdmin]
    public IActionResult ViewLang()
    {
        var rows = _db.Connection.Query("SELECT id, LangName, Localized FROM tblLanguage ORDER BY id ASC")
            .Select(r => { var d = (IDictionary<string, object>)r; return new LangListRow { Id = Vb.CInt(d["id"]), LangName = Vb.Str(d["LangName"]), Localized = Vb.Str(d["Localized"]) }; }).ToList();
        ViewData["Title"] = _lang.Lang("HelpDesk") + " " + _lang.Lang("Manage") + " " + _lang.Lang("Languages");
        return View(rows);
    }

    // admin/viewlangstring.asp
    [HttpGet("/Admin/ViewLangString")]
    [HttpPost("/Admin/ViewLangString")]
    [CheckAdmin]
    public IActionResult ViewLangString()
    {
        var langIdStr = Request.Query["lang_id"].ToString();
        if (langIdStr.Length == 0) throw _error.Error(3, _lang.Lang("NolanguageIDgiven"));
        var langId = Vb.CInt(langIdStr);
        var conn = _db.Connection;
        var saveSuccess = false;
        var addSuccess = false;

        if (Request.HasFormContentType && Request.Form["frm_save"].ToString() == "1")
        {
            // Inputs are named by the English variable; update each by name (equivalent to the original positional loop).
            var vars = conn.Query<string>("SELECT variable FROM tblLangStrings WHERE id=@id ORDER BY variable COLLATE NOCASE ASC", new { id = EnglishId }).ToList();
            foreach (var v in vars)
            {
                // The original only processed submitted form fields; mirror that (so a partial save can't blank others).
                if (!Request.Form.ContainsKey(v)) continue;
                var value = Request.Form[v].ToString();
                var exists = conn.ExecuteScalar<long>("SELECT COUNT(*) FROM tblLangStrings WHERE id=@id AND variable=@v", new { id = langId, v }) > 0;
                if (exists)
                    conn.Execute("UPDATE tblLangStrings SET LangText=@t WHERE id=@id AND variable=@v", new { t = value, id = langId, v });
                else
                    conn.Execute("INSERT INTO tblLangStrings (id, variable, LangText) VALUES (@id, @v, @t)", new { id = langId, v, t = value });
            }
            _lang.ClearCache();
            saveSuccess = true;
        }

        if (Request.HasFormContentType && Request.Form["frm_add"].ToString() == "1")
        {
            var varname = Request.Form["varname"].ToString();
            if (conn.ExecuteScalar<long>("SELECT COUNT(*) FROM tblLangStrings WHERE variable=@v", new { v = varname }) > 0)
                throw _error.Error(3, _lang.Lang("Variablenameisalreadyinuse"));
            var id1 = Vb.CInt(Request.Form["string1_id"].ToString());
            var id2 = Vb.CInt(Request.Form["string2_id"].ToString());
            conn.Execute("INSERT INTO tblLangStrings (id, variable, LangText) VALUES (@id, @v, @t)",
                new { id = id1, v = varname, t = Request.Form["string1_value"].ToString() });
            if (id1 != id2)
                conn.Execute("INSERT INTO tblLangStrings (id, variable, LangText) VALUES (@id, @v, @t)",
                    new { id = id2, v = varname, t = Request.Form["string2_value"].ToString() });
            _lang.ClearCache();
            addSuccess = true;
        }

        // Build the comparison grid: every English string + the current language's text.
        // ORDER BY ... COLLATE NOCASE: the oracle's "ORDER BY variable ASC" runs on Access/Jet (and the
        // authoritative SQL Server seed), both of which collate case-INSENSITIVELY, so e.g. "accountupdatedtext"
        // groups with "AccountUpdated" and "ASQLqueryhasfailed" sorts by "asql". SQLite's default BINARY ordering
        // is case-SENSITIVE (uppercase before lowercase), which reorders the grid — NOCASE restores parity.
        var rows = new List<LangStringRow>();
        foreach (var r in conn.Query("SELECT variable, LangText FROM tblLangStrings WHERE id=@id ORDER BY variable COLLATE NOCASE ASC", new { id = EnglishId }))
        {
            var d = (IDictionary<string, object>)r;
            var variable = Vb.Str(d["variable"]);
            var current = Vb.Str(conn.ExecuteScalar<object?>("SELECT LangText FROM tblLangStrings WHERE id=@id AND variable=@v", new { id = langId, v = variable }));
            rows.Add(new LangStringRow { Variable = variable, EnglishText = Vb.Str(d["LangText"]), CurrentText = current });
        }

        var def = (IDictionary<string, object>)conn.QueryFirst("SELECT LangName, Localized FROM tblLanguage WHERE id=@id", new { id = EnglishId });
        var cur = (IDictionary<string, object>)conn.QueryFirst("SELECT LangName, Localized FROM tblLanguage WHERE id=@id", new { id = langId });

        ViewData["Title"] = _lang.Lang("HelpDesk") + " " + _lang.Lang("Manage") + " " + _lang.Lang("LanguageStrings");
        return View(new ViewLangStringVm
        {
            LangId = langId,
            DefaultLangLabel = Vb.Str(def["LangName"]) + "(" + Vb.Str(def["Localized"]) + ")",
            CurrentLangLabel = Vb.Str(cur["LangName"]) + "(" + Vb.Str(cur["Localized"]) + ")",
            Rows = rows, SaveSuccess = saveSuccess, AddSuccess = addSuccess,
        });
    }
}
