using Dapper;
using LiberumHelpDesk.Web.Filters;
using LiberumHelpDesk.Web.Models.ViewModels;
using LiberumHelpDesk.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LiberumHelpDesk.Web.Areas.Admin.Controllers;

/// <summary>Ports admin/viewusers.asp, adduser.asp, moduser.asp (user management).</summary>
[Area("Admin")]
public sealed class UserController : Controller
{
    private readonly Db _db;
    private readonly IConfigService _config;
    private readonly IUserService _users;
    private readonly ILanguageService _lang;
    private readonly IKeyService _keys;
    private readonly IErrorService _error;
    private readonly IWebHostEnvironment _env;

    public UserController(Db db, IConfigService config, IUserService users, ILanguageService lang,
        IKeyService keys, IErrorService error, IWebHostEnvironment env)
    {
        _db = db; _config = config; _users = users; _lang = lang; _keys = keys; _error = error; _env = env;
    }

    private static string Left(string s, int n) { s = s.Trim(); return s.Length > n ? s[..n] : s; }
    private List<Opt> Departments() => _db.Connection.Query("SELECT department_id, dname FROM departments WHERE department_id > 0 ORDER BY dname COLLATE NOCASE ASC")
        .Select(r => { var d = (IDictionary<string, object>)r; return new Opt(Vb.CInt(d["department_id"]), Vb.Str(d["dname"])); }).ToList();
    private List<LangOpt> Languages() => _db.Connection.Query("SELECT id, LangName, Localized FROM tblLanguage")
        .Select(r => { var d = (IDictionary<string, object>)r; return new LangOpt(Vb.CInt(d["id"]), Vb.Str(d["LangName"]), Vb.Str(d["Localized"])); }).ToList();

    // admin/viewusers.asp
    [HttpGet("/Admin/ViewUsers")]
    [CheckAdmin]
    public IActionResult ViewUsers()
    {
        var rows = _db.Connection.Query("SELECT sid, uid, fname FROM tblUsers WHERE sid > 0 ORDER BY uid COLLATE NOCASE ASC")
            .Select(r => { var d = (IDictionary<string, object>)r; return new UserListRowVm { Sid = Vb.CInt(d["sid"]), Uid = Vb.Str(d["uid"]), Fname = Vb.Str(d["fname"]) }; }).ToList();
        ViewData["Title"] = _lang.Lang("HelpDesk") + " - " + _lang.Lang("ManageUsers");
        return View(rows);
    }

    // admin/adduser.asp
    [HttpGet("/Admin/AddUser")]
    [HttpPost("/Admin/AddUser")]
    [CheckAdmin]
    public IActionResult AddUser()
    {
        var success = false;
        if (Request.HasFormContentType && Request.Form["save"].ToString() == "1")
        {
            var f = Request.Form;
            var uid = Left(f["uid"].ToString().ToLowerInvariant(), 50);
            var email = Left(f["email"].ToString().ToLowerInvariant(), 50);
            var firstname = Left(f["firstname"].ToString(), 25);
            var lastname = Left(f["lastname"].ToString(), 24);
            if (uid.Contains('\'')) throw _error.Error(3, _lang.Lang("Username") + "&nbsp;" + _lang.Lang("containsinvalidcharacters") + ".");
            if (email.Length == 0) throw _error.Error(3, _lang.Lang("Emailaddress") + " " + _lang.Lang("isarequiredfield") + ".");
            if (firstname.Length == 0) throw _error.Error(3, _lang.Lang("FirstName") + " " + _lang.Lang("isarequiredfield") + ".");
            if (lastname.Length == 0) throw _error.Error(3, _lang.Lang("LastName") + " " + _lang.Lang("isarequiredfield") + ".");

            _db.Connection.Execute(
                "INSERT INTO tblUsers (sid, uid, email1, email2, fname, firstname, lastname, dateformat, phone, phone_home, " +
                "phone_mobile, location1, department, InoutAdmin, IsRep, RepAccess, statuscode, statustext, statusdate, " +
                "jobfunction, userresume, ListOnInoutboard, [language], [password]) VALUES " +
                "(@sid, @uid, @email, @pager, @fname, @firstname, @lastname, 'yyyy-mm-dd', @phone, @phone_home, @phone_mobile, " +
                "@location, @department, @inoutadmin, @isrep, @repaccess, @statuscode, @statustext, @statusdate, @jobfunction, " +
                "@userresume, @listoninoutboard, @usrLanguage, @newpassword)",
                new
                {
                    sid = _keys.GetUnique("users"), uid, email,
                    pager = Left(f["pager"].ToString().ToLowerInvariant(), 50),
                    fname = firstname + " " + lastname, firstname, lastname,
                    phone = Left(f["phone"].ToString(), 50), phone_home = Left(f["phone_home"].ToString(), 50),
                    phone_mobile = Left(f["phone_mobile"].ToString(), 50), location = Left(f["location"].ToString(), 50),
                    department = Vb.CInt(f["department"].ToString()), inoutadmin = f["inoutadmin"].ToString() == "on" ? 1 : 0,
                    isrep = f["isrep"].ToString() == "on" ? 1 : 0, repaccess = Vb.CInt(f["repaccess"].ToString()),
                    statuscode = Vb.CInt(f["statuscode"].ToString()), statustext = f["statustext"].ToString(), statusdate = DateTime.Now,
                    jobfunction = f["jobfunction"].ToString(), userresume = f["userresume"].ToString(),
                    listoninoutboard = Vb.CInt(f["ListOnInoutBoard"].ToString()), usrLanguage = Vb.CInt(f["usrLanguage"].ToString()),
                    newpassword = Left(f["newpassword"].ToString(), 50),
                });
            success = true;
            ViewData["NewUid"] = uid;
        }

        ViewData["Title"] = _lang.Lang("HelpDesk") + " - " + _lang.Lang("AddNewUsers");
        return View(new UserFormVm
        {
            IsEdit = false, Success = success, UseInout = _config.GetInt("useinoutboard") == 1,
            ShowPager = _config.GetInt("EnablePager") > 0, UsrLanguage = _config.GetInt("DefaultLanguage"),
            ListOnInoutBoard = 1, Departments = Departments(), Languages = Languages(),
        });
    }

    // admin/moduser.asp
    [HttpPost("/Admin/ModUser")]
    [CheckAdmin]
    public IActionResult ModUser()
    {
        var conn = _db.Connection;
        var f = Request.Form;
        var modSid = Vb.CInt(f["usersid"].ToString());
        var success = false;
        var accountDeleted = false;
        var repProbs = false;
        var closeStatus = _config.GetInt("CloseStatus");

        if (f["save"].ToString() == "1")
        {
            var email = Left(f["email"].ToString().ToLowerInvariant(), 50);
            var firstname = Left(f["firstname"].ToString(), 25);
            var lastname = Left(f["lastname"].ToString(), 24);
            if (email.Length == 0) throw _error.Error(3, _lang.Lang("Emailaddress") + _lang.Lang("isarequiredfield") + ".");
            if (firstname.Length == 0) throw _error.Error(3, _lang.Lang("FirstName") + _lang.Lang("isarequiredfield") + ".");
            if (lastname.Length == 0) throw _error.Error(3, _lang.Lang("LastName") + _lang.Lang("isarequiredfield") + ".");

            int isRep;
            if (f["isrep"].ToString() == "on") isRep = 1;
            else if (_users.UsrInt(modSid, "IsRep") == 1)
            {
                // Cannot drop rep status while open problems are assigned.
                var hasOpen = conn.ExecuteScalar<long>("SELECT COUNT(*) FROM problems WHERE rep=@s AND status<>@cs", new { s = modSid, cs = closeStatus }) > 0;
                if (hasOpen) { repProbs = true; isRep = 1; } else isRep = 0;
            }
            else isRep = 0;

            if (!repProbs)
            {
                var p = new DynamicParameters();
                p.Add("email", email); p.Add("pager", Left(f["pager"].ToString().ToLowerInvariant(), 50));
                p.Add("fname", firstname + " " + lastname); p.Add("firstname", firstname); p.Add("lastname", lastname);
                p.Add("phone", Left(f["phone"].ToString(), 50)); p.Add("phone_home", Left(f["phone_home"].ToString(), 50));
                p.Add("phone_mobile", Left(f["phone_mobile"].ToString(), 50)); p.Add("location", Left(f["location"].ToString(), 50));
                p.Add("department", Vb.CInt(f["department"].ToString())); p.Add("isrep", isRep);
                p.Add("repaccess", Vb.CInt(f["repaccess"].ToString())); p.Add("inoutadmin", f["inoutadmin"].ToString() == "on" ? 1 : 0);
                p.Add("statuscode", Vb.CInt(f["statuscode"].ToString())); p.Add("statustext", f["statustext"].ToString());
                p.Add("statusdate", DateTime.Now); p.Add("jobfunction", f["jobfunction"].ToString()); p.Add("userresume", f["userresume"].ToString());
                p.Add("listoninoutboard", Vb.CInt(f["ListOnInoutBoard"].ToString())); p.Add("usrLanguage", Vb.CInt(f["usrLanguage"].ToString()));
                p.Add("modSid", modSid);

                var set = "email1=@email, email2=@pager, fname=@fname, firstname=@firstname, lastname=@lastname, phone=@phone, " +
                          "phone_home=@phone_home, phone_mobile=@phone_mobile, location1=@location, department=@department, IsRep=@isrep, " +
                          "RepAccess=@repaccess, InoutAdmin=@inoutadmin, statuscode=@statuscode, statustext=@statustext, statusdate=@statusdate, " +
                          "jobfunction=@jobfunction, userresume=@userresume, ListOnInoutboard=@listoninoutboard, [Language]=@usrLanguage";
                var newpassword = Left(f["newpassword"].ToString(), 50);
                if (newpassword.Length > 0) { set += ", [password]=@newpassword"; p.Add("newpassword", newpassword); }
                conn.Execute($"UPDATE tblUsers SET {set} WHERE sid=@modSid", p);
                success = true;
            }
        }

        if (f["delete"].ToString() == "1")
        {
            var strUserId = _users.UsrString(modSid, "uid");
            var isRepUser = _users.UsrInt(modSid, "IsRep") == 1;
            if (!isRepUser)
            {
                conn.Execute("DELETE FROM tblUsers WHERE sid=@s", new { s = modSid });
                conn.Execute("UPDATE problems SET entered_by=0 WHERE entered_by=@s", new { s = modSid });
                conn.Execute("UPDATE problems SET rep=0 WHERE rep=@s", new { s = modSid });
                accountDeleted = true;
            }
            else
            {
                if (conn.ExecuteScalar<long>("SELECT COUNT(*) FROM categories WHERE rep_id=@s", new { s = modSid }) > 0)
                    throw _error.Error(3, "Please reassign categories to a different support rep.");
                if (conn.ExecuteScalar<long>("SELECT COUNT(*) FROM problems WHERE rep=@s AND status<>@cs", new { s = modSid, cs = closeStatus }) > 0)
                    repProbs = true;
                else
                {
                    conn.Execute("DELETE FROM tblUsers WHERE sid=@s", new { s = modSid });
                    conn.Execute("UPDATE problems SET entered_by=0 WHERE entered_by=@s", new { s = modSid });
                    accountDeleted = true;
                }
            }
            if (_config.GetInt("UseInoutBoard") == 1 && accountDeleted)
            {
                var img = Path.Combine(_env.WebRootPath, "image", strUserId + ".jpg");
                try { if (System.IO.File.Exists(img)) System.IO.File.Delete(img); } catch { }
            }
        }

        // When the account was just deleted, the user row is gone — the view shows only "Account Deleted"
        // and never renders the form, so do NOT re-read Usr(modSid, ...) (which would now faithfully raise
        // "User not found." for the missing sid). Return the minimal deleted vm instead.
        if (accountDeleted && f["delete"].ToString() == "1")
        {
            ViewData["Title"] = _lang.Lang("HelpDesk") + " - " + _lang.Lang("ModifyUser");
            return View(new UserFormVm
            {
                IsEdit = true, ModSid = modSid, Success = success, AccountDeleted = true,
                UseInout = _config.GetInt("useinoutboard") == 1, Departments = Departments(), Languages = Languages(),
            });
        }

        // Re-load form values (unless the account was just deleted or rep-blocked).
        var vm = new UserFormVm
        {
            IsEdit = true, ModSid = modSid, Success = success, AccountDeleted = false,
            RepProbsError = repProbs,
            Uid = _users.UsrString(modSid, "uid"),
            Email = _users.UsrString(modSid, "email1"), Firstname = _users.UsrString(modSid, "firstname"),
            Lastname = _users.UsrString(modSid, "lastname"), Phone = _users.UsrString(modSid, "phone"),
            PhoneHome = _users.UsrString(modSid, "phone_home"), PhoneMobile = _users.UsrString(modSid, "phone_mobile"),
            Location = _users.UsrString(modSid, "location1"), Pager = _users.UsrString(modSid, "email2"),
            Department = _users.UsrInt(modSid, "department"), UsrLanguage = _users.UsrInt(modSid, "[language]"),
            Statuscode = _users.UsrInt(modSid, "statuscode"), Statustext = _users.UsrString(modSid, "statustext"),
            Jobfunction = _users.UsrString(modSid, "jobfunction"), Userresume = _users.UsrString(modSid, "userresume"),
            ListOnInoutBoard = _users.UsrInt(modSid, "ListOnInoutBoard"), IsRep = _users.UsrInt(modSid, "IsRep") == 1,
            RepAccess = _users.UsrInt(modSid, "RepAccess"), InoutAdmin = _users.UsrInt(modSid, "inoutadmin") == 1,
            ShowPager = _config.GetInt("EnablePager") > 0 && _users.UsrInt(modSid, "IsRep") == 1,
            UseInout = _config.GetInt("useinoutboard") == 1, Departments = Departments(), Languages = Languages(),
        };
        ViewData["Title"] = _lang.Lang("HelpDesk") + " - " + _lang.Lang("ModifyUser");
        return View(vm);
    }
}
