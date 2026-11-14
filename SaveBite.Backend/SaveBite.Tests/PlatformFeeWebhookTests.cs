using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using Xunit;

namespace SaveBite.Tests;

public class PlatformFeeWebhookTests
{
    [Fact]
    public async Task SignedWebhookMarksReceiptAndStatementPaidAndDuplicateDoesNotSettleAgain()
    {
        var (service, repository) = Fixture();
        await Deliver(service, Body(50000));
        Assert.Equal(PaymentStatus.Succeeded, repository.Payment.Status);
        Assert.Equal(PlatformFeeStatementStatus.Paid, repository.Payment.Statement.Status);
        Assert.NotNull(repository.Payment.Statement.PaidAt);
        Assert.Null(repository.Payment.Statement.PaymentPauseReason);
        await Deliver(service, Body(50000));
        Assert.Equal(1, repository.Saves);
    }

    [Theory]
    [InlineData(50000, "wrong-checksum")]
    [InlineData(49999, "checksum")]
    public async Task InvalidSignatureOrUnderpaymentCannotSettleStatement(decimal amount, string key)
    {
        var (service, repository) = Fixture();
        await Assert.ThrowsAsync<AppException>(() => Deliver(service, Body(amount, key)));
        Assert.Equal(PaymentStatus.Pending, repository.Payment.Status);
        Assert.Equal(PlatformFeeStatementStatus.Issued, repository.Payment.Statement.Status);
        Assert.Equal(0, repository.Saves);
    }

    // Exercise signature and settlement logic without the PostgreSQL transaction wrapper.
    private static Task<Guid?> Deliver(PlatformFeeStatementService service, JsonElement body) =>
        (Task<Guid?>)typeof(PlatformFeeStatementService).GetMethod("ProcessFeeWebhookCoreAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(service, [body, CancellationToken.None])!;

    private static JsonElement Body(decimal amount, string key = "checksum")
    {
        var data = JsonSerializer.SerializeToElement(new { amount, code = "00", orderCode = 123456 });
        return JsonSerializer.SerializeToElement(new { code = "00", data,
            signature = PayOsCheckoutGateway.Sign(FormattableString.Invariant($"amount={amount}&code=00&orderCode=123456"), key) });
    }

    private static (PlatformFeeStatementService Service, Repository Repository) Fixture()
    {
        var proxy = DispatchProxy.Create<IPlatformFeeStatementRepository, Repository>();
        var repository = (Repository)(object)proxy;
        var shop = new Shop { Id = Guid.NewGuid(), OwnerUserId = Guid.NewGuid(), Name = "Shop", Status = ShopStatus.Active };
        var statement = new PlatformFeeStatement { Id = Guid.NewGuid(), ShopId = shop.Id, Shop = shop,
            Status = PlatformFeeStatementStatus.Issued, TotalFeeAmount = 50000, PaymentPauseReason = "Previous configuration issue" };
        repository.Payment = new PlatformFeePayment { Id = Guid.NewGuid(), StatementId = statement.Id, Statement = statement,
            ShopId = shop.Id, Shop = shop, Amount = 50000, Status = PaymentStatus.Pending, GatewayTransactionId = "123456" };
        var credentials = DispatchProxy.Create<IPlatformPayOsConfigService, Config>();
        var service = new PlatformFeeStatementService(proxy, TimeProvider.System, NullLogger<PlatformFeeStatementService>.Instance,
            new ConfigurationBuilder().Build(), null!, new Notifications(), null!, null!, credentials,
            null!, null!, null!, null!, null!);
        return (service, repository);
    }
    public class Config : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? method, object?[]? args) =>
            method!.Name == "GetCredentialsAsync" ? Task.FromResult(new PlatformPayOsCredentials("client", "api-key", "checksum")) : throw new NotSupportedException();
    }
    public class Repository : DispatchProxy
    {
        public PlatformFeePayment Payment = null!;
        public int Saves;
        protected override object? Invoke(MethodInfo? method, object?[]? args) => method!.Name switch
        {
            "GetPaymentByGatewayCodeAsync" => Task.FromResult<PlatformFeePayment?>(Payment),
            "GetStatementDetailAsync" => Task.FromResult<PlatformFeeStatement?>(Payment.Statement),
            "HasOtherOverdueStatementsAsync" => Task.FromResult(false),
            "CancelPendingPaymentsForStatementAsync" => Cancel(),
            "SaveChangesAsync" => Save(),
            _ => throw new NotSupportedException(method.Name)
        };
        private Task Cancel() { Payment.Status = PaymentStatus.Cancelled; return Task.CompletedTask; }
        private Task Save() { Saves++; return Task.CompletedTask; }
    }
    private sealed class Notifications : INotificationQueue
    {
        public Task EnqueueAsync(NotificationTarget target, CreateNotificationCommand command, CancellationToken ct = default) => Task.CompletedTask;
    }
}
