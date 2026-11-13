using Microsoft.EntityFrameworkCore;
using Npgsql;
using SaveBite.Backend.Data;

namespace SaveBite.Backend.Uploads;

// Session advisory locks coordinate uploads and cleanup across API/job instances.
// A dedicated, unpooled connection releases the lock even on exceptional exits.
// No database transaction is held during the Cloudinary request.
public static class UploadAssetLock
{
    public static async Task<NpgsqlConnection?> TryAcquireAsync(AppDbContext db, Guid assetId, CancellationToken ct)
    {
        var settings = new NpgsqlConnectionStringBuilder(db.Database.GetConnectionString()) { Pooling = false };
        var connection = new NpgsqlConnection(settings.ConnectionString);
        try
        {
            await connection.OpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT pg_try_advisory_lock(hashtextextended(@asset, 0))";
            command.Parameters.AddWithValue("asset", $"upload-asset:{assetId:D}");
            if (await command.ExecuteScalarAsync(ct) is true) return connection;
            await connection.DisposeAsync();
            return null;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
