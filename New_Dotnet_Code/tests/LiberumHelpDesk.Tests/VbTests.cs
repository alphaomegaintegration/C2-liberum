using LiberumHelpDesk.Web.Services;

namespace LiberumHelpDesk.Tests;

public class VbTests
{
    [Theory]
    [InlineData(0.5, 0)]   // banker's rounding: half to even
    [InlineData(1.5, 2)]
    [InlineData(2.5, 2)]
    [InlineData(3.5, 4)]
    [InlineData(2.4, 2)]
    [InlineData(2.6, 3)]
    public void CInt_uses_bankers_rounding(double input, int expected)
        => Assert.Equal(expected, Vb.CInt(input));

    [Fact]
    public void CInt_null_empty_and_garbage_collapse_to_zero()
    {
        Assert.Equal(0, Vb.CInt(null));
        Assert.Equal(0, Vb.CInt(DBNull.Value));
        Assert.Equal(0, Vb.CInt(""));
        Assert.Equal(0, Vb.CInt("   "));
        Assert.Equal(0, Vb.CInt("abc"));
    }

    [Fact]
    public void CInt_parses_numeric_strings_and_ints()
    {
        Assert.Equal(100, Vb.CInt("100"));
        Assert.Equal(100, Vb.CInt(100));
        Assert.Equal(100, Vb.CInt(100L));
        Assert.Equal(-1, Vb.CInt(true));  // VBScript True = -1
    }

    [Fact]
    public void IsOne_matches_equals_one_checks()
    {
        Assert.True(Vb.IsOne(1));
        Assert.True(Vb.IsOne("1"));
        Assert.False(Vb.IsOne(0));
        Assert.False(Vb.IsOne(null));
    }
}
