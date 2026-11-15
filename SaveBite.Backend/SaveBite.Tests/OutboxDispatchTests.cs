using System.Reflection;
using System.Text.Json;
using SaveBite.Backend.BackgroundJobs.Jobs;
using SaveBite.Backend.Messaging.Configuration;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;

namespace SaveBite.Tests;

public class OutboxDispatchTests
{
    [Fact]
    public void TrustPolicyFailuresHaveAutomaticReplayDestination()
        => Assert.Contains((RabbitMqQueues.TrustLevelReconcileDeadLetterQueueName, RabbitMqQueues.TrustLevelReconcileQueueName), RabbitFailureRecoveryJob.Queues);

    [Fact]
    public async Task MissingLocationServiceCannotAcknowledgeAddressEvent()
    {
        var events = DispatchProxy.Create<IOrderEventRepository, Events>();
        var proxy = (Events)(object)events;
        proxy.Message = new OrderEventOutbox { RoutingKey = RabbitMqRoutingKeys.UserDefaultAddressChanged,
            PayloadJson = JsonSerializer.Serialize(new UserDefaultAddressChangedEvent(Guid.NewGuid(), Guid.NewGuid(), 1, 2, 1, true)) };
        var job = new OrderEventDispatchJob(events, null!, TimeProvider.System, new RabbitMqTopologyState());
        await Assert.ThrowsAsync<InvalidOperationException>(() => job.ExecuteAsync(proxy.Message.Id, CancellationToken.None));
        Assert.Null(proxy.Message.PublishedAt);
    }

    [Fact]
    public void DifferentAddressesWithSameVersionProduceDistinctOutboxKeys()
    {
        using var db = new SaveBite.Backend.Data.AppDbContext(new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<SaveBite.Backend.Data.AppDbContext>()
            .UseAuditPostgres().Options);
        var user = Guid.NewGuid();
        UserDefaultAddressChangedOutbox.Stage(db, user, Guid.NewGuid(), 1, 2, 1, true);
        UserDefaultAddressChangedOutbox.Stage(db, user, Guid.NewGuid(), 3, 4, 1, true);
        Assert.Equal(2, db.ChangeTracker.Entries<OrderEventOutbox>().Select(e => e.Entity.EventKey).Distinct().Count());
    }

    public class Events : DispatchProxy
    {
        public OrderEventOutbox Message = null!;
        protected override object? Invoke(MethodInfo? method, object?[]? args) => method!.Name == "DispatchAsync"
            ? ((Func<OrderEventOutbox, Task>)args![1]!)(Message) : throw new NotSupportedException();
    }
}

internal static class AuditTestProvider
{
    internal static Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<SaveBite.Backend.Data.AppDbContext> UseAuditPostgres(
        this Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<SaveBite.Backend.Data.AppDbContext> builder)
        => Microsoft.EntityFrameworkCore.NpgsqlDbContextOptionsBuilderExtensions.UseNpgsql(builder, "Host=localhost;Database=unused");
}
