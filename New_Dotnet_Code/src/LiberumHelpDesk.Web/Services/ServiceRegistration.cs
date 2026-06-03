using Microsoft.Extensions.DependencyInjection;

namespace LiberumHelpDesk.Web.Services;

public static class ServiceRegistration
{
    /// <summary>
    /// Registers the ported public.asp services. All are scoped to a request (mirroring one cnnDB per page).
    /// <see cref="SeederPaths"/> is registered by the host since it depends on the environment.
    /// </summary>
    public static IServiceCollection AddLiberumServices(this IServiceCollection services)
    {
        services.AddScoped<Db>();
        services.AddScoped<IKeyService, KeyService>();
        services.AddScoped<IConfigService, ConfigService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();

        // Shared chrome / i18n / dates / session (Phase 2).
        services.AddSingleton<ILanguageCache, LanguageCache>();
        services.AddScoped<ISessionContext, HttpSessionContext>();
        services.AddScoped<ILanguageService, LanguageService>();
        services.AddScoped<IDateService, DateService>();
        services.AddScoped<IErrorService, ErrorService>();
        services.AddScoped<IChromeService, ChromeService>();

        // Email (Phase 3): SendMail -> MailKit, eMessage token substitution.
        services.AddScoped<IEmailSender, MailKitEmailSender>();
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }
}
