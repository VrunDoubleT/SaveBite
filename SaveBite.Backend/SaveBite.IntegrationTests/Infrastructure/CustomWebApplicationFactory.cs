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

    public CustomWebApplicationFactory()
    {
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
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var testConfigs = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = DatabaseFixture.TestConnectionString
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

            // 3. Mock Redis so tests don't require a running Redis instance
            services.RemoveAll(typeof(IConnectionMultiplexer));
            services.AddSingleton(Mock.Of<IConnectionMultiplexer>());

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
