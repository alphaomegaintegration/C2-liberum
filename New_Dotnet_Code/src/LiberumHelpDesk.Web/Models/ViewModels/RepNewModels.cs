namespace LiberumHelpDesk.Web.Models.ViewModels;

public sealed class RepNewVm
{
    public bool JustSubmitted { get; init; }
    public string SubmitResults { get; init; } = "";
    public string BaseEmail { get; init; } = "";
    public bool UseSelectUser { get; init; }
    public int DefaultStatus { get; init; }
    public int DefaultPriority { get; init; }
    public int SelfSid { get; init; }
    public string DueDate { get; init; } = "";
    public string DateFormat { get; init; } = "";
    public int EnableKB { get; init; }
    public int EmailType { get; init; }
    public IReadOnlyList<Opt> Departments { get; init; } = [];
    public IReadOnlyList<Opt> Categories { get; init; } = [];
    public IReadOnlyList<Opt> Statuses { get; init; } = [];
    public IReadOnlyList<Opt> Priorities { get; init; } = [];
    public IReadOnlyList<Opt> Reps { get; init; } = [];
}
