namespace LiberumHelpDesk.Web.Models.ViewModels;

public sealed class LangListRow
{
    public int Id { get; init; }
    public string LangName { get; init; } = "";
    public string Localized { get; init; } = "";
}

public sealed class LangStringRow
{
    public string Variable { get; init; } = "";
    public string EnglishText { get; init; } = "";
    public string CurrentText { get; init; } = "";
}

public sealed class ViewLangStringVm
{
    public int LangId { get; init; }
    public string DefaultLangLabel { get; init; } = "";
    public string CurrentLangLabel { get; init; } = "";
    public IReadOnlyList<LangStringRow> Rows { get; init; } = [];
    public bool SaveSuccess { get; init; }
    public bool AddSuccess { get; init; }
}
