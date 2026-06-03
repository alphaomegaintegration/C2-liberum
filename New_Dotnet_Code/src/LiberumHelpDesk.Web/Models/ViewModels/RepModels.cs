namespace LiberumHelpDesk.Web.Models.ViewModels;

public readonly record struct RepOption(int Sid, string Uid, int Count, bool IsSelf);

public sealed class RepMenuVm
{
    public string SiteName { get; init; } = "";
    public int RepAccess { get; init; }
    public int EnableKB { get; init; }
    public int UseInout { get; init; }
    public IReadOnlyList<RepOption> Reps { get; init; } = [];
}

public sealed class RepListRow
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public string Uid { get; init; } = "";
    public string Uemail { get; init; } = "";
    public string StartDate { get; init; } = "";
    public string Pname { get; init; } = "";
    public string Sname { get; init; } = "";
    public int? InoutSid { get; init; }
}

public sealed class RepListVm
{
    public string Ruid { get; init; } = "";
    public int RepId { get; init; }
    public bool DisplayTotal { get; init; }
    public int Total { get; init; }
    public int UseInout { get; init; }
    public IReadOnlyList<RepListRow> Rows { get; init; } = [];
    public int Sort { get; init; }
    public int Order { get; init; }
    public int Start { get; init; }
    public int NumToDisplay { get; init; }
    public int IdOrder { get; init; }
    public int TitleOrder { get; init; }
    public int UidOrder { get; init; }
    public int DateOrder { get; init; }
    public int PriOrder { get; init; }
    public int StatusOrder { get; init; }
    public bool ShowPrev { get; init; }
    public bool ShowNext { get; init; }
    public int StartP { get; init; }
    public int StartN { get; init; }
    public bool HasResults { get; init; }
}

public sealed class RepNoteVm
{
    public string DateDisplay { get; init; } = "";
    public string Uid { get; init; } = "";
    public bool Private { get; init; }
    public string NoteHtml { get; init; } = "";
}

public sealed class RepDetailsVm
{
    public int Id { get; init; }
    public bool JustUpdated { get; init; }
    public string UpdateMessage { get; init; } = "";

    public string Uid { get; init; } = "";
    public string Uemail { get; init; } = "";
    public string Uphone { get; init; } = "";
    public string Ulocation { get; init; } = "";
    public int Department { get; init; }
    public int Category { get; init; }
    public int Status { get; init; }
    public int Priority { get; init; }
    public int Rep { get; init; }
    public int TimeSpent { get; init; }
    public int Kb { get; init; }
    public string EnteredByUid { get; init; } = "";
    public string StartDate { get; init; } = "";
    public string CloseDate { get; init; } = "";
    public string DueDate { get; init; } = "";
    public string DateFormat { get; init; } = "";
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public string Solution { get; init; } = "";

    public bool IsClosed { get; init; }
    public bool ReadonlyText { get; init; }   // strTextDisable
    public bool DisabledList { get; init; }    // strListDisable
    public int EnableKB { get; init; }
    public int EmailType { get; init; }
    public bool ShowSave { get; init; }

    public IReadOnlyList<Opt> Departments { get; init; } = [];
    public IReadOnlyList<Opt> Categories { get; init; } = [];
    public IReadOnlyList<Opt> Statuses { get; init; } = [];
    public IReadOnlyList<Opt> Priorities { get; init; } = [];
    public IReadOnlyList<Opt> Reps { get; init; } = [];
    public IReadOnlyList<RepNoteVm> Notes { get; init; } = [];
}
