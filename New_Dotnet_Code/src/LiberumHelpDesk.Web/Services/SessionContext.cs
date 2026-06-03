using Microsoft.AspNetCore.Http;

namespace LiberumHelpDesk.Web.Services;

/// <summary>
/// Wraps the Classic ASP Session("lhd_*") state. <see cref="Sid"/> mirrors GetSid (0 when absent or ≤0).
/// </summary>
public interface ISessionContext
{
    int Sid { get; set; }            // lhd_sid
    bool IsAdmin { get; set; }       // lhd_IsAdmin
    int LanguageId { get; set; }     // lhd_LanguageID (0 when absent)
    string? ExtUid { get; set; }     // lhd_ext_uid

    /// <summary>logoff.asp: clears lhd_LanguageID, lhd_IsAdmin, lhd_sid.</summary>
    void SignOut();
}

public sealed class HttpSessionContext : ISessionContext
{
    private readonly IHttpContextAccessor _accessor;
    public HttpSessionContext(IHttpContextAccessor accessor) => _accessor = accessor;

    private ISession S => _accessor.HttpContext?.Session
        ?? throw new InvalidOperationException("No active session.");

    public int Sid
    {
        get { var v = S.GetInt32("lhd_sid"); return v is > 0 ? v.Value : 0; }
        set => S.SetInt32("lhd_sid", value);
    }

    public bool IsAdmin
    {
        get => S.GetInt32("lhd_IsAdmin") == 1;
        set => S.SetInt32("lhd_IsAdmin", value ? 1 : 0);
    }

    public int LanguageId
    {
        get => S.GetInt32("lhd_LanguageID") ?? 0;
        set => S.SetInt32("lhd_LanguageID", value);
    }

    public string? ExtUid
    {
        get => S.GetString("lhd_ext_uid");
        set { if (value is null) S.Remove("lhd_ext_uid"); else S.SetString("lhd_ext_uid", value); }
    }

    public void SignOut()
    {
        S.Remove("lhd_LanguageID");
        S.Remove("lhd_IsAdmin");
        S.Remove("lhd_sid");
    }
}
