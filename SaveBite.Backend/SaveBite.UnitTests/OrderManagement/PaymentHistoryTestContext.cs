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

internal sealed class PaymentHistoryTestContext
{
    public UnitTestClock Clock { get; } = new();
    public Order Order { get; }
    public Mock<IPaymentHistoryRepository> Payments { get; } = new();
    public Mock<ICheckoutRepository> Orders { get; } = new();
    public PaymentHistoryService Service { get; }
    public PaymentHistoryTestContext()
    {
        Order = UnitTestData.Order(Clock.Now);
        Payments.Setup(x => x.PageAsync(It.IsAny<Guid>(), It.IsAny<PaymentStatus?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<Payment>)Order.Payments.ToList(), 1));
        Payments.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => Order.Payments.First());
        Orders.Setup(x => x.GetOrderAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync(Order);
        Service = new PaymentHistoryService(Payments.Object, Orders.Object, Clock);
    }
}
