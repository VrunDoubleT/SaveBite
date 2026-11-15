using System.Reflection;
using System.Text.Json;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using Xunit;

namespace SaveBite.Tests;

public sealed class PayOsWebhookRoutingTests
{
    [Fact]
    public async Task IndependentChannelsWithIdenticalOrderCodesUseTheirOwnHandlers()
    {
        var checkout = DispatchProxy.Create<ICheckoutPaymentService, Calls>();
        var fees = DispatchProxy.Create<IPlatformFeeStatementService, Calls>();
        var dispatcher = new PayOsWebhookDispatcher(checkout, fees);
        var body = JsonSerializer.SerializeToElement(new { data = new { orderCode = 123L } });
        await dispatcher.DispatchAsync(body, default);
        await dispatcher.DispatchAsync(body, default, platformFees: true);
        Assert.Equal(new[] { "WebhookAsync" }, ((Calls)(object)checkout).Methods);
        Assert.Equal(new[] { "ProcessFeeWebhookAsync" }, ((Calls)(object)fees).Methods);
    }

    public class Calls : DispatchProxy
    {
        public List<string> Methods { get; } = [];
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Methods.Add(targetMethod!.Name);
            return Task.CompletedTask;
        }
    }
}
