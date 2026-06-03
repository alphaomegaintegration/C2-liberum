using Dapper;

namespace LiberumHelpDesk.Web.Services;

/// <summary>
/// Port of <c>Cfg</c> (public.asp): reads the singleton <c>tblConfig</c> row and the admin
/// <c>config.asp</c> write path. The original does <c>SELECT &lt;col&gt; FROM tblConfig</c> with no
/// WHERE on a single-row table; we read the row once per request and serve columns from it.
/// </summary>
public interface IConfigService
{
    object? Get(string setting);
    string GetString(string setting);
    int GetInt(string setting);
    void Update(IDictionary<string, object?> values);
    void Invalidate();
}

public sealed class ConfigService : IConfigService
{
    public static readonly IReadOnlySet<string> Columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "SiteName", "BaseURL", "AdminPass", "EmailType", "SMTPServer", "HDName", "HDReply", "BaseEmail",
        "EnablePager", "NotifyUser", "EnableKB", "DefaultPriority", "DefaultStatus", "CloseStatus", "AuthType",
        "Version", "UseSelectUser", "UseInoutBoard", "KBFreeText", "DefaultLanguage", "AllowImageUpload", "MaxImageSize"
    };

    private readonly Db _db;
    private Dictionary<string, object?>? _row;

    public ConfigService(Db db) => _db = db;

    private Dictionary<string, object?> Row()
    {
        if (_row != null) return _row;
        var raw = _db.Connection.QueryFirstOrDefault("SELECT * FROM tblConfig") as IDictionary<string, object>;
        _row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (raw != null)
            foreach (var kv in raw)
                _row[kv.Key] = kv.Value is DBNull ? null : kv.Value;
        return _row;
    }

    public object? Get(string setting)
    {
        var row = Row();
        // Faithful Cfg(): a setting whose column is unknown, or a missing tblConfig row (EOF), renders the
        // DisplayError(3) red box ("<setting> is an invalid setting.") and ends the request — not a raw 500.
        if (!Columns.Contains(setting) || row.Count == 0)
            throw ErrorService.Generic(setting + " is an invalid setting.");
        row.TryGetValue(setting, out var v);
        return v;
    }

    public string GetString(string setting) => Vb.Str(Get(setting));

    public int GetInt(string setting) => Vb.CInt(Get(setting));

    public void Update(IDictionary<string, object?> values)
    {
        var sets = new List<string>();
        var p = new DynamicParameters();
        var n = 0;
        foreach (var kv in values)
        {
            if (!Columns.Contains(kv.Key))
                throw new ArgumentException($"Invalid config setting: {kv.Key}");
            var pn = "@p" + n++;
            sets.Add($"{kv.Key} = {pn}");
            p.Add(pn, kv.Value);
        }
        if (sets.Count == 0) return;
        _db.Connection.Execute("UPDATE tblConfig SET " + string.Join(", ", sets), p);
        Invalidate();
    }

    public void Invalidate() => _row = null;
}
