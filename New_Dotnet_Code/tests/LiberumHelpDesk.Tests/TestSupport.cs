using LiberumHelpDesk.Web.Services;

namespace LiberumHelpDesk.Tests;

internal static class TestPaths
{
    /// <summary>
    /// The web project's Data folder (schema/seed scripts + lang files) is copied into the test
    /// output directory via the web project's CopyToOutputDirectory items, so we read it from there.
    /// </summary>
    public static string WebDataDir()
    {
        var local = Path.Combine(AppContext.BaseDirectory, "Data");
        if (Directory.Exists(local)) return local;
        throw new DirectoryNotFoundException("Could not locate the Data directory at " + local);
    }

    public static SeederPaths SeederPaths(bool seedAdminUser = false)
    {
        var data = WebDataDir();
        return new SeederPaths
        {
            SchemaSqlPath = Path.Combine(data, "schema.sqlite.sql"),
            SeedSqlPath = Path.Combine(data, "seed.sqlite.sql"),
            LangDirPath = Path.Combine(data, "lang"),
            SeedAdminUser = seedAdminUser,
        };
    }
}

/// <summary>An in-memory, mutable session context for tests (no HttpContext).</summary>
internal sealed class TestSessionContext : ISessionContext
{
    public int Sid { get; set; }
    public bool IsAdmin { get; set; }
    public int LanguageId { get; set; }
    public string? ExtUid { get; set; }
    public void SignOut() { Sid = 0; IsAdmin = false; LanguageId = 0; ExtUid = null; }
}

/// <summary>
/// A self-contained in-memory help desk DB with the ported services wired up. One Db = one SQLite
/// connection (mirrors the per-page cnnDB). Disposed with the fixture.
/// </summary>
internal sealed class HelpdeskFixture : IDisposable
{
    public Db Db { get; }
    public KeyService Keys { get; }
    public ConfigService Config { get; }
    public UserService Users { get; }
    public DatabaseSeeder Seeder { get; }
    public TestSessionContext Session { get; }
    public LanguageCache LangCache { get; }
    public LanguageService Lang { get; }
    public DateService Dates { get; }

    public HelpdeskFixture(bool seed = true, bool seedAdminUser = false)
    {
        // A private, single-connection in-memory database (shared cache so the connection persists).
        Db = new Db("Data Source=:memory:");
        Keys = new KeyService(Db);
        Config = new ConfigService(Db);
        Users = new UserService(Db);
        Session = new TestSessionContext();
        LangCache = new LanguageCache();
        Lang = new LanguageService(Db, Session, Config, Users, LangCache);
        Dates = new DateService(Session, Users);
        Seeder = new DatabaseSeeder(Db, Keys, TestPaths.SeederPaths(seedAdminUser));
        if (seed) Seeder.EnsureSeeded();
    }

    public void Dispose() => Db.Dispose();
}
