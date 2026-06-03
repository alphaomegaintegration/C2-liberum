namespace LiberumHelpDesk.Web.Models.ViewModels;

public sealed class PrintNote
{
    public string Header { get; init; } = "";   // already-rendered "[date - uid]" (+ PRIVATE)
    public string NoteHtml { get; init; } = "";
}

public sealed class PrintProblemVm
{
    public int Id { get; init; }
    public string Uid { get; init; } = "";
    public string Uemail { get; init; } = "";
    public string Uphone { get; init; } = "";
    public string Ulocation { get; init; } = "";
    public string EnteredByUid { get; init; } = "";
    public string Dname { get; init; } = "";
    public string Cname { get; init; } = "";
    public string Sname { get; init; } = "";
    public string Pname { get; init; } = "";
    public string Rname { get; init; } = "";
    public string Remail { get; init; } = "";   // rep email1 — for the AssignedTo mailto link (user/print.asp line 164)
    public string Title { get; init; } = "";
    public string StartDate { get; init; } = "";
    public string CloseDate { get; init; } = "";
    public bool IsClosed { get; init; }
    public string DescriptionHtml { get; init; } = "";
    public string SolutionHtml { get; init; } = "";
    public IReadOnlyList<PrintNote> Notes { get; init; } = [];
}
