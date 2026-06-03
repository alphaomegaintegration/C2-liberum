namespace LiberumHelpDesk.Web.Models.ViewModels;

public sealed class ConfigVm
{
    public bool Saved { get; init; }
    public string SiteName { get; init; } = "";
    public string BaseURL { get; init; } = "";
    public string HDName { get; init; } = "";
    public string HDReply { get; init; } = "";
    public string BaseEmail { get; init; } = "";
    public string SMTPServer { get; init; } = "";
    public string MaxImageSize { get; init; } = "";
    public int EmailType { get; init; }
    public int EnablePager { get; init; }
    public int NotifyUser { get; init; }
    public int EnableKB { get; init; }
    public int KBFreeText { get; init; }
    public int DefaultPriority { get; init; }
    public int DefaultStatus { get; init; }
    public int CloseStatus { get; init; }
    public int AuthType { get; init; }
    public int UseSelectUser { get; init; }
    public int UseInoutBoard { get; init; }
    public int AllowImageUpload { get; init; }
    public int DefaultLanguage { get; init; }

    public IReadOnlyList<Opt> EmailTypes { get; init; } = [];
    public IReadOnlyList<Opt> Priorities { get; init; } = [];
    public IReadOnlyList<Opt> Statuses { get; init; } = [];
    public IReadOnlyList<Opt> StatusesNonClose { get; init; } = [];
    public IReadOnlyList<Opt> AuthTypes { get; init; } = [];
    public IReadOnlyList<LangOpt> Languages { get; init; } = [];
}
