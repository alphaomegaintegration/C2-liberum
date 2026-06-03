namespace LiberumHelpDesk.Web.Models.ViewModels;

public sealed class KbRow
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public string StartDate { get; init; } = "";
    public string CloseDate { get; init; } = "";
}

public sealed class KbSearchVm
{
    public bool Searched { get; init; }
    public IReadOnlyList<KbRow> Results { get; init; } = [];
}

public sealed class KbNote
{
    public string DateRaw { get; init; } = "";
    public string NoteHtml { get; init; } = "";
}

public sealed class KbDetailsVm
{
    public int Id { get; init; }
    public string StartDate { get; init; } = "";
    public string CloseDate { get; init; } = "";
    public string Cname { get; init; } = "";
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public string Solution { get; init; } = "";
    public IReadOnlyList<KbNote> Notes { get; init; } = [];
}
