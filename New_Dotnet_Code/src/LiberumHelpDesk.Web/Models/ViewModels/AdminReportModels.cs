namespace LiberumHelpDesk.Web.Models.ViewModels;

public sealed class ReportsFormVm
{
    public int SMonth { get; init; }
    public int SDay { get; init; }
    public int SYear { get; init; }
    public int EMonth { get; init; }
    public int EDay { get; init; }
    public int EYear { get; init; }
    public int CurrentYear { get; init; }
}

public sealed class ReportRow
{
    public string Name { get; init; } = "";
    public int Total { get; init; }
    public int TotalTime { get; init; }
    public double AvgTime { get; init; }
    public double PctProblems { get; init; }
    public double PctTime { get; init; }
}

public sealed class ViewReportsVm
{
    public int Type { get; init; }
    public string GroupHeaderKey { get; init; } = "";
    public IReadOnlyList<ReportRow> Rows { get; init; } = [];
    public int Total { get; init; }
    public int TotalTime { get; init; }
    public double TotalAvg { get; init; }
}
