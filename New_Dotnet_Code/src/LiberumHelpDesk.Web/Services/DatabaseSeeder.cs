using System.Text;
using Dapper;

namespace LiberumHelpDesk.Web.Services;

public sealed class SeederPaths
{
    public required string SchemaSqlPath { get; init; }
    public required string SeedSqlPath { get; init; }
    public required string LangDirPath { get; init; }

    /// <summary>
    /// Opt-in: seed a sample rep/admin user so DB-auth login is testable immediately. OFF by default
    /// because the stock seed has NO login-able user (admin is the AdminPass gate). Sample rows are
    /// excluded from parity DB-state diffs.
    /// </summary>
    public bool SeedAdminUser { get; init; }
}

/// <summary>
/// Replica of <c>setup.asp</c> for a fresh install: applies schema.sqlite.sql, the helpdesk.sql base
/// seed, then imports the seven language files into tblLangStrings in <c>UpdateAllLanguages</c> order.
/// Idempotent: schema uses CREATE TABLE IF NOT EXISTS, and base seed/import run only on an empty DB.
/// </summary>
public interface IDatabaseSeeder
{
    void EnsureSeeded();
}

public sealed class DatabaseSeeder : IDatabaseSeeder
{
    // (LangName, Localized, FileName) in setup.asp UpdateAllLanguages order. English is forced to
    // id=1 (it already exists from the helpdesk.sql seed); the rest get GetUnique('Lang') => 2..7.
    private static readonly (string Name, string Localized, string File)[] Languages =
    {
        ("English",   "English",    "English_English.txt"),
        ("Norwegian", "Norsk",      "Norwegian_Norsk.txt"),
        ("Danish",    "Dansk",      "Danish_Dansk.txt"),
        ("Dutch",     "Nederlands", "Dutch_Nederlands.txt"),
        ("German",    "Deutsch",    "German_Deutsch.txt"),
        ("French",    "Français",   "French_Français.txt"),
        ("Spanish",   "Español",    "Spanish_Español.txt"),
    };

    // Legacy SQL seed does not include category rows; create a practical starter list when empty.
    // IDs are intentionally high to avoid interfering with legacy-id assumptions in tests/fixtures.
    private static readonly (int Id, string Name)[] DefaultCategories =
    {
        (1001, "General"),
        (1002, "Hardware"),
        (1003, "Software"),
        (1004, "Network"),
        (1005, "Account Access"),
    };

    private readonly Db _db;
    private readonly IKeyService _keys;
    private readonly SeederPaths _paths;

    static DatabaseSeeder() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public DatabaseSeeder(Db db, IKeyService keys, SeederPaths paths)
    {
        _db = db;
        _keys = keys;
        _paths = paths;
    }

    public void EnsureSeeded()
    {
        // 1. Schema (idempotent).
        ExecuteScript(File.ReadAllText(_paths.SchemaSqlPath));

        // 2. Base seed + language import only when the DB is empty.
        var seeded = _db.Connection.ExecuteScalar<long>("SELECT COUNT(*) FROM tblConfig") > 0;
        if (!seeded)
        {
            ExecuteScript(File.ReadAllText(_paths.SeedSqlPath));
            ImportAllLanguages();
            if (_paths.SeedAdminUser) SeedSampleUsers();
        }

        // Keep new and existing installs usable: if categories are missing, add a starter list.
        EnsureDefaultCategories();
    }

    private void EnsureDefaultCategories()
    {
        var categoryCount = _db.Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM categories WHERE category_id > 0");
        if (categoryCount > 0) return;

        var repId = _db.Connection.ExecuteScalar<int?>(
            "SELECT sid FROM tblUsers WHERE IsRep = 1 ORDER BY sid ASC LIMIT 1") ?? 0;

        using var tx = _db.Connection.BeginTransaction();
        foreach (var (id, name) in DefaultCategories)
        {
            _db.Connection.Execute(
                "INSERT OR IGNORE INTO categories (category_id, cname, rep_id) VALUES (@id, @name, @rep)",
                new { id, name, rep = repId }, tx);
        }
        tx.Commit();
    }

    private void ExecuteScript(string sql)
    {
        using var cmd = _db.Connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    // Replicates setup.asp UpdateLang for each language file.
    private void ImportAllLanguages()
    {
        var enc = Encoding.GetEncoding(1252); // OpenTextFile(...,1,0) used the system ANSI codepage (CP1252).
        foreach (var (name, localized, file) in Languages)
        {
            var existing = _db.Connection.ExecuteScalar<object?>(
                "SELECT id FROM tblLanguage WHERE LangName = @n AND Localized = @l",
                new { n = name, l = localized });

            int langId;
            if (existing is null or DBNull)
            {
                langId = _keys.GetUnique("Lang");
                _db.Connection.Execute(
                    "INSERT INTO tblLanguage (id, LangName, Localized) VALUES (@id, @n, @l)",
                    new { id = langId, n = name, l = localized });
            }
            else
            {
                langId = Vb.CInt(existing);
            }

            var path = Path.Combine(_paths.LangDirPath, file);
            if (!File.Exists(path)) continue;

            using var txn = _db.Connection.BeginTransaction();
            foreach (var rawLine in File.ReadAllLines(path, enc))
            {
                var line = rawLine.Trim();
                if (line.Length == 0) continue;
                if (line[0] == ';' || line[0] == '[') continue; // comment / section header

                var idx = line.IndexOf('='); // Split(line, "=", 2): only the first '=' splits.
                if (idx <= 0) continue;
                var variable = line[..idx].Trim();
                var text = line[(idx + 1)..].Trim();
                if (variable.Length == 0 || text.Length == 0) continue;

                // Fresh install: no pre-existing rows, so the Overwrite/insert branches collapse to a plain insert.
                _db.Connection.Execute(
                    "INSERT INTO tblLangStrings (id, variable, LangText) VALUES (@id, @v, @t)",
                    new { id = langId, v = variable, t = text }, txn);
            }
            txn.Commit();
        }
    }

    // Opt-in convenience only (see SeederPaths.SeedAdminUser).
    private void SeedSampleUsers()
    {
        var sid = _keys.GetUnique("users");
        _db.Connection.Execute(
            @"INSERT INTO tblUsers (sid, uid, password, fname, email1, IsRep, RepAccess, dateformat, [Language])
              VALUES (@sid, 'admin', 'admin', 'Administrator', 'admin@localhost', 1, 1, 'yyyy-mm-dd', 1)",
            new { sid });
    }
}
