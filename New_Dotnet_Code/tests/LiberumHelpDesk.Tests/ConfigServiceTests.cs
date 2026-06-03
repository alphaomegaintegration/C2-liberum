using LiberumHelpDesk.Web.Services;

namespace LiberumHelpDesk.Tests;

public class ConfigServiceTests
{
    [Fact]
    public void Reads_seeded_singleton_values()
    {
        using var fx = new HelpdeskFixture();
        Assert.Equal("Company Name", fx.Config.GetString("SiteName"));
        Assert.Equal("admin", fx.Config.GetString("AdminPass"));
        Assert.Equal(2, fx.Config.GetInt("AuthType"));
        Assert.Equal(1, fx.Config.GetInt("DefaultStatus"));
        Assert.Equal(100, fx.Config.GetInt("CloseStatus"));
        Assert.Equal(1, fx.Config.GetInt("DefaultPriority"));
        Assert.Equal("0.98", fx.Config.GetString("Version"));
    }

    [Fact]
    public void Update_persists_and_invalidates_cache()
    {
        using var fx = new HelpdeskFixture();
        Assert.Equal("Company Name", fx.Config.GetString("SiteName"));

        fx.Config.Update(new Dictionary<string, object?>
        {
            ["SiteName"] = "Acme Help Desk",
            ["CloseStatus"] = 100,
        });

        Assert.Equal("Acme Help Desk", fx.Config.GetString("SiteName"));
        // A fresh ConfigService over the same connection sees the persisted change.
        var fresh = new LiberumHelpDesk.Web.Services.ConfigService(fx.Db);
        Assert.Equal("Acme Help Desk", fresh.GetString("SiteName"));
    }

    [Fact]
    public void Invalid_setting_name_is_rejected()
    {
        using var fx = new HelpdeskFixture();
        // Cfg() of an unknown setting renders the faithful DisplayError(3) page instead of a raw 500.
        var ex = Assert.Throws<LhdException>(() => fx.Config.GetString("AdminPass; DROP TABLE tblConfig"));
        Assert.Contains("is an invalid setting.", ex.Html);
        // Update still guards its column whitelist (internal invariant, never user-reachable) with ArgumentException.
        Assert.Throws<ArgumentException>(() => fx.Config.Update(new Dictionary<string, object?> { ["evil"] = 1 }));
    }
}
