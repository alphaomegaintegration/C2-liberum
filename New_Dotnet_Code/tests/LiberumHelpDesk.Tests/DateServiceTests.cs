using System.Globalization;
using Dapper;

namespace LiberumHelpDesk.Tests;

public class DateServiceTests
{
    private static void WithEnUs(Action body)
    {
        var prev = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        try { body(); }
        finally { CultureInfo.CurrentCulture = prev; }
    }

    [Fact]
    public void DisplayDate_uses_unpadded_components_in_user_format()
    {
        using var fx = new HelpdeskFixture(); // sid=0, dateformat 'yyyy-mm-dd'
        Assert.Equal("2026-6-2", fx.Dates.DisplayDate("2026-06-02 09:05:03", withTime: false));
    }

    [Fact]
    public void DisplayDate_with_time_appends_locale_long_time()
    {
        WithEnUs(() =>
        {
            using var fx = new HelpdeskFixture();
            Assert.Equal("2026-6-2 9:05:03 AM", fx.Dates.DisplayDate("2026-06-02 09:05:03", withTime: true));
        });
    }

    [Fact]
    public void DisplayDate_honours_a_different_user_format()
    {
        using var fx = new HelpdeskFixture();
        fx.Db.Connection.Execute("UPDATE tblUsers SET dateformat = 'mm/dd/yyyy' WHERE sid = 0");
        Assert.Equal("6/2/2026", fx.Dates.DisplayDate("2026-06-02 09:05:03", withTime: false));
    }

    [Fact]
    public void DisplayDate_empty_input_is_empty()
    {
        using var fx = new HelpdeskFixture();
        Assert.Equal("", fx.Dates.DisplayDate("", withTime: true));
        Assert.Equal("", fx.Dates.DisplayDate(null, withTime: false));
    }

    [Fact]
    public void SqlDate_is_unpadded_and_delimited()
    {
        using var fx = new HelpdeskFixture();
        Assert.Equal("'2026-6-2 9:5:3'", fx.Dates.SqlDate("2026-06-02 09:05:03", addDelim: true));
        Assert.Equal("2026-6-2 9:5:3", fx.Dates.SqlDate("2026-06-02 09:05:03", addDelim: false));
        Assert.Equal("", fx.Dates.SqlDate("", addDelim: true));
    }

    [Fact]
    public void ConvertFormattedDate_round_trips_iso_format()
    {
        using var fx = new HelpdeskFixture(); // dateformat 'yyyy-mm-dd'
        var dt = fx.Dates.ConvertFormattedDate("2026-06-02");
        Assert.Equal(new DateTime(2026, 6, 2), dt);
    }

    [Fact]
    public void ConvertFormattedDate_round_trips_us_format()
    {
        using var fx = new HelpdeskFixture();
        fx.Db.Connection.Execute("UPDATE tblUsers SET dateformat = 'mm/dd/yyyy' WHERE sid = 0");
        var dt = fx.Dates.ConvertFormattedDate("6/2/2026");
        Assert.Equal(new DateTime(2026, 6, 2), dt);
    }

    [Theory]
    [InlineData(2, 30, 2026, 28)] // Feb non-leap clamps to 28
    [InlineData(2, 30, 2024, 29)] // Feb leap clamps to 29
    [InlineData(4, 31, 2026, 30)] // April clamps to 30
    [InlineData(1, 31, 2026, 31)] // January keeps 31
    public void FixDay_clamps_to_month_end(int month, int day, int year, int expected)
    {
        using var fx = new HelpdeskFixture();
        Assert.Equal(expected, fx.Dates.FixDay(month, day, year));
    }
}
