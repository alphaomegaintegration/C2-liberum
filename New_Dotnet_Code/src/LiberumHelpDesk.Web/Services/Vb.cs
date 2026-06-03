using System.Globalization;

namespace LiberumHelpDesk.Web.Services;

/// <summary>
/// Faithful ports of the VBScript coercion semantics the original relied on, so that
/// Cfg()/Usr() integer reads and form parsing behave like the ASP code.
/// </summary>
public static class Vb
{
    /// <summary>
    /// VBScript <c>CInt</c>: banker's (round-half-to-even) rounding. Empty/NULL/non-numeric
    /// collapse to 0, which is the observable outcome in the original under
    /// <c>On Error Resume Next</c> (the failing statement leaves a 0/empty numeric context).
    /// Note: VBScript CInt is 16-bit, but ids here exceed that range, so we use 32-bit to avoid
    /// breaking large datasets (documented deviation).
    /// </summary>
    public static int CInt(object? value)
    {
        switch (value)
        {
            case null:
            case DBNull:
                return 0;
            case bool b:
                return b ? -1 : 0; // VBScript True = -1
            case int i:
                return i;
            case long l:
                return unchecked((int)l);
            case double d:
                return (int)Math.Round(d, MidpointRounding.ToEven);
        }

        var s = value.ToString()?.Trim();
        if (string.IsNullOrEmpty(s)) return 0;
        if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            return (int)Math.Round(parsed, MidpointRounding.ToEven);
        return 0;
    }

    /// <summary>Mirrors the pervasive <c>= 1</c> truthiness checks against int columns.</summary>
    public static bool IsOne(object? value) => CInt(value) == 1;

    /// <summary>Null/DBNull-safe string read, mirroring how ADO recordset fields stringify.</summary>
    public static string Str(object? value) => value is null or DBNull ? "" : value.ToString() ?? "";

    /// <summary>
    /// Print-view formatting (user/rep/kb print): newlines -&gt; &lt;br /&gt;, and bracketed sections bolded.
    /// Mirrors the original Replace(vbNewLine,"&lt;br&gt;") then Replace("[","&lt;b&gt;[") then Replace("]","]&lt;/b&gt;").
    /// </summary>
    public static string FormatBlock(object? value)
    {
        var s = Str(value).Replace("\r\n", "\n").Replace("\n", "<br />");
        s = s.Replace("[", "<b>[").Replace("]", "]</b>");
        return s;
    }
}
