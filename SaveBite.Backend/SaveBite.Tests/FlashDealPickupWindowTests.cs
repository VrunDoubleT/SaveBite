using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Helpers;
using Xunit;

namespace SaveBite.Tests;

public class FlashDealPickupWindowTests
{
    // Local: 16:00-18:00, shop closes at 19:00.
    private static readonly DateTime Start = new(2026, 11, 14, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = Start.AddHours(2);

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    [InlineData(60)]
    public void AcceptsPickupBetweenDealEndAndOneHourLater(int minutes)
        => FlashDealPickupWindow.Validate(Start, End, End.AddMinutes(minutes), new(8, 0), new(19, 0));

    [Theory]
    [InlineData(-1)]
    [InlineData(61)]
    public void RejectsPickupOutsideExtensionWindow(int minutes)
        => Assert.Throws<AppException>(() => FlashDealPickupWindow.Validate(Start, End, End.AddMinutes(minutes), new(8, 0), new(20, 0)));

    [Fact]
    public void ClosingSoonLimitsPickupAndAllowsExactClosing()
    {
        Assert.Equal(End.AddMinutes(15), FlashDealPickupWindow.Latest(Start, End, new(8, 0), new(18, 15)));
        FlashDealPickupWindow.Validate(Start, End, End.AddMinutes(15), new(8, 0), new(18, 15));
        Assert.Throws<AppException>(() => FlashDealPickupWindow.Validate(Start, End, End.AddMinutes(16), new(8, 0), new(18, 15)));
    }

    [Fact]
    public void EndingAtShopClosingAllowsNoExtension()
    {
        FlashDealPickupWindow.Validate(Start, End, End, new(8, 0), new(18, 0));
        Assert.Throws<AppException>(() => FlashDealPickupWindow.Validate(Start, End, End.AddSeconds(1), new(8, 0), new(18, 0)));
    }

    [Fact]
    public void TwentyFourSevenCrossesMidnightButStillCapsAtOneHour()
    {
        var start = new DateTime(2026, 11, 14, 15, 30, 0, DateTimeKind.Utc); // 22:30
        var end = start.AddHours(1); // 23:30
        Assert.Equal(end.AddHours(1), FlashDealPickupWindow.Latest(start, end, new(0, 0), new(0, 0)));
        FlashDealPickupWindow.Validate(start, end, end.AddHours(1), new(0, 0), new(0, 0));
        Assert.Throws<AppException>(() => FlashDealPickupWindow.Validate(start, end, end.AddMinutes(61), new(0, 0), new(0, 0)));
    }

    [Theory]
    [InlineData(15, 30)] // starts 22:30
    [InlineData(17, 30)] // starts 00:30 next local day
    public void OvernightShopUsesClosingOfSameOperatingSession(int hour, int minute)
    {
        var start = new DateTime(2026, 11, 14, hour, minute, 0, DateTimeKind.Utc);
        var end = start.AddHours(1);
        var expected = new DateTime(2026, 11, 14, 19, 0, 0, DateTimeKind.Utc); // closes 02:00
        Assert.Equal(end.AddHours(1) < expected ? end.AddHours(1) : expected,
            FlashDealPickupWindow.Latest(start, end, new(20, 0), new(2, 0)));
    }

    [Fact]
    public void CannotSpanClosedHoursEvenIfBothEndpointTimesAppearOpen()
        => Assert.Throws<AppException>(() => FlashDealPickupWindow.Latest(Start, End.AddDays(1), new(8, 0), new(20, 0)));

    [Fact]
    public void EditingCannotShortenCommittedOrderDeadline()
    {
        var committed = End.AddMinutes(35);
        Assert.Throws<AppException>(() => FlashDealPickupWindow.Validate(Start, End, End.AddMinutes(34), new(8, 0), new(19, 0), committed));
        FlashDealPickupWindow.Validate(Start, End, committed, new(8, 0), new(19, 0), committed);
    }

    [Fact]
    public void CustomerCanOrderAt1759WhenPickupCutoffIs1845()
    {
        var now = End.AddMinutes(-1);
        var pickup = CheckoutRules.Pickup(now, 2, 10, End.AddMinutes(45));
        Assert.Equal(now.AddMinutes(10), pickup.SoftDeadlineAt);
        Assert.Equal(now.AddMinutes(20), pickup.HardDeadlineAt);
        Assert.True(pickup.HardDeadlineAt > End);
        Assert.Throws<AppException>(() => CheckoutRules.Pickup(now, 2, 10, End));
    }
    [Fact]
    public void InfeasibleTripExplainsCutoffDistanceTravelAndArrival()
    {
        var error = Assert.Throws<AppException>(() => CheckoutRules.Pickup(End.AddMinutes(-10), 12.5m, 25, End));
        Assert.Contains("Final pickup deadline: 14/11/2026 18:00", error.Message);
        Assert.Contains("12.5 km", error.Message);
        Assert.Contains("25 minutes", error.Message);
        Assert.Contains("Estimated arrival: 14/11/2026 18:15", error.Message);
        Assert.Contains("Time remaining for pickup: 10 minutes", error.Message);
    }

}
