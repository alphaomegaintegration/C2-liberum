using Dapper;

namespace LiberumHelpDesk.Web.Services;

/// <summary>Port of <c>GetUnique</c> (public.asp): the db_keys application-level sequence.</summary>
public interface IKeyService
{
    int GetUnique(string dbname);
}

public sealed class KeyService : IKeyService
{
    // The five db_keys columns. 'lang' in the ASP maps to column 'Lang' (SQLite is case-insensitive).
    private static readonly string[] Columns = { "problems", "departments", "categories", "users", "Lang" };

    private readonly Db _db;

    public KeyService(Db db) => _db = db;

    public int GetUnique(string dbname)
    {
        var col = Array.Find(Columns, c => string.Equals(c, dbname, StringComparison.OrdinalIgnoreCase))
                  ?? throw new ArgumentException($"Invalid db_keys name: {dbname}");

        // Original does SELECT then UPDATE on the same connection (non-atomic). We wrap them in a
        // transaction so the observable id sequence is identical but the race is removed.
        using var txn = _db.Connection.BeginTransaction();
        var key = Vb.CInt(_db.Connection.ExecuteScalar<object?>(
            $"SELECT {col} FROM db_keys", transaction: txn));
        _db.Connection.Execute(
            $"UPDATE db_keys SET {col} = {col} + 1", transaction: txn);
        txn.Commit();
        return key;
    }
}
