namespace LiberumHelpDesk.Web.Models.ViewModels;

public sealed class RepSearchVm
{
    public IReadOnlyList<Opt> Reps { get; init; } = [];
    public IReadOnlyList<Opt> Categories { get; init; } = [];
    public IReadOnlyList<Opt> Departments { get; init; } = [];
    public IReadOnlyList<Opt> Statuses { get; init; } = [];
    public IReadOnlyList<Opt> Priorities { get; init; } = [];
    public int SMonth { get; init; }
    public int SDay { get; init; }
    public int SYear { get; init; }
    public int EMonth { get; init; }
    public int EDay { get; init; }
    public int EYear { get; init; }
    public int CurrentYear { get; init; }
}

public sealed class RepResultRow
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public string Uid { get; init; } = "";
    public string Uemail { get; init; } = "";
    public string Ruid { get; init; } = "";
    public string StartDate { get; init; } = "";
    public string Sname { get; init; } = "";
}

public sealed class RepResultsVm
{
    public IReadOnlyList<RepResultRow> Rows { get; init; } = [];
    public bool HasResults { get; init; }
    public int Start { get; init; }
    public int Num { get; init; }
    public bool ShowPrev { get; init; }
    public bool ShowNext { get; init; }
    public int StartP { get; init; }
    public int StartN { get; init; }
    public IDictionary<string, string> Echo { get; init; } = new Dictionary<string, string>();
}

public sealed class SelectUserRow
{
    public int Sid { get; init; }
    public string Uid { get; init; } = "";
    public string Firstname { get; init; } = "";
    public string Lastname { get; init; } = "";
    public string Location1 { get; init; } = "";
}

public sealed class SelectUserVm
{
    public string SearchName { get; init; } = "";
    public bool Posted { get; init; }
    public IReadOnlyList<SelectUserRow> Users { get; init; } = [];
}
