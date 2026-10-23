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

internal sealed class OrderReadTestContext
{
    public UnitTestClock Clock { get; } = new();
    public Order Order { get; }
    public Mock<ICheckoutRepository> Repo { get; } = new();
    public Mock<ICheckoutPaymentService> Payments { get; } = new();
    public OrderService Service { get; }
    public OrderReadTestContext()
    {
        Order = UnitTestData.Order(Clock.Now);
        Repo.Setup(x => x.GetOrderPageAsync(It.IsAny<Guid>(), It.IsAny<OrderStatus?>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<Order>)new[] { Order }, false, (IReadOnlyDictionary<string, int>)new Dictionary<string, int> { ["Pending"] = 1 }));
        Repo.Setup(x => x.GetOrdersAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new[] { Order });
        Service = new OrderService(Repo.Object, Payments.Object, Clock);
    }
}
