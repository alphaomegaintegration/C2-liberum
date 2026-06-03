namespace LiberumHelpDesk.Web.Models.ViewModels;

public sealed class LookupRowVm
{
    public int Id { get; init; }
    public IReadOnlyList<string> Cells { get; init; } = [];
    // viewstatus.asp: the CloseStatus row gets an <em>*</em> marker after its id (first) cell.
    public bool Marked { get; init; }
}

public sealed class LookupListVm
{
    public string Heading { get; init; } = "";
    public IReadOnlyList<string> Headers { get; init; } = [];
    public IReadOnlyList<LookupRowVm> Rows { get; init; } = [];
    public int Mtype { get; init; }
    public string AddLabel { get; init; } = "";
    // viewstatus.asp footnote: "<i><em>*</em>ClosedStatusDonotdelete.</i>" (null on the other list pages).
    public string? Footnote { get; init; }
}

public sealed class ModifyVm
{
    public int Mtype { get; init; }
    public int DataId { get; init; }
    public int NumDataFields { get; init; }
    public string Title { get; init; } = "";
    public string Data1Name { get; init; } = "";
    public string Data2Name { get; init; } = "";
    public string Data1 { get; init; } = "";
    public string Data2 { get; init; } = "";
    public bool CategoryRepDropdown { get; init; }
    public IReadOnlyList<Opt> Reps { get; init; } = [];
    public int SelectedRep { get; init; }
    public bool ShowNumberNote { get; init; }
    // modify.asp hidden fields (lines 162-163): values come from QueryString mLanguage/mLangID;
    // empty strings for mtype 2-5 since those params are not passed.
    public string MLanguage { get; init; } = "";
    public string MLangID { get; init; } = "";
}

public sealed class ConfDeleteVm
{
    public int Mtype { get; init; }
    public int Id { get; init; }
    public bool OkToDelete { get; init; }
    public bool CatExists { get; init; }
    public IReadOnlyList<(int Id, string Name)> BlockingCategories { get; init; } = [];
    public IReadOnlyList<int> BlockingProblems { get; init; } = [];
}
