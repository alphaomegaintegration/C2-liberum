using System.Globalization;
using LiberumHelpDesk.Web.Middleware;
using LiberumHelpDesk.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AppOptions>(builder.Configuration.GetSection(AppOptions.SectionName));
var appOptions = builder.Configuration.GetSection(AppOptions.SectionName).Get<AppOptions>() ?? new AppOptions();

builder.Services.AddControllersWithViews();

builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(appOptions.SessionTimeoutMinutes);
    options.Cookie.HttpOnly = true;
    options.Cookie.Name = "lhd_session";
    options.Cookie.IsEssential = true;
});

builder.Services.AddLiberumServices();

// SeederPaths resolved against the content root (Data/ is copied to output and present in publish/Docker).
builder.Services.AddSingleton(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    string Resolve(string p) => Path.IsPathRooted(p) ? p : Path.Combine(env.ContentRootPath, p);
    return new SeederPaths
    {
        SchemaSqlPath = Resolve("Data/schema.sqlite.sql"),
        SeedSqlPath = Resolve("Data/seed.sqlite.sql"),
        LangDirPath = Resolve(appOptions.LangFileDirectory),
        SeedAdminUser = appOptions.SeedAdminUser,
    };
});

var app = builder.Build();

// Lock culture so date/number formatting matches the IIS oracle box (parity).
var culture = CultureInfo.GetCultureInfo(appOptions.Culture);
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

// Replicate setup.asp: seed schema + base data + language strings on first run.
if (appOptions.SeedOnStartup)
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>().EnsureSeeded();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// Render DisplayError/auth-guard pages (LhdException) faithfully.
app.UseMiddleware<LhdErrorMiddleware>();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program;
