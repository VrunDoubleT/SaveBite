using Npgsql;
using Respawn;

namespace SaveBite.IntegrationTests.Infrastructure;

public static class DatabaseFixture
{
    public const string TestConnectionString = 
        "Host=localhost;Port=5432;Database=savebite_test;Username=savebite;Password=SaveBiTePasswOrd@@14;Include Error Detail=true;";

    private static Respawner? _respawner;

    public static async Task ResetDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(TestConnectionString);
        await connection.OpenAsync();

        _respawner ??= await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = new[] { "public" },
            TablesToIgnore = new Respawn.Graph.Table[]
            {
                new("__EFMigrationsHistory")
            }
        });

        await _respawner.ResetAsync(connection);
    }
}
