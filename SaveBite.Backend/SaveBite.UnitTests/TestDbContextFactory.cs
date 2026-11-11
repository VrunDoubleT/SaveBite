using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SaveBite.Backend.Data;

namespace SaveBite.Tests;

public class SqliteCompatibilityInterceptor : DbCommandInterceptor
{
    private static string CleanSql(string sql)
    {
        if (string.IsNullOrEmpty(sql)) return sql;
        if (Regex.IsMatch(sql, @"(?i)^\s*PRAGMA\s+foreign_keys\s*=\s*(1|ON)", RegexOptions.IgnoreCase))
        {
            return "PRAGMA foreign_keys = OFF;";
        }
        var cleaned = Regex.Replace(sql, @"\bFOR\s+UPDATE\b", "", RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"CAST\((.*?)\s+AS\s+jsonb\)", "$1", RegexOptions.IgnoreCase);
        return cleaned;
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        command.CommandText = CleanSql(command.CommandText);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        command.CommandText = CleanSql(command.CommandText);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        command.CommandText = CleanSql(command.CommandText);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        command.CommandText = CleanSql(command.CommandText);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
    {
        command.CommandText = CleanSql(command.CommandText);
        return base.ScalarExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
    {
        command.CommandText = CleanSql(command.CommandText);
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }
}

public static class TestDbContextFactory
{
    public static AppDbContext Create()
    {
        var connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=False");
        connection.Open();
        using (var pragmaCmd = connection.CreateCommand())
        {
            pragmaCmd.CommandText = "PRAGMA foreign_keys = OFF;";
            pragmaCmd.ExecuteNonQuery();
        }

        connection.CreateFunction("pg_advisory_xact_lock", (long id) => 1);
        connection.CreateFunction("pg_advisory_xact_lock", (int id) => 1);
        connection.CreateFunction("gen_random_uuid", () => Guid.NewGuid().ToString());
        connection.CreateFunction("now", () => DateTime.UtcNow.ToString("o"));

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(new SqliteCompatibilityInterceptor())
            .Options;

        var context = new AppDbContext(options);

        try
        {
            var script = context.Database.GenerateCreateScript();
            script = Regex.Replace(script, @"(?i)\(created_at\s+AT\s+TIME\s+ZONE\s+'[^']+'\)::date", "date(created_at)");
            script = Regex.Replace(script, @"(?i)now\(\)\s+AT\s+TIME\s+ZONE\s+'[^']+'", "CURRENT_TIMESTAMP");
            script = Regex.Replace(script, @"(?i)AT\s+TIME\s+ZONE\s+'[^']+'", "");
            script = Regex.Replace(script, @"(?i)::date", "");
            script = Regex.Replace(script, @"(?i)::[a-z0-9_]+", "");
            script = Regex.Replace(script, @"(?i)\bjsonb\b", "TEXT");

            var statements = script.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            using var cmd = connection.CreateCommand();
            foreach (var stmt in statements)
            {
                if (string.IsNullOrWhiteSpace(stmt)) continue;
                try
                {
                    cmd.CommandText = stmt;
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TestDb] Failed stmt: {ex.Message} in {stmt.Substring(0, Math.Min(80, stmt.Length))}");
                }
            }

            cmd.CommandText = "PRAGMA foreign_keys = OFF;";
            cmd.ExecuteNonQuery();
        }
        catch
        {
        }

        return context;
    }
}
