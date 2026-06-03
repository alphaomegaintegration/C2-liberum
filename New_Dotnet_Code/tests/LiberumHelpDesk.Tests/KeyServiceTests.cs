using Dapper;

namespace LiberumHelpDesk.Tests;

public class KeyServiceTests
{
    [Fact]
    public void GetUnique_returns_then_increments_db_keys()
    {
        using var fx = new HelpdeskFixture();

        // db_keys seeded as problems=1.
        Assert.Equal(1, fx.Keys.GetUnique("problems"));
        Assert.Equal(2, fx.Keys.GetUnique("problems"));
        Assert.Equal(3, fx.Keys.GetUnique("problems"));

        var stored = fx.Db.Connection.ExecuteScalar<long>("SELECT problems FROM db_keys");
        Assert.Equal(4, stored);
    }

    [Fact]
    public void GetUnique_is_case_insensitive_lang_maps_to_Lang_column()
    {
        using var fx = new HelpdeskFixture(seed: false);
        // Seed only schema + base (no language import) so db_keys.Lang stays at 2.
        fx.Db.Connection.Execute("CREATE TABLE db_keys (problems INTEGER, departments INTEGER, categories INTEGER, users INTEGER, Lang INTEGER)");
        fx.Db.Connection.Execute("INSERT INTO db_keys VALUES (1,2,1,1,2)");

        Assert.Equal(2, fx.Keys.GetUnique("lang")); // lowercase request -> Lang column
        Assert.Equal(3, fx.Keys.GetUnique("Lang"));
    }

    [Fact]
    public void GetUnique_rejects_unknown_key()
    {
        using var fx = new HelpdeskFixture();
        Assert.Throws<ArgumentException>(() => fx.Keys.GetUnique("robert'); DROP TABLE db_keys;--"));
    }
}
