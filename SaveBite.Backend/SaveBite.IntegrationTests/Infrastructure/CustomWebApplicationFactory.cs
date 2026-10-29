using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using RabbitMQ.Client;
using SaveBite.Backend.Caching.Interfaces;
using SaveBite.Backend.Data;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Services.Interfaces;
using StackExchange.Redis;

namespace SaveBite.IntegrationTests.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<IEmailService> EmailServiceMock { get; } = new();
    public Mock<ICloudinaryService> CloudinaryServiceMock { get; } = new();
    public Mock<IRabbitMqPublisher> RabbitMqPublisherMock { get; } = new();
    public Mock<IFlashDealRedisService> FlashDealRedisServiceMock { get; } = new();
    public Mock<ICheckoutPaymentGateway> CheckoutPaymentGatewayMock { get; } = new();

    public CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("Checkout__PaymentProvider", "PayOS");
        CloudinaryServiceMock
            .Setup(c => c.UploadImageAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudinaryUploadResult("https://res.cloudinary.com/demo/image/upload/sample.jpg", "sample_public_id"));

        CloudinaryServiceMock
            .Setup(c => c.UploadDocumentAsync(It.IsAny<IFormFile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudinaryUploadResult("https://res.cloudinary.com/demo/raw/upload/sample.pdf", "sample_doc_id"));

        CloudinaryServiceMock
            .Setup(c => c.DeleteImageAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        FlashDealRedisServiceMock
            .Setup(x => x.GetNearbyDealsAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
            .ReturnsAsync(new List<SaveBite.Backend.Models.Responses.FlashDealResponse>());

        FlashDealRedisServiceMock
            .Setup(x => x.GetDealsByShopAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<SaveBite.Backend.Models.Responses.FlashDealResponse>());

        FlashDealRedisServiceMock
            .Setup(x => x.GetDealAsync(It.IsAny<Guid>()))
            .ReturnsAsync((SaveBite.Backend.Models.Responses.FlashDealResponse?)null);

        CheckoutPaymentGatewayMock
            .Setup(x => x.IsConfigured(It.IsAny<SaveBite.Backend.Models.Entities.ShopPaymentConfig?>()))
            .Returns(true);

        CheckoutPaymentGatewayMock
            .Setup(x => x.CreateAsync(It.IsAny<SaveBite.Backend.Models.Entities.Order>(), It.IsAny<SaveBite.Backend.Models.Entities.Payment>(), It.IsAny<SaveBite.Backend.Models.Entities.ShopPaymentConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SaveBite.Backend.Models.Entities.Order o, SaveBite.Backend.Models.Entities.Payment p, SaveBite.Backend.Models.Entities.ShopPaymentConfig c, CancellationToken ct) =>
                new GatewayPayment("PENDING", p.GatewayOrderCode ?? 123456789, p.Amount, 0, "link_test_123", "https://pay.payos.vn/web/test123456", "00020101021238540010A00000072701260006970422..."));

        CheckoutPaymentGatewayMock
            .Setup(x => x.GetAsync(It.IsAny<SaveBite.Backend.Models.Entities.Payment>(), It.IsAny<SaveBite.Backend.Models.Entities.ShopPaymentConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SaveBite.Backend.Models.Entities.Payment p, SaveBite.Backend.Models.Entities.ShopPaymentConfig c, CancellationToken ct) =>
                new GatewayPayment(p.Status == SaveBite.Backend.Models.Enums.PaymentStatus.Succeeded ? "PAID" : "PENDING",
                    p.GatewayOrderCode ?? 123456789, p.Amount, p.Status == SaveBite.Backend.Models.Enums.PaymentStatus.Succeeded ? p.Amount : 0,
                    "link_test_123", "https://pay.payos.vn/web/test123456", "00020101021238540010A00000072701260006970422..."));

        CheckoutPaymentGatewayMock
            .Setup(x => x.CancelAsync(It.IsAny<SaveBite.Backend.Models.Entities.Payment>(), It.IsAny<SaveBite.Backend.Models.Entities.ShopPaymentConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SaveBite.Backend.Models.Entities.Payment p, SaveBite.Backend.Models.Entities.ShopPaymentConfig c, CancellationToken ct) =>
                new GatewayPayment("CANCELLED", p.GatewayOrderCode ?? 123456789, p.Amount, 0, "link_test_123", null));

        CheckoutPaymentGatewayMock
            .Setup(x => x.VerifyWebhook(It.IsAny<System.Text.Json.JsonElement>(), It.IsAny<SaveBite.Backend.Models.Entities.ShopPaymentConfig>()))
            .Returns(true);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var testConfigs = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = DatabaseFixture.TestConnectionString,
                ["Checkout:PaymentProvider"] = "PayOS"
            };
            configBuilder.AddInMemoryCollection(testConfigs);
        });

        builder.ConfigureTestServices(services =>
        {
            // 1. Replace DbContext with Test Database
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(DatabaseFixture.TestConnectionString);
            });

            // 2. Mock 3rd-party external services
            services.RemoveAll(typeof(IEmailService));
            services.AddSingleton(EmailServiceMock.Object);

            services.RemoveAll(typeof(ICloudinaryService));
            services.AddSingleton(CloudinaryServiceMock.Object);

            services.RemoveAll(typeof(ICheckoutPaymentGateway));
            services.AddSingleton(CheckoutPaymentGatewayMock.Object);

            // 3. Mock Redis so tests don't require a running Redis instance
            var redisMock = new Mock<IConnectionMultiplexer>();
            var redisDbMock = new Mock<IDatabase>();
            redisMock.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(redisDbMock.Object);
            services.RemoveAll(typeof(IConnectionMultiplexer));
            services.AddSingleton(redisMock.Object);

            services.RemoveAll(typeof(IRedisCacheService));
            services.AddSingleton(Mock.Of<IRedisCacheService>());

            services.RemoveAll(typeof(IAuthRedisService));
            services.AddSingleton(Mock.Of<IAuthRedisService>());

            services.RemoveAll(typeof(IFlashDealRedisService));
            services.AddSingleton(FlashDealRedisServiceMock.Object);

            // 4. Mock RabbitMQ and remove its hosted background consumers
            services.RemoveAll(typeof(IConnection));
            services.AddSingleton(Mock.Of<IConnection>());

            services.RemoveAll(typeof(IRabbitMqPublisher));
            services.AddSingleton(RabbitMqPublisherMock.Object);

            var hostedServices = services.Where(d => d.ServiceType == typeof(IHostedService)).ToList();
            foreach (var hs in hostedServices)
            {
                var implName = hs.ImplementationType?.FullName ?? "";
                if (implName.Contains("RabbitMq") || implName.Contains("Messaging") || implName.Contains("Consumer"))
                {
                    services.Remove(hs);
                }
            }
        });
    }
}
