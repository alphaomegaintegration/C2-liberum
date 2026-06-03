namespace LiberumHelpDesk.Web.Services;

/// <summary>
/// Host/infrastructure config — the .NET equivalent of the original settings.asp Application() vars
/// (collapsed to SQLite). Runtime, admin-editable settings live in tblConfig via <see cref="IConfigService"/>.
/// </summary>
public sealed class AppOptions
{
    public const string SectionName = "Liberum";

    /// <summary>Mirrors Application("Debug"): when true the original showed raw SQL / full errors.</summary>
    public bool Debug { get; set; }

    /// <summary>Directory holding the 7 language .txt files (relative to content root unless rooted).</summary>
    public string LangFileDirectory { get; set; } = "Data/lang";

    /// <summary>Where in/out board profile images are written (was www/image).</summary>
    public string ImageUploadDirectory { get; set; } = "wwwroot/image";

    public int SessionTimeoutMinutes { get; set; } = 40;

    /// <summary>Run the DatabaseSeeder (setup.asp replica) on startup if the DB is empty.</summary>
    public bool SeedOnStartup { get; set; } = true;

    /// <summary>Opt-in sample rep/admin user for immediate login testing (excluded from parity).</summary>
    public bool SeedAdminUser { get; set; }

    /// <summary>Culture lock for date/number formatting parity with the IIS oracle box (en-US default).</summary>
    public string Culture { get; set; } = "en-US";
}
