using Dapper;

namespace LiberumHelpDesk.Tests;

public class SeederTests
{
    [Fact]
    public void Base_seed_creates_lookup_rows()
    {
        using var fx = new HelpdeskFixture();
        var c = fx.Db.Connection;

        Assert.Equal(1, c.ExecuteScalar<long>("SELECT COUNT(*) FROM tblConfig"));
        Assert.Equal(3, c.ExecuteScalar<long>("SELECT COUNT(*) FROM status"));
        Assert.Equal("CLOSED", c.ExecuteScalar<string>("SELECT sname FROM status WHERE status_id = 100"));
        Assert.Equal(3, c.ExecuteScalar<long>("SELECT COUNT(*) FROM priority"));
        Assert.Equal(2, c.ExecuteScalar<long>("SELECT COUNT(*) FROM departments"));
        Assert.Equal(6, c.ExecuteScalar<long>("SELECT COUNT(*) FROM tblConfig_Email"));
        Assert.Equal(3, c.ExecuteScalar<long>("SELECT COUNT(*) FROM tblConfig_Auth"));

        // The unknown user (sid=0) is the only stock account.
        Assert.Equal("unknown", c.ExecuteScalar<string>("SELECT uid FROM tblUsers WHERE sid = 0"));
        Assert.Equal(1, c.ExecuteScalar<long>("SELECT COUNT(*) FROM tblUsers"));
    }

    [Fact]
    public void Email_templates_are_seeded_with_CR_line_separators()
    {
        using var fx = new HelpdeskFixture();
        var c = fx.Db.Connection;

        Assert.Equal(7, c.ExecuteScalar<long>("SELECT COUNT(*) FROM tblEmailMsg"));
        Assert.Equal("HELPDESK: Problem [problemid] Created",
            c.ExecuteScalar<string>("SELECT subject FROM tblEmailMsg WHERE type = 'usernew'"));

        var body = c.ExecuteScalar<string>("SELECT body FROM tblEmailMsg WHERE type = 'usernew'")!;
        Assert.Contains("PROBLEM DETAILS", body);
        Assert.Contains("[description]", body);
        Assert.Contains("\r", body); // CHAR(13) line separators preserved
        Assert.DoesNotContain("\n", body);
    }

    [Fact]
    public void Languages_import_in_UpdateAllLanguages_order_with_expected_ids()
    {
        using var fx = new HelpdeskFixture();
        var c = fx.Db.Connection;

        Assert.Equal(7, c.ExecuteScalar<long>("SELECT COUNT(*) FROM tblLanguage"));
        Assert.Equal("English",   c.ExecuteScalar<string>("SELECT LangName FROM tblLanguage WHERE id = 1"));
        Assert.Equal("Norwegian", c.ExecuteScalar<string>("SELECT LangName FROM tblLanguage WHERE id = 2"));
        Assert.Equal("Danish",    c.ExecuteScalar<string>("SELECT LangName FROM tblLanguage WHERE id = 3"));
        Assert.Equal("Dutch",     c.ExecuteScalar<string>("SELECT LangName FROM tblLanguage WHERE id = 4"));
        Assert.Equal("German",    c.ExecuteScalar<string>("SELECT LangName FROM tblLanguage WHERE id = 5"));
        Assert.Equal("French",    c.ExecuteScalar<string>("SELECT LangName FROM tblLanguage WHERE id = 6"));
        Assert.Equal("Spanish",   c.ExecuteScalar<string>("SELECT LangName FROM tblLanguage WHERE id = 7"));

        // db_keys.Lang advanced from 2 to 8 (six non-English languages consumed 2..7).
        Assert.Equal(8, c.ExecuteScalar<long>("SELECT Lang FROM db_keys"));
    }

    [Fact]
    public void English_strings_are_imported_under_id_1()
    {
        using var fx = new HelpdeskFixture();
        var c = fx.Db.Connection;

        var count = c.ExecuteScalar<long>("SELECT COUNT(*) FROM tblLangStrings WHERE id = 1");
        Assert.True(count > 300, $"expected >300 English strings, got {count}");

        Assert.Equal("Access denied",
            c.ExecuteScalar<string>("SELECT LangText FROM tblLangStrings WHERE id = 1 AND variable = 'AccessDenied'"));
    }

    [Fact]
    public void Accented_languages_decode_with_cp1252()
    {
        using var fx = new HelpdeskFixture();
        var c = fx.Db.Connection;

        // German (id=5) must have imported strings, and umlauts must round-trip (0xFC -> ü), not mojibake.
        var german = c.ExecuteScalar<long>("SELECT COUNT(*) FROM tblLangStrings WHERE id = 5");
        Assert.True(german > 300, $"expected >300 German strings, got {german}");

        var anyUmlaut = c.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM tblLangStrings WHERE id = 5 AND (LangText LIKE '%ü%' OR LangText LIKE '%ä%' OR LangText LIKE '%ö%')");
        Assert.True(anyUmlaut > 0, "expected at least one German string containing an umlaut");
    }

    [Fact]
    public void EnsureSeeded_is_idempotent()
    {
        using var fx = new HelpdeskFixture();   // seeds once
        fx.Seeder.EnsureSeeded();               // second call must be a no-op
        fx.Seeder.EnsureSeeded();

        var c = fx.Db.Connection;
        Assert.Equal(1, c.ExecuteScalar<long>("SELECT COUNT(*) FROM tblConfig"));
        Assert.Equal(7, c.ExecuteScalar<long>("SELECT COUNT(*) FROM tblLanguage"));
        Assert.Equal(7, c.ExecuteScalar<long>("SELECT COUNT(*) FROM tblEmailMsg"));
    }

    [Fact]
    public void SeedAdminUser_opt_in_creates_a_login_able_rep()
    {
        using var fx = new HelpdeskFixture(seedAdminUser: true);
        var c = fx.Db.Connection;

        var isRep = c.ExecuteScalar<long>("SELECT IsRep FROM tblUsers WHERE uid = 'admin'");
        Assert.Equal(1, isRep);
        Assert.Equal("admin", c.ExecuteScalar<string>("SELECT password FROM tblUsers WHERE uid = 'admin'"));
    }
}
