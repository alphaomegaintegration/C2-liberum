using System.Globalization;

namespace LiberumHelpDesk.Web.Services;

/// <summary>
/// Ports of the date helpers in public.asp: SQLDate, DisplayDate, ConvertFormattedDate, FixDay.
/// DisplayDate/ConvertFormattedDate honour the viewing user's tblUsers.dateformat (resolved from
/// the current session sid, exactly like the original page-global cnnDB/sid).
/// </summary>
public interface IDateService
{
    string SqlDate(object? dtDate, bool addDelim);
    string DisplayDate(object? dtDate, bool withTime);
    DateTime? ConvertFormattedDate(string? input);
    int FixDay(int month, int day, int year);
}

public sealed class DateService : IDateService
{
    private static string NormalizeUnicodeSpaces(string value)
    {
        // ICU/globalization can emit non-breaking space variants in time strings.
        // Convert them to plain ASCII spaces for deterministic parity output.
        return value
            .Replace('\u00A0', ' ')
            .Replace('\u202F', ' ')
            .Replace('\u2007', ' ');
    }

    private readonly ISessionContext _session;
    private readonly IUserService _users;

    public DateService(ISessionContext session, IUserService users)
    {
        _session = session;
        _users = users;
    }

    private static bool TryParse(object? value, out DateTime dt)
    {
        dt = default;
        if (value is null or DBNull) return false;
        if (value is DateTime d) { dt = d; return true; }
        var s = value.ToString();
        if (string.IsNullOrWhiteSpace(s)) return false;
        // DB stores zero-padded ISO; parse invariantly (CDate-equivalent for our inputs).
        return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
    }

    // SQLDate: "Year-Month-Day Hour:Minute:Second", NON zero-padded (faithful), optionally delimited.
    // The original delimiter is "'" for SQL Server/DSN and "#" for Access; our parity oracle is SQL
    // Server, so we use "'". SQLDate is not used to build SQL in this port (dates are parameterised);
    // it is retained for parity/test fidelity.
    public string SqlDate(object? dtDate, bool addDelim)
    {
        if (!TryParse(dtDate, out var dt)) return "";
        var s = $"{dt.Year}-{dt.Month}-{dt.Day} {dt.Hour}:{dt.Minute}:{dt.Second}";
        return addDelim ? "'" + s + "'" : s;
    }

    // DisplayDate: applies the user's dateformat by replacing yyyy/yy/mm/dd (NON padded), then appends
    // the locale long time when withTime is set.
    public string DisplayDate(object? dtDate, bool withTime)
    {
        if (!TryParse(dtDate, out var dt)) return "";

        var fmt = _users.UsrString(_session.Sid, "dateformat");
        if (fmt.Length == 0) fmt = "mm/dd/yyyy";
        fmt = fmt.ToLowerInvariant();

        var yearStr = dt.Year.ToString(CultureInfo.InvariantCulture);
        var right2 = yearStr.Length >= 2 ? yearStr[^2..] : yearStr;

        // Replacement order matches the original exactly: yyyy, yy, mm, dd.
        fmt = fmt.Replace("yyyy", yearStr);
        fmt = fmt.Replace("yy", right2);
        fmt = fmt.Replace("mm", dt.Month.ToString(CultureInfo.InvariantCulture));
        fmt = fmt.Replace("dd", dt.Day.ToString(CultureInfo.InvariantCulture));

        if (withTime)
        {
            var culture = CultureInfo.CurrentCulture; // locked to en-US in Program.cs for parity
            var longTime = dt.ToString(culture.DateTimeFormat.LongTimePattern, culture);
            fmt = fmt + " " + NormalizeUnicodeSpaces(longTime);
        }
        return fmt;
    }

    // ConvertFormattedDate: parse a user-format date back to a DateTime using the user's dateformat.
    public DateTime? ConvertFormattedDate(string? input)
    {
        if (input is null) return null;
        var userFormat = _users.UsrString(_session.Sid, "dateformat");
        if (userFormat.Length == 0) return null;

        // Split char = first non-lowercase-alpha char in the format (mirrors the original loop).
        var i = 0;
        while (i < userFormat.Length && userFormat[i] is >= 'a' and <= 'z') i++;
        if (i >= userFormat.Length) return null;
        var splitChar = userFormat[i];

        var fmtParts = userFormat.Split(splitChar);
        var inParts = input.Split(splitChar);
        if (inParts.Length != 3) return null; // original: Ubound(varInput) = 2

        string? day = null, month = null, year = null;
        for (var p = 0; p < fmtParts.Length && p < inParts.Length; p++)
        {
            switch (fmtParts[p])
            {
                case "dd": day = inParts[p]; break;
                case "mm": month = inParts[p]; break;
                case "yy": year = inParts[p]; break;
                case "yyyy": year = inParts[p]; break;
            }
        }

        year = (year?.Length) switch
        {
            2 => "20" + year,
            4 => year,
            _ => null,
        };

        if (IsNumeric(year) && IsNumeric(month) && IsNumeric(day) &&
            DateTime.TryParse($"{year}-{month}-{day}", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            return dt;
        }
        // Parse failure => null, so callers' IsDate()/`is null` checks reject the submission
        // (the original effectively yielded Null here => IsDate(Null)=False => DisplayError(1,"DueDate")).
        return null;
    }

    // FixDay: clamp a day to the last valid day of the month (used by date dropdowns).
    public int FixDay(int month, int day, int year)
    {
        var result = day;
        if (month is 4 or 6 or 9 or 11 && day > 30) result = 30;
        if (month == 2 && day > 28) result = (year % 4 == 0) ? 29 : 28;
        return result;
    }

    private static bool IsNumeric(string? s) =>
        !string.IsNullOrEmpty(s) && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out _);
}
