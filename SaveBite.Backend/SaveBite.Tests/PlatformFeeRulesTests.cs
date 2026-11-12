using SaveBite.Backend.Configurations;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using Xunit;

namespace SaveBite.Tests;

public class PlatformFeeRulesTests
{
    private static readonly DateTime Start = new(2026, 11, 13, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void OverdueCronRunsEveryMinuteToPreventDelay()
    {
        Assert.Equal("* * * * *", TimingConfiguration.Jobs.PlatformFeeOverdueCron);
    }

    [Fact]
    public void PaymentReconciliationCronRunsEveryMinute()
    {
        Assert.Equal("* * * * *", TimingConfiguration.Jobs.PlatformFeePaymentReconciliationCron);
    }

    [Fact]
    public void ShopSuspensionRulesCheckAllReasons()
    {
        var shop = new Shop
        {
            Id = Guid.NewGuid(),
            Status = ShopStatus.Suspended,
            SuspendedForPlatformFees = true
        };

        Assert.True(ShopSuspensionRules.HasReasons(shop));
        Assert.False(ShopSuspensionRules.ReactivateIfResolved(shop));
        Assert.Equal(ShopStatus.Suspended, shop.Status);

        // When fee is paid and flag cleared, reactivate
        shop.SuspendedForPlatformFees = false;
        Assert.False(ShopSuspensionRules.HasReasons(shop));
        Assert.True(ShopSuspensionRules.ReactivateIfResolved(shop));
        Assert.Equal(ShopStatus.Active, shop.Status);
    }

    [Fact]
    public void ShopCannotReactivateIfOtherReasonsRemain()
    {
        var shop = new Shop
        {
            Id = Guid.NewGuid(),
            Status = ShopStatus.Suspended,
            SuspendedForPlatformFees = false,
            SuspendedByAdmin = true
        };

        Assert.True(ShopSuspensionRules.HasReasons(shop));
        Assert.False(ShopSuspensionRules.ReactivateIfResolved(shop));
        Assert.Equal(ShopStatus.Suspended, shop.Status);
    }

    [Fact]
    public void OverduePlatformFeeStatementRequiresSettlement()
    {
        var statement = new PlatformFeeStatement
        {
            Id = Guid.NewGuid(),
            Status = PlatformFeeStatementStatus.Issued,
            DueAt = Start.AddDays(-1), // Past due
            TotalFeeAmount = 500000
        };

        Assert.True(statement.DueAt < Start);
        Assert.True(EmailReminderRules.UnpaidFee(statement));
    }

    [Fact]
    public void PaymentExpirationGraceWindowAppliesForOverdueStatement()
    {
        var statement = new PlatformFeeStatement
        {
            Id = Guid.NewGuid(),
            Status = PlatformFeeStatementStatus.Overdue,
            DueAt = Start.AddDays(-2),
            TotalFeeAmount = 250000
        };

        var now = Start;
        var expiresAtTime = statement.DueAt.HasValue && statement.DueAt.Value > now
            ? statement.DueAt.Value
            : now.AddDays(TimingConfiguration.PlatformFees.PaymentLinkGraceDays);

        Assert.True(expiresAtTime > now);
        Assert.Equal(now.AddDays(1), expiresAtTime);
    }
}
