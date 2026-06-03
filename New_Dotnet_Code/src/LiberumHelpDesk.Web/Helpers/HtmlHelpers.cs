using LiberumHelpDesk.Web.Services;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace LiberumHelpDesk.Web.Helpers;

public static class HtmlHelpers
{
    /// <summary>
    /// Emits a language string RAW (unencoded), mirroring the original &lt;%=lang(cnnDB,"X")%&gt; which
    /// Response.Write'd values without HTML-encoding. Parity requires unencoded output.
    /// </summary>
    public static IHtmlContent Lang(this IHtmlHelper html, string key)
    {
        var svc = html.ViewContext.HttpContext.RequestServices.GetRequiredService<ILanguageService>();
        return new HtmlString(svc.Lang(key));
    }
}
