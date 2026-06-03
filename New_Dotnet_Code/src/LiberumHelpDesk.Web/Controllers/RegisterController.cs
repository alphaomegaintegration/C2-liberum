using Dapper;
using LiberumHelpDesk.Web.Models.ViewModels;
using LiberumHelpDesk.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace LiberumHelpDesk.Web.Controllers;

/// <summary>Port of register.asp — registration, profile editing, and password changes.</summary>
[Route("Register")]
public sealed class RegisterController : Controller
{
    private readonly Db _db;
    private readonly IConfigService _config;
    private readonly ISessionContext _session;
    private readonly IUserService _users;
    private readonly ILanguageService _lang;
    private readonly IKeyService _keys;
    private readonly IErrorService _error;

    public RegisterController(Db db, IConfigService config, ISessionContext session, IUserService users,
        ILanguageService lang, IKeyService keys, IErrorService error)
    {
        _db = db; _config = config; _session = session; _users = users; _lang = lang; _keys = keys; _error = error;
    }

    private static string LeftTrim(string s, int n) { s = s.Trim(); return s.Length > n ? s[..n] : s; }

    [HttpGet]
    [HttpPost]
    public IActionResult Index()
    {
        var sid = _session.Sid;
        var f = Request.HasFormContentType ? Request.Form : null;
        var edit = Request.Query["edit"].ToString() == "1" || (f? ["edit"].ToString() == "1");
        var success = false;

        if (f != null && f["create"].ToString() == "1")
        {
            var uid = LeftTrim(f["uid"].ToString().ToLowerInvariant(), 50);
            var email = LeftTrim(f["email"].ToString().ToLowerInvariant(), 50);
            var pager = LeftTrim(f["pager"].ToString().ToLowerInvariant(), 50);
            var password1 = LeftTrim(f["password1"].ToString(), 50);
            var password2 = LeftTrim(f["password2"].ToString(), 50);
            var phone = LeftTrim(f["phone"].ToString(), 50);
            var location = LeftTrim(f["location"].ToString(), 50);
            var department = Vb.CInt(f["department"].ToString());
            var usrLanguage = Vb.CInt(f["usrLanguage"].ToString());
            var firstname = LeftTrim(f["firstname"].ToString(), 25);
            var lastname = LeftTrim(f["lastname"].ToString(), 24);
            var fname = firstname + " " + lastname;
            var dateformat = LeftTrim(f["dateformat"].ToString().ToLowerInvariant(), 12);
            var phoneHome = ""; var phoneMobile = "";
            if (_config.GetInt("useinoutboard") == 1)
            {
                phoneHome = LeftTrim(f["phone_home"].ToString(), 50);
                phoneMobile = LeftTrim(f["phone_mobile"].ToString(), 50);
            }

            if (email.Length == 0) throw _error.Error(3, _lang.Lang("Emailaddress") + " " + _lang.Lang("isarequiredfield") + ".");
            if (firstname.Length == 0) throw _error.Error(3, _lang.Lang("FirstName") + " " + _lang.Lang("isarequiredfield") + ".");
            if (lastname.Length == 0) throw _error.Error(3, _lang.Lang("LastName") + " " + _lang.Lang("isarequiredfield") + ".");

            if (edit)
            {
                var oldpassword = LeftTrim(f["oldpassword"].ToString(), 50);
                var changingPassword = oldpassword.Length > 0 || password1.Length > 0;
                if (changingPassword)
                {
                    if (password1 != password2) throw _error.Error(3, _lang.Lang("Passwordsdonotmatch") + ".");
                    if (oldpassword != _users.UsrString(sid, "password")) throw _error.Error(3, _lang.Lang("Passwordisincorrect") + ".");
                }

                var set = "email1=@email, email2=@pager, fname=@fname, firstname=@firstname, lastname=@lastname, " +
                          "phone=@phone, phone_home=@phoneHome, phone_mobile=@phoneMobile, location1=@location, " +
                          "[language]=@usrLanguage, dateformat=@dateformat, department=@department";
                if (changingPassword) set += ", [password]=@password1";

                _db.Connection.Execute($"UPDATE tblUsers SET {set} WHERE sid=@sid",
                    new { email, pager, fname, firstname, lastname, phone, phoneHome, phoneMobile, location,
                          usrLanguage, dateformat, department, password1, sid });
                success = true;
                _session.LanguageId = 0; // Session("lhd_LanguageID") = Empty
            }
            else
            {
                if (uid.Length == 0) throw _error.Error(3, _lang.Lang("Username") + "&nbsp;" + _lang.Lang("isarequiredfield") + ".");
                if (uid.Contains('\'')) throw _error.Error(3, _lang.Lang("Username") + "&nbsp;" + _lang.Lang("containsinvalidcharacters") + ".");
                if (password1 != password2) throw _error.Error(3, _lang.Lang("Passwordsdonotmatch") + ".");
                if (password1.Length == 0) throw _error.Error(3, _lang.Lang("Password") + "&nbsp;" + _lang.Lang("isarequiredfield") + ".");

                var exists = _db.Connection.ExecuteScalar<object?>(
                    "SELECT uid FROM tblUsers WHERE uid=@uid COLLATE NOCASE", new { uid });
                if (exists is not null)
                    throw _error.Error(3, _lang.Lang("Username") + "&nbsp;" + _lang.Lang("alreadyinuse") + ".");

                var newSid = _keys.GetUnique("users");
                _db.Connection.Execute(
                    "INSERT INTO tblUsers (sid, uid, [password], email1, email2, fname, firstname, lastname, phone, " +
                    "phone_home, phone_mobile, location1, dateformat, [language], department) VALUES " +
                    "(@newSid, @uid, @password1, @email, @pager, @fname, @firstname, @lastname, @phone, " +
                    "@phoneHome, @phoneMobile, @location, @dateformat, @usrLanguage, @department)",
                    new { newSid, uid, password1, email, pager, fname, firstname, lastname, phone,
                          phoneHome, phoneMobile, location, dateformat, usrLanguage, department });
                success = true;
            }
        }

        var vm = new RegisterViewModel
        {
            Edit = edit,
            Success = success,
            AuthType = _config.GetInt("AuthType"),
            ShowPager = _config.GetInt("EnablePager") > 0 && _users.UsrInt(sid, "IsRep") == 1,
            UseInout = _config.GetInt("useinoutboard") == 1,
            UidDisplay = edit ? _users.UsrString(sid, "uid") : "",
            FrmEmail = edit ? _users.UsrString(sid, "email1") : "",
            FrmPhone = edit ? _users.UsrString(sid, "phone") : "",
            FrmLocation = edit ? _users.UsrString(sid, "location1") : "",
            FrmDepartment = edit ? _users.UsrInt(sid, "department") : 0,
            FrmPager = edit ? _users.UsrString(sid, "email2") : "",
            FrmFirstname = edit ? _users.UsrString(sid, "firstname") : "",
            FrmLastname = edit ? _users.UsrString(sid, "lastname") : "",
            FrmDateFormat = edit ? _users.UsrString(sid, "dateformat") : "",
            FrmUsrLanguage = edit ? _users.UsrInt(sid, "[language]") : 0,
            FrmPhoneHome = edit && _config.GetInt("useinoutboard") == 1 ? _users.UsrString(sid, "phone_home") : "",
            FrmPhoneMobile = edit && _config.GetInt("useinoutboard") == 1 ? _users.UsrString(sid, "phone_mobile") : "",
            Departments = Departments(),
            Languages = Languages(),
        };

        ViewData["Title"] = _lang.Lang("HelpDesk") + " &nbsp;-&nbsp; " + _lang.Lang("Register");
        return View(vm);
    }

    private List<Opt> Departments()
    {
        var list = new List<Opt>();
        foreach (var r in _db.Connection.Query("SELECT department_id, dname FROM departments WHERE department_id > 0 ORDER BY dname COLLATE NOCASE ASC"))
        {
            var d = (IDictionary<string, object>)r;
            list.Add(new Opt(Vb.CInt(d["department_id"]), Vb.Str(d["dname"])));
        }
        return list;
    }

    private List<LangOpt> Languages()
    {
        var list = new List<LangOpt>();
        foreach (var r in _db.Connection.Query("SELECT id, LangName, Localized FROM tblLanguage"))
        {
            var d = (IDictionary<string, object>)r;
            list.Add(new LangOpt(Vb.CInt(d["id"]), Vb.Str(d["LangName"]), Vb.Str(d["Localized"])));
        }
        return list;
    }
}
