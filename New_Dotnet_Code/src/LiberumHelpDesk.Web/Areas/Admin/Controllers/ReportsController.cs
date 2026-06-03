using Dapper;
using LiberumHelpDesk.Web.Filters;
using LiberumHelpDesk.Web.Models.ViewModels;
using LiberumHelpDesk.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LiberumHelpDesk.Web.Areas.Admin.Controllers;

/// <summary>Ports admin/reports.asp (form) and admin/viewreports.asp (GROUP BY report output).</summary>
[Area("Admin")]
public sealed class ReportsController : Controller
{
    private readonly Db _db;
    private readonly ILanguageService _lang;
    private readonly IDateService _dates;

    public ReportsController(Db db, ILanguageService lang, IDateService dates)
    {
        _db = db; _lang = lang; _dates = dates;
    }

    [HttpGet("/Admin/Reports")]
    [CheckAdmin]
    public IActionResult Reports()
    {
        var now = DateTime.Now;
        var sMonth = now.Month - 1; var sYear = now.Year;
        if (sMonth == 0) { sMonth = 12; sYear -= 1; }
        ViewData["Title"] = _lang.Lang("HelpDesk") + " - " + _lang.Lang("Reports");
        return View(new ReportsFormVm
        {
            SMonth = sMonth, SDay = now.Day, SYear = sYear,
            EMonth = now.Month, EDay = now.Day, EYear = now.Year, CurrentYear = now.Year,
        });
    }

    [HttpPost("/Admin/ViewReports")]
    [CheckAdmin]
    public IActionResult ViewReports()
    {
        var f = Request.Form;
        var type = Vb.CInt(f["type"].ToString());

        var sDay = _dates.FixDay(Vb.CInt(f["s_month"].ToString()), Vb.CInt(f["s_day"].ToString()), Vb.CInt(f["s_year"].ToString()));
        var eDay = _dates.FixDay(Vb.CInt(f["e_month"].ToString()), Vb.CInt(f["e_day"].ToString()), Vb.CInt(f["e_year"].ToString()));
        var start = new DateTime(Vb.CInt(f["s_year"].ToString()), Vb.CInt(f["s_month"].ToString()), sDay, 0, 0, 0);
        var end = new DateTime(Vb.CInt(f["e_year"].ToString()), Vb.CInt(f["e_month"].ToString()), eDay, 23, 59, 59);

        var (sql, headerKey) = type switch
        {
            1 => ("SELECT c.cname AS name, COUNT(*) AS total, SUM(p.time_spent) AS total_time FROM problems p " +
                  "JOIN categories c ON p.category = c.category_id WHERE start_date > @start AND start_date < @end GROUP BY c.cname ORDER BY c.cname ASC", "Category"),
            2 => ("SELECT r.uid AS name, COUNT(*) AS total, SUM(p.time_spent) AS total_time FROM problems p " +
                  "JOIN tblUsers r ON p.rep = r.sid WHERE start_date > @start AND start_date < @end AND r.sid > 0 GROUP BY r.uid ORDER BY r.uid ASC", "Rep"),
            _ => ("SELECT d.dname AS name, COUNT(*) AS total, SUM(p.time_spent) AS total_time FROM problems p " +
                  "JOIN departments d ON p.department = d.department_id WHERE start_date > @start AND start_date < @end GROUP BY d.dname ORDER BY d.dname ASC", "Department"),
        };

        var raw = _db.Connection.Query(sql, new { start, end }).Select(r =>
        {
            var d = (IDictionary<string, object>)r;
            return (Name: Vb.Str(d["name"]), Total: Vb.CInt(d["total"]), TotalTime: Vb.CInt(d["total_time"]));
        }).ToList();

        var grandTotal = raw.Sum(x => x.Total);
        var grandTime = raw.Sum(x => x.TotalTime);

        var rows = raw.Select(x => new ReportRow
        {
            Name = x.Name, Total = x.Total, TotalTime = x.TotalTime,
            AvgTime = x.Total != 0 ? (double)x.TotalTime / x.Total : 0,
            PctProblems = grandTotal != 0 ? (double)x.Total / grandTotal * 100 : 0,
            PctTime = grandTime != 0 ? (double)x.TotalTime / grandTime * 100 : 0,
        }).ToList();

        ViewData["Title"] = _lang.Lang("HelpDesk") + " - " + _lang.Lang("Reports");
        return View(new ViewReportsVm
        {
            Type = type, GroupHeaderKey = headerKey, Rows = rows, Total = grandTotal, TotalTime = grandTime,
            TotalAvg = grandTotal != 0 ? (double)grandTime / grandTotal : 0,
        });
    }
}
