namespace LiberumHelpDesk.Web.Models.ViewModels;

public sealed class LogonViewModel
{
    public string SiteName { get; init; } = "";
    public bool Invalid { get; init; }
    public string FrmUrl { get; init; } = "";
    public int EnableKB { get; init; }
    public int EmailType { get; init; }
}
