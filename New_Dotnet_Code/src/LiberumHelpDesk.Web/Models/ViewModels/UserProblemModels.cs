namespace LiberumHelpDesk.Web.Models.ViewModels;

public readonly record struct Opt(int Id, string Name);

public sealed class ProblemNewVm
{
    public string Uid { get; init; } = "";
    public string Uemail { get; init; } = "";
    public string Ulocation { get; init; } = "";
    public string Uphone { get; init; } = "";
    public int UserDepartment { get; init; }
    public int DefaultPriority { get; init; }
    public string DueDate { get; init; } = "";
    public string DateFormat { get; init; } = "";
    public IReadOnlyList<Opt> Departments { get; init; } = [];
    public IReadOnlyList<Opt> Categories { get; init; } = [];
    public IReadOnlyList<Opt> Priorities { get; init; } = [];
}

public sealed class ProblemSubmittedVm
{
    public int Id { get; init; }
    public string Uid { get; init; } = "";
    public string Uemail { get; init; } = "";
    public string Uphone { get; init; } = "";
    public string Ulocation { get; init; } = "";
    public string StartDate { get; init; } = "";
    public string DueDate { get; init; } = "";
    public string Dname { get; init; } = "";
    public string Cname { get; init; } = "";
    public string Pname { get; init; } = "";
    public string RepEmail { get; init; } = "";
    public string RepFname { get; init; } = "";
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
}

public sealed class ProblemNoteVm
{
    public string DateDisplay { get; init; } = "";
    public string Uid { get; init; } = "";
    public string NoteHtml { get; init; } = "";
}

public sealed class ProblemDetailsVm
{
    public int Id { get; init; }
    public string Uid { get; init; } = "";
    public string Uemail { get; init; } = "";
    public string Uphone { get; init; } = "";
    public string Ulocation { get; init; } = "";
    public string StartDate { get; init; } = "";
    public string DueDate { get; init; } = "";
    public string CloseDate { get; init; } = "";
    public bool IsClosed { get; init; }
    public string Dname { get; init; } = "";
    public string Cname { get; init; } = "";
    public string Pname { get; init; } = "";
    public string RepEmail { get; init; } = "";
    public string RepFname { get; init; } = "";
    public string Sname { get; init; } = "";
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public string Solution { get; init; } = "";
    public IReadOnlyList<ProblemNoteVm> Notes { get; init; } = [];
}

public sealed class ProblemListRow
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public string StartDate { get; init; } = "";
    public string Fname { get; init; } = "";
    public string Remail { get; init; } = "";
    public string Sname { get; init; } = "";
}

public sealed class ProblemListVm
{
    public string Uid { get; init; } = "";
    public IReadOnlyList<ProblemListRow> Rows { get; init; } = [];
    public int Sort { get; init; }
    public int Order { get; init; }
    public int Start { get; init; }
    public int NumToDisplay { get; init; }
    public int IdOrder { get; init; }
    public int TitleOrder { get; init; }
    public int RepOrder { get; init; }
    public int DateOrder { get; init; }
    public int StatusOrder { get; init; }
    public bool ShowPager { get; init; }
    public bool ShowPrev { get; init; }
    public bool ShowNext { get; init; }
    public int StartP { get; init; }
    public int StartN { get; init; }
}
