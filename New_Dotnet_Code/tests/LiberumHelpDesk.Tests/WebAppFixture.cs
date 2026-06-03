using LiberumHelpDesk.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LiberumHelpDesk.Tests;

/// <summary>Captures emails instead of sending, so flows can assert notifications.</summary>
public sealed class CapturingEmailSender : IEmailSender
{
    public readonly List<(string To, string From, string FromName, string Subject, string Body)> Sent = new();
    public void Send(string to, string from, string fromName, string subject, string body)
        => Sent.Add((to, from, fromName, subject, body));
}

/// <summary>
/// Boots the real app against a throwaway file-backed SQLite DB (seeded on startup with the sample
/// admin user), with email capture. Used for end-to-end flow tests through the MVC pipeline.
/// </summary>
public sealed class HelpdeskWebApp : WebApplicationFactory<Program>
{
    public string DbPath { get; } =
        Path.Combine(Path.GetTempPath(), "lhd_" + Guid.NewGuid().ToString("N") + ".db");

    public CapturingEmailSender Emails { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("ConnectionStrings:HelpDesk", $"Data Source={DbPath};Cache=Shared");
        builder.UseSetting("Liberum:SeedOnStartup", "true");
        builder.UseSetting("Liberum:SeedAdminUser", "true");
        builder.UseSetting("Liberum:Culture", "en-US");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);
        });
    }

    public SqliteConnection OpenDb()
    {
        var c = new SqliteConnection($"Data Source={DbPath};Cache=Shared");
        c.Open();
        return c;
    }

    public HttpClient NewClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true,
    });

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        try
        {
            SqliteConnection.ClearAllPools();
            foreach (var f in new[] { DbPath, DbPath + "-wal", DbPath + "-shm" })
                if (File.Exists(f)) File.Delete(f);
        }
        catch { /* best effort */ }
    }
}
