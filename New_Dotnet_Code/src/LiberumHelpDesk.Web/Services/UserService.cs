using Dapper;

namespace LiberumHelpDesk.Web.Services;

/// <summary>
/// Port of <c>Usr</c> (public.asp): <c>SELECT &lt;column&gt; FROM tblUsers WHERE sid=&lt;sid&gt;</c>.
/// The original is called with a column name (sometimes bracketed, e.g. <c>[language]</c>); we strip
/// brackets and whitelist against the real columns.
/// </summary>
public interface IUserService
{
    object? Usr(int sid, string column);
    string UsrString(int sid, string column);
    int UsrInt(int sid, string column);
    bool Exists(int sid);
}

public sealed class UserService : IUserService
{
    public static readonly IReadOnlySet<string> Columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "sid", "uid", "password", "fname", "email1", "email2", "phone", "location1", "location2", "department",
        "IsRep", "dtCreated", "dtLastAccess", "ListOnInoutBoard", "firstname", "lastname", "dateformat",
        "inoutadmin", "phone_home", "phone_mobile", "jobfunction", "userresume", "statustext", "statuscode",
        "statusdate", "Language", "RepAccess"
    };

    private readonly Db _db;

    public UserService(Db db) => _db = db;

    private static string NormalizeColumn(string column)
    {
        var c = column.Trim();
        if (c.StartsWith('[') && c.EndsWith(']')) c = c[1..^1];
        if (!Columns.Contains(c))
            throw new ArgumentException($"Invalid tblUsers column: {column}");
        return c;
    }

    public object? Usr(int sid, string column)
    {
        var col = NormalizeColumn(column);
        var v = _db.Connection.ExecuteScalar<object?>(
            $"SELECT {col} FROM tblUsers WHERE sid = @sid", new { sid });
        if (v is null or DBNull)
        {
            // ExecuteScalar returns null for both a NULL column and a missing row. Faithful Usr() only errors
            // on EOF (no row): render DisplayError(3, "User not found.") and end the request; a present row
            // with a NULL column returns null as before.
            if (!Exists(sid))
                throw ErrorService.Generic("User not found.");
            return null;
        }
        return v;
    }

    public string UsrString(int sid, string column) => Vb.Str(Usr(sid, column));

    public int UsrInt(int sid, string column) => Vb.CInt(Usr(sid, column));

    public bool Exists(int sid) =>
        _db.Connection.ExecuteScalar<long>("SELECT COUNT(*) FROM tblUsers WHERE sid = @sid", new { sid }) > 0;
}
