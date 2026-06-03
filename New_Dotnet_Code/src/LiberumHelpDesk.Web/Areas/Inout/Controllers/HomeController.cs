using Dapper;
using LiberumHelpDesk.Web.Filters;
using LiberumHelpDesk.Web.Models.ViewModels;
using LiberumHelpDesk.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LiberumHelpDesk.Web.Areas.Inout.Controllers;

/// <summary>Ports inout/default.asp (board), details.asp, status.asp, update.asp, savefile.asp.</summary>
[Area("Inout")]
public sealed class HomeController : Controller
{
    private readonly Db _db;
    private readonly IConfigService _config;
    private readonly ISessionContext _session;
    private readonly IUserService _users;
    private readonly ILanguageService _lang;
    private readonly IErrorService _error;
    private readonly IWebHostEnvironment _env;

    public HomeController(Db db, IConfigService config, ISessionContext session, IUserService users,
        ILanguageService lang, IErrorService error, IWebHostEnvironment env)
    {
        _db = db; _config = config; _session = session; _users = users; _lang = lang; _error = error; _env = env;
    }

    private string ImagePath(string uid) => Path.Combine(_env.WebRootPath, "image", uid + ".jpg");

    // inout/default.asp — the status board.
    [HttpGet("/Inout")]
    [HttpPost("/Inout")]
    [CheckUser]
    public IActionResult Index()
    {
        var sid = _session.Sid;
        var inoutadmin = _users.UsrInt(sid, "inoutadmin");
        var f = Request.HasFormContentType ? Request.Form : null;
        var button = f?["button"].ToString() ?? "";

        string mFirstname = "", mLastname = "", mUid = "", mDept = "", mPhone = ""; var mInoutStatus = 0;
        if (button == _lang.Lang("Search"))
        {
            mFirstname = f!["mFirstname"].ToString(); mLastname = f["mLastname"].ToString();
            mUid = f["mUid"].ToString(); mDept = f["mDept"].ToString(); mPhone = f["mPhone"].ToString();
            mInoutStatus = Vb.CInt(f["mInoutStatus"].ToString());
        }

        var sort = Vb.CInt(Request.Query["sort"].ToString());
        var order = Request.Query["order"].ToString().Length > 0 ? Vb.CInt(Request.Query["order"].ToString()) : 0;
        string orderBy;
        int fnO = 0, lnO = 0, stO = 0, phO = 0, uidO = 0, depO = 0;
        switch (sort)
        {
            case 1: orderBy = order == 0 ? "firstname ASC, lastname ASC" : "firstname DESC, lastname DESC"; fnO = order == 0 ? 1 : 0; break;
            case 2: orderBy = order == 0 ? "lastname ASC, firstname ASC" : "lastname DESC, firstname DESC"; lnO = order == 0 ? 1 : 0; break;
            case 3: orderBy = order == 0 ? "statuscode ASC, firstname ASC, lastname ASC" : "statuscode DESC, firstname DESC, lastname DESC"; stO = order == 0 ? 1 : 0; break;
            case 4: orderBy = "phone " + (order == 0 ? "ASC" : "DESC"); phO = order == 0 ? 1 : 0; break;
            case 5: orderBy = "uid " + (order == 0 ? "ASC" : "DESC"); uidO = order == 0 ? 1 : 0; break;
            case 6: orderBy = order == 0 ? "dname ASC, firstname ASC, lastname ASC" : "dname DESC, firstname DESC, lastname DESC"; depO = order == 0 ? 1 : 0; break;
            default: orderBy = order == 0 ? "firstname ASC, lastname ASC" : "firstname DESC, lastname DESC"; fnO = order == 0 ? 1 : 0; break;
        }

        var where = "(tblUsers.department = departments.department_id) AND (ListOnInoutBoard = 1) AND (sid > 0)";
        var p = new DynamicParameters();
        if (mFirstname.Length > 0) { where += " AND (Firstname LIKE @mfn)"; p.Add("mfn", mFirstname + "%"); }
        if (mLastname.Length > 0) { where += " AND (Lastname LIKE @mln)"; p.Add("mln", mLastname + "%"); }
        if (mUid.Length > 0) { where += " AND (uid LIKE @muid)"; p.Add("muid", mUid + "%"); }
        if (mPhone.Length > 0) { where += " AND (phone LIKE @mph)"; p.Add("mph", mPhone + "%"); }
        if (mInoutStatus == 1) where += " AND (statuscode >= 1)";
        if (mDept.Length > 0) { where += " AND (departments.dname LIKE @mdept)"; p.Add("mdept", mDept + "%"); }

        var clearForm = button == _lang.Lang("ClearForm");
        var rows = new List<InoutRow>();
        if (!clearForm)
        {
            foreach (var r in _db.Connection.Query(
                "SELECT tblUsers.*, departments.dname FROM tblUsers, departments WHERE " + where + " ORDER BY " + orderBy, p))
            {
                var d = (IDictionary<string, object>)r;
                var rsid = Vb.CInt(d["sid"]);
                rows.Add(new InoutRow
                {
                    Sid = rsid, Firstname = Vb.Str(d["firstname"]), Lastname = Vb.Str(d["lastname"]),
                    StatusCode = Vb.CInt(d["statuscode"]), StatusText = Vb.Str(d["statustext"]), StatusDate = Vb.Str(d["statusdate"]),
                    Phone = Vb.Str(d["phone"]), Uid = Vb.Str(d["uid"]), Dname = Vb.Str(d["dname"]),
                    CanEdit = inoutadmin == 1 || sid == rsid,
                });
            }
        }

        ViewData["Title"] = _config.GetString("SiteName") + "&nbsp;" + _lang.Lang("InOutBoard");
        ViewData["SiteName"] = _config.GetString("SiteName");
        return View(new InoutBoardVm
        {
            MFirstname = mFirstname, MLastname = mLastname, MUid = mUid, MDept = mDept, MPhone = mPhone,
            MInoutStatusChecked = mInoutStatus == 1, Sort = sort, Order = order,
            FirstNameOrder = fnO, LastNameOrder = lnO, StatusOrder = stO, PhoneOrder = phO, UserIDOrder = uidO, DeptOrder = depO,
            Rows = rows, Count = rows.Count, ClearForm = clearForm,
        });
    }

    // inout/details.asp
    [HttpGet("/Inout/Details")]
    [CheckUser]
    public IActionResult Details()
    {
        if (Request.Query["id"].ToString().Length == 0) throw _error.Error(3, _lang.Lang("NovalidIDgiven"));
        var usid = Vb.CInt(Request.Query["id"].ToString());
        var sid = _session.Sid;
        var inoutadmin = _users.UsrInt(sid, "inoutadmin");

        var u = _db.Connection.QueryFirstOrDefault(
            "SELECT tblUsers.*, departments.dname FROM tblUsers, departments WHERE (sid=@usid) AND (tblUsers.department = departments.department_id)",
            new { usid });
        if (u is null) throw _error.Error(3, _lang.Lang("Noresultsfound"));
        var d = (IDictionary<string, object>)u;
        var uid = Vb.Str(d["uid"]);
        var statusCode = Vb.CInt(d["statuscode"]);

        ViewData["Title"] = _config.GetString("SiteName") + "&nbsp;" + _lang.Lang("InOutBoard");
        ViewData["SiteName"] = _config.GetString("SiteName");
        return View(new InoutDetailsVm
        {
            Usid = usid, Uname = Vb.Str(d["fname"]), Uid = uid, Dname = Vb.Str(d["dname"]),
            Email1 = Vb.Str(d["email1"]), Phone = Vb.Str(d["phone"]),
            PhoneHome = FormatHome(Vb.Str(d["phone_home"])), PhoneMobile = FormatMobile(Vb.Str(d["phone_mobile"])),
            StatusCode = statusCode, StatusText = Vb.Str(d["statustext"]),
            Jobfunction = Vb.Str(d["jobfunction"]), Userresume = Vb.Str(d["userresume"]),
            HasImage = System.IO.File.Exists(ImagePath(uid)),
            CanEdit = usid == sid || inoutadmin == 1,
            CanChangeStatus = statusCode != 2 || inoutadmin == 1,
        });
    }

    // inout/status.asp
    [HttpGet("/Inout/Status")]
    [HttpPost("/Inout/Status")]
    [CheckUser]
    public IActionResult Status()
    {
        if (Request.Query["id"].ToString().Length == 0) throw _error.Error(3, _lang.Lang("NovalidIDgiven"));
        var usid = Vb.CInt(Request.Query["id"].ToString());
        var sid = _session.Sid;
        var inoutadmin = _users.UsrInt(sid, "inoutadmin");

        var saved = false;
        if (Request.HasFormContentType && Request.Form["save"].ToString() == "1")
        {
            int sStatus; string statusText;
            if (Request.Form["frm_status"].ToString() == "on") { sStatus = 1; statusText = Request.Form["frm_statustext"].ToString(); }
            else { sStatus = 0; statusText = ""; }
            _db.Connection.Execute(
                "UPDATE tblUsers SET statuscode=@s, statustext=@t, statusdate=@d WHERE sid=@usid",
                new { s = sStatus, t = statusText, d = DateTime.Now, usid });
            saved = true;
        }

        var u = _db.Connection.QueryFirstOrDefault("SELECT * FROM tblUsers WHERE sid=@usid", new { usid });
        if (u is null) throw _error.Error(3, _lang.Lang("Noresultsfound"));
        var d = (IDictionary<string, object>)u;
        var statusCode = Vb.CInt(d["statuscode"]);
        if (statusCode == 2 && inoutadmin != 1)
            throw _error.Error(3, _lang.Lang("OnlyAdministratorscanchangethisstatus"));

        ViewData["Title"] = _config.GetString("SiteName") + "&nbsp;" + _lang.Lang("InOutBoard");
        ViewData["SiteName"] = _config.GetString("SiteName");
        return View(new InoutStatusVm
        {
            Usid = usid, Uname = Vb.Str(d["fname"]), Checked = statusCode > 0, StatusText = Vb.Str(d["statustext"]), Saved = saved,
        });
    }

    // inout/update.asp
    [HttpGet("/Inout/Update")]
    [HttpPost("/Inout/Update")]
    [CheckUser]
    public IActionResult Update()
    {
        var mId = Vb.CInt(Request.Query["id"].ToString());
        var sid = _session.Sid;
        var inoutadmin = _users.UsrInt(sid, "inoutadmin");

        // Faithful precedence: (mId<>sid AND !admin) OR (len(mId)=0). The original tests len() of the *coerced*
        // integer mId, whose string form is always >=1 char, so that final clause is dead (always false) — an
        // admin with no id falls through to "No results found" rather than being denied.
        if ((mId != sid && inoutadmin != 1) || mId.ToString().Length == 0)
            throw _error.Error(3, _lang.Lang("AccessDenied"));

        var saved = false;
        if (Request.HasFormContentType && Request.Form["save"].ToString() == "1")
        {
            _db.Connection.Execute(
                "UPDATE tblUsers SET phone_home=@ph, phone_mobile=@pm, jobfunction=@jf, userresume=@ur WHERE sid=@mId",
                new
                {
                    ph = Request.Form["frm_phone_home"].ToString(), pm = Request.Form["frm_phone_mobile"].ToString(),
                    jf = Request.Form["frm_jobfunction"].ToString(), ur = Request.Form["frm_userresume"].ToString(), mId
                });
            saved = true;
        }

        var u = _db.Connection.QueryFirstOrDefault(
            "SELECT tblUsers.*, departments.dname FROM tblUsers, departments WHERE (sid=@mId) AND (department = department_id)",
            new { mId });
        if (u is null) throw _error.Error(3, _lang.Lang("Noresultsfound"));
        var d = (IDictionary<string, object>)u;
        var uid = Vb.Str(d["uid"]);

        ViewData["Title"] = _config.GetString("SiteName") + "&nbsp;" + _lang.Lang("InOutBoard");
        ViewData["SiteName"] = _config.GetString("SiteName");
        return View(new InoutUpdateVm
        {
            Mid = mId, Uname = Vb.Str(d["fname"]), Uid = uid, Dname = Vb.Str(d["dname"]), Email1 = Vb.Str(d["email1"]),
            Phone = Vb.Str(d["phone"]), PhoneHome = Vb.Str(d["phone_home"]), PhoneMobile = Vb.Str(d["phone_mobile"]),
            Jobfunction = Vb.Str(d["jobfunction"]), Userresume = Vb.Str(d["userresume"]),
            Saved = saved, HasImage = System.IO.File.Exists(ImagePath(uid)), MaxImageSize = _config.GetString("MaxImageSize"),
        });
    }

    // inout/savefile.asp — image upload.
    [HttpPost("/Inout/SaveFile")]
    [CheckUser]
    public async Task<IActionResult> SaveFile(IFormFile? blob)
    {
        var usid = Vb.CInt(Request.Query["uid"].ToString());
        var uid = _users.UsrString(usid, "uid");
        string message;

        var maxSize = Vb.CInt(_config.GetString("MaxImageSize"));
        if (blob is null || blob.Length == 0)
        {
            message = _lang.Lang("Anerroroccurreduploadingafile") + ": no file";
        }
        else
        {
            var ext = Path.GetExtension(blob.FileName).TrimStart('.').ToLowerInvariant();
            if (ext != "jpg" && ext != "jpeg")
                message = _lang.Lang("Anerroroccurreduploadingafile") + ": invalid type";
            // Faithful FileSizeIsBad(): bad if length > MaximumFileSize OR < 1000, with NO ">0" guard on max
            // (a MaxImageSize of 0 therefore rejects every non-empty file, exactly like the original).
            else if (blob.Length < 1000 || blob.Length > maxSize)
                message = _lang.Lang("Anerroroccurreduploadingafile") + ": size";
            else
            {
                var dir = Path.Combine(_env.WebRootPath, "image");
                Directory.CreateDirectory(dir);
                await using var fs = System.IO.File.Create(Path.Combine(dir, uid + ".jpg"));
                await blob.CopyToAsync(fs);
                message = _lang.Lang("fileuploadedsuccessfully");
            }
        }

        ViewData["Title"] = _config.GetString("SiteName") + "&nbsp;" + _lang.Lang("Uploadimage");
        ViewData["SiteName"] = _config.GetString("SiteName");
        ViewData["Message"] = message;
        ViewData["Usid"] = usid;
        return View();
    }

    private static string FormatHome(string s) =>
        s.Length == 8 ? $"{s[..2]} {s.Substring(2, 2)} {s.Substring(4, 2)} {s.Substring(6, 2)}" : s;

    private static string FormatMobile(string s) =>
        s.Length == 8 ? $"{s[..3]} {s.Substring(3, 2)} {s.Substring(5, 3)}" : s;
}
