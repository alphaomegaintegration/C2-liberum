#:package Microsoft.Data.Sqlite@10.0.8
using Microsoft.Data.Sqlite;

// Usage: dotnet run dbq.cs -- "<connstr>" <q|e|ef> "<sql-or-path>"
//   q = query (dump rows), e = execute inline non-query, ef = execute SQL read from a file path
var cs = args[0];
var mode = args[1];
var sql = mode == "ef" ? File.ReadAllText(args[2]) : args[2];
if (mode == "ef") mode = "e";
using var con = new SqliteConnection(cs);
con.Open();
using var cmd = con.CreateCommand();
cmd.CommandText = sql;
if (mode == "e")
{
    var n = cmd.ExecuteNonQuery();
    Console.WriteLine($"rows affected: {n}");
}
else
{
    using var rdr = cmd.ExecuteReader();
    do
    {
        for (int i = 0; i < rdr.FieldCount; i++) Console.Write((i > 0 ? " | " : "") + rdr.GetName(i));
        Console.WriteLine();
        while (rdr.Read())
        {
            for (int i = 0; i < rdr.FieldCount; i++) Console.Write((i > 0 ? " | " : "") + rdr.GetValue(i));
            Console.WriteLine();
        }
        Console.WriteLine("--");
    } while (rdr.NextResult());
}
