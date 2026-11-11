using System;
using System.Threading.Tasks;
using Npgsql;

namespace SaveBite.IntegrationTests.Infrastructure;

public static class DatabaseFixture
{
    public const string TestConnectionString = 
        "Host=localhost;Port=5432;Database=savebite_test;Username=savebite;Password=SaveBiTePasswOrd@@14;Include Error Detail=true;";

    public static async Task ResetDatabaseAsync()
    {
        NpgsqlConnection.ClearAllPools();

        await using var connection = new NpgsqlConnection(TestConnectionString);
        await connection.OpenAsync();

        // 1. Terminate any other hanging connections to avoid locking conflicts
        try
        {
            await using var terminateCmd = connection.CreateCommand();
            terminateCmd.CommandText = @"
SELECT pg_terminate_backend(pid) 
FROM pg_stat_activity 
WHERE datname = 'savebite_test' 
  AND pid <> pg_backend_pid();";
            await terminateCmd.ExecuteNonQueryAsync();
        }
        catch
        {
            // Ignore if lack of permission to terminate
        }

        // 2. Fast truncate all tables except migration history
        const string truncateSql = @"
DO $$ 
DECLARE 
    r RECORD;
BEGIN 
    FOR r IN (
        SELECT tablename 
        FROM pg_tables 
        WHERE schemaname = 'public' 
          AND tablename <> '__EFMigrationsHistory'
    ) LOOP 
        EXECUTE 'TRUNCATE TABLE public.' || quote_ident(r.tablename) || ' CASCADE;'; 
    END LOOP; 
END $$;";

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = truncateSql;
        await cmd.ExecuteNonQueryAsync();
    }
}
