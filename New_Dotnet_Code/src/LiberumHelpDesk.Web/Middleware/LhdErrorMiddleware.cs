using LiberumHelpDesk.Web.Services;

namespace LiberumHelpDesk.Web.Middleware;

/// <summary>
/// Catches <see cref="LhdException"/> (raised by DisplayError ports / auth guards) and writes the
/// faithful error-box HTML, mirroring Response.Clear + Response.Write + Response.End.
/// </summary>
public sealed class LhdErrorMiddleware
{
    private readonly RequestDelegate _next;
    public LhdErrorMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (LhdException ex)
        {
            if (!context.Response.HasStarted)
                context.Response.Clear();
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync(ex.Html);
        }
    }
}
