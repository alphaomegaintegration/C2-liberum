using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace LiberumHelpDesk.Web.Services;

/// <summary>
/// A per-request database session holding a single open connection. This mirrors the original
/// ASP pattern where every page does <c>Set cnnDB = CreateCon</c> once at the top and passes it
/// around (CreateCon in public.asp). Registered scoped; disposed at end of request.
/// </summary>
public sealed class Db : IDisposable
{
    public SqliteConnection Connection { get; }

    public Db(IConfiguration config)
        : this(config.GetConnectionString("HelpDesk")
               ?? throw new InvalidOperationException("Missing connection string 'HelpDesk'."))
    {
    }

    public Db(string connectionString)
    {
        Connection = new SqliteConnection(connectionString);
        Connection.Open();
    }

    public void Dispose() => Connection.Dispose();
}
