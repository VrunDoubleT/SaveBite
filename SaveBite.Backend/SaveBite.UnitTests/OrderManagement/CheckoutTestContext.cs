using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.Caching.Interfaces;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using SaveBite.Api.Tests.UnitTests.Support;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Api.Tests.UnitTests.Support;

internal sealed class CheckoutTestContext
{
    public UnitTestClock Clock { get; } = new();
    public User User { get; } = UnitTestData.Customer();
    public CartItem Item { get; }
    public TrustLevel Level { get; }
    public List<CartItem> Items { get; } = new();
    public bool PermanentLock { get; set; }
    public bool Recovering { get; set; }
    public CheckoutQuote? Quote { get; set; }
    public Order? CreatedOrder { get; set; }
    public Order? ExistingOrder { get; set; }
    public Mock<ICheckoutRepository> Repo { get; } = new();
    public Mock<ITravelTimeService> Travel { get; } = new();
    public Mock<ICheckoutPaymentGateway> Gateway { get; } = new();
    public Mock<ICheckoutPaymentService> Payments { get; } = new();
    public Mock<ICheckoutStockCache> Stock { get; } = new();
    public Mock<ICheckoutQuoteStore> Quotes { get; } = new();
    public Mock<ICheckoutPaymentExpirationScheduler> Scheduler { get; } = new();
    public Mock<IOrderEventPublisher> Events { get; } = new();
    public Mock<IShopOrderEventService> Realtime { get; } = new();
    public Mock<IOrderCodeCache> Codes { get; } = new();
    public CheckoutService Service { get; }
    public Guid ShopId => Item.FlashDealVariant.FlashDeal.ShopId;
    public CheckoutLocation Location => new(10.0294, 105.7508, 10, Clock.Now);
    public CheckoutTestContext()
    {
        Item = UnitTestData.CartItem(User.Id, Clock.Now); Items.Add(Item); Level = UnitTestData.Level(Clock.Now);
        Repo.Setup(x => x.GetStateAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new CheckoutState(User, Items, Level, false, PermanentLock, Recovering, Item.FlashDealVariant.FlashDeal.Shop.PaymentConfig));
        Repo.Setup(x => x.WithUserLockAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Func<Task<Guid>>>(), It.IsAny<CancellationToken>()))
            .Returns((Guid _, Guid _, Func<Task<Guid>> action, CancellationToken _) => action());
        Repo.Setup(x => x.FindRequestAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => ExistingOrder ?? CreatedOrder);
        Repo.Setup(x => x.AddOrder(It.IsAny<Order>())).Callback<Order>(order =>
        { CreatedOrder = order; order.Shop = Item.FlashDealVariant.FlashDeal.Shop; order.Customer = User; });
        Travel.Setup(x => x.EstimateAsync(It.IsAny<CheckoutLocation>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TravelEstimate(1, 5, "route-polyline"));
        Gateway.Setup(x => x.IsConfigured(It.IsAny<ShopPaymentConfig>())).Returns(true);
        Quotes.Setup(x => x.SaveAsync(It.IsAny<CheckoutQuote>(), It.IsAny<CancellationToken>()))
            .Callback<CheckoutQuote, CancellationToken>((quote, _) => Quote = quote).Returns(Task.CompletedTask);
        Quotes.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid user, Guid id, CancellationToken _) => Quote?.UserId == user && Quote.Id == id ? Quote : null);
        Payments.Setup(x => x.StartAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => CheckoutOrderMapper.Map(CreatedOrder ?? ExistingOrder!, now: Clock.Now));
        Scheduler.Setup(x => x.Schedule(It.IsAny<Guid>(), It.IsAny<DateTime>())).Returns("expiry-job");
        Codes.Setup(x => x.NextAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("000124");
        Service = new CheckoutService(Repo.Object, Travel.Object, Gateway.Object, Payments.Object, Stock.Object,
            Clock, Quotes.Object, Scheduler.Object, Events.Object, Realtime.Object, Codes.Object);
    }
    public async Task<PlaceCheckoutOrderRequest> Request(string method = "PayAtShop")
    {
        var quote = await Service.QuoteAsync(User.Id, new CheckoutQuoteRequest(ShopId, Location));
        return new PlaceCheckoutOrderRequest(quote.Id, method, Guid.NewGuid());
    }
}
