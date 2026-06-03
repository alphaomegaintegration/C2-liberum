using System.Collections.Concurrent;
using Dapper;

namespace LiberumHelpDesk.Web.Services;

/// <summary>
/// App-wide language string cache, mirroring the original Application("lhd_LangCache_&lt;id&gt;") arrays.
/// Singleton. Cleared by ClearLangCache (admin language edits / DefaultLanguage change).
/// </summary>
public interface ILanguageCache
{
    (string Key, string Value)[] GetOrLoad(int langId, Func<(string Key, string Value)[]> loader);
    void Clear();
}

public sealed class LanguageCache : ILanguageCache
{
    private readonly ConcurrentDictionary<int, (string, string)[]> _cache = new();

    public (string, string)[] GetOrLoad(int langId, Func<(string, string)[]> loader)
        => _cache.GetOrAdd(langId, _ => loader());

    public void Clear() => _cache.Clear();
}

/// <summary>
/// Port of <c>Lang</c> / <c>ArrayFind</c> (public.asp). Resolves the language id from session / user /
/// config, then binary-searches a sorted (variable, text) array. Fallbacks: missing key =&gt; "@var@",
/// empty value =&gt; "!var!".
/// </summary>
public interface ILanguageService
{
    string Lang(string variable);
    void ClearCache();
}

public sealed class LanguageService : ILanguageService
{
    // The original sorts with SQL Server's (case-insensitive) collation then binary-searches with
    // StrComp(UCase, UCase). SQLite's default ORDER BY is case-sensitive BINARY, which would break the
    // search, so we re-sort in memory with this same case-insensitive ordinal comparer (plan C3).
    private static readonly StringComparer Cmp = StringComparer.OrdinalIgnoreCase;

    private readonly Db _db;
    private readonly ISessionContext _session;
    private readonly IConfigService _config;
    private readonly IUserService _users;
    private readonly ILanguageCache _cache;

    public LanguageService(Db db, ISessionContext session, IConfigService config, IUserService users, ILanguageCache cache)
    {
        _db = db;
        _session = session;
        _config = config;
        _users = users;
        _cache = cache;
    }

    public string Lang(string variable)
    {
        var langId = ResolveLanguageId();
        var arr = _cache.GetOrLoad(langId, () => LoadLanguage(langId));

        var found = ArrayFind(arr, variable);
        if (found is null) return "@" + variable + "@";   // key not found
        if (found.Length < 1) return "!" + variable + "!"; // present but empty
        return found;
    }

    public void ClearCache() => _cache.Clear();

    // Lang(): sid==0 -> DefaultLanguage; else session lhd_LanguageID; if <1, user.[language]; if 0/null,
    // DefaultLanguage; then cache to session.
    private int ResolveLanguageId()
    {
        var sid = _session.Sid;
        if (sid == 0)
            return _config.GetInt("DefaultLanguage");

        var langId = _session.LanguageId;
        if (langId < 1)
        {
            langId = _users.UsrInt(sid, "[language]");
            if (langId == 0)
                langId = _config.GetInt("DefaultLanguage");
            _session.LanguageId = langId;
        }
        return langId;
    }

    private (string Key, string Value)[] LoadLanguage(int id)
    {
        var rows = _db.Connection.Query(
            "SELECT variable, LangText FROM tblLangStrings WHERE id = @id", new { id });

        var list = new List<(string Key, string Value)>();
        foreach (var r in rows)
        {
            var d = (IDictionary<string, object>)r;
            list.Add((Vb.Str(d["variable"]), Vb.Str(d["LangText"])));
        }

        var arr = list.ToArray();
        Array.Sort(arr, (a, b) => Cmp.Compare(a.Key, b.Key));
        return arr;
    }

    // Binary search mirroring ArrayFind (returns null when not found).
    private static string? ArrayFind((string Key, string Value)[] arr, string key)
    {
        int lo = 0, hi = arr.Length - 1;
        while (lo <= hi)
        {
            var mid = (lo + hi) / 2;
            var c = Cmp.Compare(key, arr[mid].Key);
            if (c == 0) return arr[mid].Value;
            if (c < 0) hi = mid - 1;
            else lo = mid + 1;
        }
        return null;
    }
}
