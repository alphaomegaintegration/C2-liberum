namespace LiberumHelpDesk.Tests;

public class LanguageServiceTests
{
    [Fact]
    public void Anonymous_user_resolves_default_language_english()
    {
        using var fx = new HelpdeskFixture();   // sid=0, DefaultLanguage=1
        Assert.Equal("Access denied", fx.Lang.Lang("AccessDenied"));
    }

    [Fact]
    public void Missing_key_returns_at_fallback()
    {
        using var fx = new HelpdeskFixture();
        Assert.Equal("@NoSuchKeyXyz@", fx.Lang.Lang("NoSuchKeyXyz"));
    }

    [Fact]
    public void Lookup_is_case_insensitive_despite_sqlite_binary_ordering()
    {
        using var fx = new HelpdeskFixture();
        // The key is "AccessDenied"; requesting it with different casing must still resolve (C3).
        Assert.Equal("Access denied", fx.Lang.Lang("accessdenied"));
        Assert.Equal("Access denied", fx.Lang.Lang("ACCESSDENIED"));

        // Mixed-case keys present in the file must all be findable.
        Assert.False(fx.Lang.Lang("clickfordetails").StartsWith('@'));
        Assert.False(fx.Lang.Lang("BaseURLHelp_2").StartsWith('@'));
        Assert.False(fx.Lang.Lang("CATEGORY_2").StartsWith('@'));
    }

    [Fact]
    public void Switching_default_language_resolves_other_language()
    {
        using var fx = new HelpdeskFixture();
        Assert.Equal("Access denied", fx.Lang.Lang("AccessDenied"));

        // German is id=5; with sid=0 the language id is read from DefaultLanguage on every call.
        fx.Config.Update(new Dictionary<string, object?> { ["DefaultLanguage"] = 5 });
        Assert.Equal("Zugriff verweigert", fx.Lang.Lang("AccessDenied"));
    }

    [Fact]
    public void Logged_in_user_language_preference_is_used_and_cached()
    {
        using var fx = new HelpdeskFixture(seedAdminUser: true);
        // Give the sample user (sid=1) a German preference and log them in.
        Dapper.SqlMapper.Execute(fx.Db.Connection, "UPDATE tblUsers SET [Language] = 5 WHERE uid = 'admin'");
        fx.Session.Sid = 1;
        fx.Session.LanguageId = 0; // force resolution from the user row

        Assert.Equal("Zugriff verweigert", fx.Lang.Lang("AccessDenied"));
        Assert.Equal(5, fx.Session.LanguageId); // cached to session like the original
    }
}
