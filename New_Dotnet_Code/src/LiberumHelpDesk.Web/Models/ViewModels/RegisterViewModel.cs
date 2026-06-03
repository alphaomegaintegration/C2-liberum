namespace LiberumHelpDesk.Web.Models.ViewModels;

public readonly record struct LangOpt(int Id, string Name, string Localized);

public sealed class RegisterViewModel
{
    public bool Edit { get; init; }
    public bool Success { get; init; }
    public int AuthType { get; init; }
    public bool ShowPager { get; init; }
    public bool UseInout { get; init; }
    public string UidDisplay { get; init; } = "";

    public string FrmFirstname { get; init; } = "";
    public string FrmLastname { get; init; } = "";
    public string FrmEmail { get; init; } = "";
    public string FrmPager { get; init; } = "";
    public string FrmPhone { get; init; } = "";
    public string FrmPhoneHome { get; init; } = "";
    public string FrmPhoneMobile { get; init; } = "";
    public string FrmLocation { get; init; } = "";
    public int FrmDepartment { get; init; }
    public int FrmUsrLanguage { get; init; }
    public string FrmDateFormat { get; init; } = "";

    public IReadOnlyList<Opt> Departments { get; init; } = [];
    public IReadOnlyList<LangOpt> Languages { get; init; } = [];
    public string[] DateFormats { get; } = ["mm/dd/yyyy", "dd/mm/yyyy", "yyyy-mm-dd"];
}
