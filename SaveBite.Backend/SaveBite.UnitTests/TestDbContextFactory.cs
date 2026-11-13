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
        var cleaned = Regex.Replace(sql, @"\bFOR\s+(UPDATE|SHARE)\b", "", RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"""fee_rate""\s+BETWEEN\s+0\s+AND\s+1", "CAST(\"fee_rate\" AS REAL) BETWEEN 0 AND 1", RegexOptions.IgnoreCase);
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

#pragma warning disable EF1001
file sealed class SqliteDecimalAggregateMethodCallTranslatorProvider
    : Microsoft.EntityFrameworkCore.Sqlite.Query.Internal.SqliteAggregateMethodCallTranslatorProvider
{
    private readonly Microsoft.EntityFrameworkCore.Query.RelationalAggregateMethodCallTranslatorProviderDependencies _deps;

    public SqliteDecimalAggregateMethodCallTranslatorProvider(
        Microsoft.EntityFrameworkCore.Query.RelationalAggregateMethodCallTranslatorProviderDependencies deps)
        : base(deps)
    {
        _deps = deps;
    }
    public override Microsoft.EntityFrameworkCore.Query.SqlExpressions.SqlExpression? Translate(
        Microsoft.EntityFrameworkCore.Metadata.IModel model,
        System.Reflection.MethodInfo method,
        Microsoft.EntityFrameworkCore.Query.EnumerableExpression source,
        IReadOnlyList<Microsoft.EntityFrameworkCore.Query.SqlExpressions.SqlExpression> arguments,
        Microsoft.EntityFrameworkCore.Diagnostics.IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        var m = method.IsGenericMethod ? method.GetGenericMethodDefinition() : method;
        var isMin = m == Microsoft.EntityFrameworkCore.Query.QueryableMethods.MinWithSelector
            || m == Microsoft.EntityFrameworkCore.Query.QueryableMethods.MinWithoutSelector;
        var isMax = m == Microsoft.EntityFrameworkCore.Query.QueryableMethods.MaxWithSelector
            || m == Microsoft.EntityFrameworkCore.Query.QueryableMethods.MaxWithoutSelector;

        if ((isMin || isMax)
            && source.Selector is Microsoft.EntityFrameworkCore.Query.SqlExpressions.SqlExpression sqlExpr
            && (sqlExpr.Type == typeof(decimal) || sqlExpr.Type == typeof(decimal?)))
        {
                var doubleMapping = _deps.RelationalTypeMappingSource.FindMapping(typeof(double));
                var castExpr = _deps.SqlExpressionFactory.Convert(sqlExpr, typeof(double), doubleMapping);
                var aggExpr = _deps.SqlExpressionFactory.Function(
                    isMin ? "MIN" : "MAX",
                    [castExpr],
                    nullable: true,
                    argumentsPropagateNullability: [false],
                    typeof(double),
                    doubleMapping);
                return _deps.SqlExpressionFactory.Convert(aggExpr, sqlExpr.Type, sqlExpr.TypeMapping);
        }

        return base.Translate(model, method, source, arguments, logger);
    }
}
#pragma warning restore EF1001

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
        connection.CreateFunction("pg_advisory_lock", (long id) => 1);
        connection.CreateFunction("pg_advisory_lock", (int id) => 1);
        connection.CreateFunction("pg_advisory_unlock", (long id) => 1);
        connection.CreateFunction("pg_advisory_unlock", (int id) => 1);
        connection.CreateFunction("hashtextextended", (string? s, long seed) => HashTextExtended(s, seed));
        connection.CreateFunction("hashtextextended", (string? s, int seed) => HashTextExtended(s, seed));
        connection.CreateFunction("gen_random_uuid", () => Guid.NewGuid().ToString().ToUpperInvariant());
        connection.CreateFunction("now", () => DateTime.UtcNow.ToString("o"));

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(new SqliteCompatibilityInterceptor())
            .ReplaceService<Microsoft.EntityFrameworkCore.Query.IAggregateMethodCallTranslatorProvider, SqliteDecimalAggregateMethodCallTranslatorProvider>()
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
            script = Regex.Replace(script, @"(?i)""fee_rate""\s+BETWEEN\s+0\s+AND\s+1", "CAST(\"fee_rate\" AS REAL) BETWEEN 0 AND 1");

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
                    System.Diagnostics.Debug.WriteLine($"[TestDb] Failed stmt: {ex.Message} in {stmt.Substring(0, Math.Min(80, stmt.Length))}");
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

    private static long HashTextExtended(string? input, long seed)
    {
        unchecked
        {
            ulong hash = 14695981039346656037UL ^ (ulong)seed;
            if (input is not null)
            {
                foreach (var c in input)
                {
                    hash ^= c;
                    hash *= 1099511628211UL;
                }
            }
            return (long)hash;
        }
    }

    internal static SaveBite.Backend.Models.Entities.Refund SeedRefundOrder(AppDbContext db, SaveBite.Backend.Models.Entities.Refund refund)
    {
        var order = refund.Order;
        if (order == null || order.Id == Guid.Empty || db.Orders.Any(o => o.Id == order.Id))
            return refund;

        var now = DateTime.UtcNow;
        var customerId = order.CustomerId != Guid.Empty ? order.CustomerId : Guid.NewGuid();

        if (!db.Users.Any(u => u.Id == customerId))
        {
            db.Users.Add(new SaveBite.Backend.Models.Entities.User
            {
                Id = customerId,
                Email = $"cust-{customerId:N}@test.local",
                PasswordHash = "hash",
                FullName = order.Customer?.FullName ?? "Test Customer",
                Role = SaveBite.Backend.Models.Enums.UserRole.User,
                Status = SaveBite.Backend.Models.Enums.UserStatus.Active,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        if (!db.Shops.Any(s => s.Id == order.ShopId))
        {
            var ownerId = order.Shop?.OwnerUserId is Guid oid && oid != Guid.Empty ? oid : Guid.NewGuid();
            if (db.Shops.Any(s => s.OwnerUserId == ownerId))
                ownerId = Guid.NewGuid();

            if (!db.Users.Any(u => u.Id == ownerId))
            {
                db.Users.Add(new SaveBite.Backend.Models.Entities.User
                {
                    Id = ownerId,
                    Email = $"owner-{ownerId:N}@test.local",
                    PasswordHash = "hash",
                    FullName = "Test Owner",
                    Role = SaveBite.Backend.Models.Enums.UserRole.StoreOwner,
                    Status = SaveBite.Backend.Models.Enums.UserStatus.Active,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            var appId = Guid.NewGuid();
            db.ShopApplications.Add(new SaveBite.Backend.Models.Entities.ShopApplication
            {
                Id = appId,
                ApplicantUserId = ownerId,
                Name = order.Shop?.Name ?? "Test Shop",
                AddressLine = "123 Test St",
                Status = SaveBite.Backend.Models.Enums.ShopApplicationStatus.Approved,
                PayosClientId = "cid",
                PayosApiKey = "key",
                PayosChecksumKey = "sum",
                CreatedAt = now,
                UpdatedAt = now
            });

            db.Shops.Add(new SaveBite.Backend.Models.Entities.Shop
            {
                Id = order.ShopId,
                OwnerUserId = ownerId,
                ApplicationId = appId,
                Name = string.IsNullOrWhiteSpace(order.Shop?.Name) ? "Test Shop" : order.Shop.Name,
                AddressLine = "123 Test St",
                Status = SaveBite.Backend.Models.Enums.ShopStatus.Active,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        db.Orders.Add(new SaveBite.Backend.Models.Entities.Order
        {
            Id = order.Id,
            OrderCode = string.IsNullOrWhiteSpace(order.OrderCode) ? $"ORD-{order.Id:N}"[..16] : order.OrderCode,
            CustomerId = customerId,
            ShopId = order.ShopId,
            PaymentMethod = order.PaymentMethod,
            Status = order.Status,
            TotalAmount = order.TotalAmount > 0 ? order.TotalAmount : 100000m,
            DepositAmount = order.DepositAmount,
            RemainingAmount = order.RemainingAmount,
            CreatedAt = now,
            UpdatedAt = now
        });

        db.SaveChanges();
        return refund;
    }

    public static Microsoft.Extensions.DependencyInjection.IServiceScopeFactory CreateScopeFactory()
    {
        var provider = new Moq.Mock<IServiceProvider>();
        provider.Setup(p => p.GetService(typeof(SaveBite.Backend.Services.Interfaces.IShopStatusEventService)))
            .Returns(new Moq.Mock<SaveBite.Backend.Services.Interfaces.IShopStatusEventService>().Object);
        provider.Setup(p => p.GetService(typeof(SaveBite.Backend.Services.Interfaces.IShopSuspensionCleanupService)))
            .Returns(new Moq.Mock<SaveBite.Backend.Services.Interfaces.IShopSuspensionCleanupService>().Object);

        var scope = new Moq.Mock<Microsoft.Extensions.DependencyInjection.IServiceScope>();
        scope.Setup(s => s.ServiceProvider).Returns(provider.Object);

        var factory = new Moq.Mock<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>();
        factory.Setup(f => f.CreateScope()).Returns(scope.Object);
        return factory.Object;
    }
}
