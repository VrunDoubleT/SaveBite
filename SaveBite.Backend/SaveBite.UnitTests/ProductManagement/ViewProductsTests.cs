using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using SaveBite.Backend.Validators;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.ProductManagement;

/// <summary>
/// Unit Tests for Feature: View Products
/// Service: ProductService.ListAsync & ProductListRequestValidator
/// Function Code: UT_ViewProducts
/// Sheet Name: UT_ViewProducts
/// Business Rules: BR-073, BR-075, BR-076
/// </summary>
public class ViewProductsTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IProductRepository> _repositoryMock = new();
    private readonly Mock<ICloudinaryService> _cloudinaryMock = new();
    private readonly Mock<ILogger<ProductService>> _loggerMock = new();
    private readonly ProductService _service;
    private readonly ProductListRequestValidator _validator = new();

    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _shopId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _categoryId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public ViewProductsTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new ProductService(
            _repositoryMock.Object,
            _cloudinaryMock.Object,
            TimeProvider.System,
            _loggerMock.Object
        );
    }

    // =========================================================================
    // UTCID01: Chủ cửa hàng xem danh sách sản phẩm thuộc cửa hàng thành công (Normal - N)
    // Business Rules: BR-073, BR-075
    // =========================================================================
    [Fact]
    public async Task UTCID01_ListProducts_StoreOwner_ReturnsPagedProducts_Normal()
    {
        // Arrange
        var request = new ProductListRequest
        {
            Page = 1,
            PageSize = 20,
            Search = null,
            CategoryId = null,
            Status = null
        };

        var products = new List<Product>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ShopId = _shopId,
                CategoryId = _categoryId,
                Name = "Bánh Mì Pate",
                Status = ProductStatus.Active,
                Category = new Category { Id = _categoryId, Name = "Bánh Mì" },
                Variants = new List<ProductVariant>
                {
                    new() { Id = Guid.NewGuid(), OriginalPrice = 30000m, DealPrice = 20000m, IsActive = true }
                },
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                ShopId = _shopId,
                CategoryId = _categoryId,
                Name = "Bánh Mì Trứng",
                Status = ProductStatus.Active,
                Category = new Category { Id = _categoryId, Name = "Bánh Mì" },
                Variants = new List<ProductVariant>
                {
                    new() { Id = Guid.NewGuid(), OriginalPrice = 25000m, DealPrice = 18000m, IsActive = true }
                },
                UpdatedAt = DateTime.UtcNow
            }
        };

        _repositoryMock
            .Setup(r => r.GetAvailableShopAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductShopOption(_shopId, "Tiệm Bánh Mì SaveBite"));

        _repositoryMock
            .Setup(r => r.ListAsync(_shopId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((products, 2));

        // Act
        var result = await _service.ListAsync(_userId, request, CancellationToken.None);

        // Assert & Log
        PrintSuccess("UTCID01", result, "200 OK");
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items[0].Name.Should().Be("Bánh Mì Pate");
        result.Items[0].MinOriginalPrice.Should().Be(30000m);
        result.Items[0].MinDealPrice.Should().Be(20000m);
        result.Items[1].Name.Should().Be("Bánh Mì Trứng");
    }

    // =========================================================================
    // UTCID02: Lọc danh sách theo từ khóa tìm kiếm và danh mục (Normal - N)
    // Business Rules: BR-073
    // =========================================================================
    [Fact]
    public async Task UTCID02_ListProducts_FilterBySearchAndCategory_ReturnsFilteredProducts_Normal()
    {
        // Arrange
        var request = new ProductListRequest
        {
            Page = 1,
            PageSize = 20,
            Search = "Pate",
            CategoryId = _categoryId,
            Status = ProductStatus.Active
        };

        var products = new List<Product>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ShopId = _shopId,
                CategoryId = _categoryId,
                Name = "Bánh Mì Pate",
                Status = ProductStatus.Active,
                Category = new Category { Id = _categoryId, Name = "Bánh Mì" },
                Variants = new List<ProductVariant>
                {
                    new() { Id = Guid.NewGuid(), OriginalPrice = 30000m, DealPrice = 20000m, IsActive = true }
                },
                UpdatedAt = DateTime.UtcNow
            }
        };

        _repositoryMock
            .Setup(r => r.GetAvailableShopAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductShopOption(_shopId, "Tiệm Bánh Mì SaveBite"));

        _repositoryMock
            .Setup(r => r.ListAsync(_shopId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((products, 1));

        // Act
        var result = await _service.ListAsync(_userId, request, CancellationToken.None);

        // Assert & Log
        PrintSuccess("UTCID02", result, "200 OK");
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Bánh Mì Pate");
    }

    // =========================================================================
    // UTCID03: Cửa hàng chưa có sản phẩm nào -> Trả về danh sách rỗng (Normal - N)
    // Business Rules: BR-073
    // =========================================================================
    [Fact]
    public async Task UTCID03_ListProducts_EmptyShop_ReturnsEmptyPagedResult_Normal()
    {
        // Arrange
        var request = new ProductListRequest
        {
            Page = 1,
            PageSize = 20
        };

        _repositoryMock
            .Setup(r => r.GetAvailableShopAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductShopOption(_shopId, "Tiệm Bánh Mì SaveBite"));

        _repositoryMock
            .Setup(r => r.ListAsync(_shopId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Product>(), 0));

        // Act
        var result = await _service.ListAsync(_userId, request, CancellationToken.None);

        // Assert & Log
        PrintSuccess("UTCID03", result, "200 OK");
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    // =========================================================================
    // UTCID04: Người dùng không có cửa hàng hoặc cửa hàng không khả dụng -> Ném NotFoundException (Abnormal - A)
    // Business Rules: BR-073
    // =========================================================================
    [Fact]
    public async Task UTCID04_ListProducts_NoShopAvailable_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var request = new ProductListRequest
        {
            Page = 1,
            PageSize = 20
        };

        _repositoryMock
            .Setup(r => r.GetAvailableShopAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductShopOption?)null);

        // Act
        Func<Task> act = async () => await _service.ListAsync(_userId, request, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.Message.Should().Be("Shop was not found or is unavailable.");

        _output.WriteLine("================ UTCID04 ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 404 Not Found");
        _output.WriteLine($"ERROR: {exception.Which.Message}");
        _output.WriteLine("========================================");
    }

    // =========================================================================
    // UTCID05: Phân trang không hợp lệ (Page = 0) -> Validator từ chối (Boundary - B)
    // =========================================================================
    [Fact]
    public async Task UTCID05_ListProducts_PageZero_ValidationFails_Boundary()
    {
        // Arrange
        var request = new ProductListRequest
        {
            Page = 0,
            PageSize = 20
        };

        // Act
        var validationResult = await _validator.ValidateAsync(request);

        // Assert & Log
        PrintValidationError("UTCID05", validationResult.Errors.First().ErrorMessage);
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Page");
    }

    // =========================================================================
    // UTCID06: Trạng thái lọc không hợp lệ nằm ngoài enum -> Validator từ chối (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID06_ListProducts_InvalidStatusEnum_ValidationFails_Abnormal()
    {
        // Arrange
        var request = new ProductListRequest
        {
            Page = 1,
            PageSize = 20,
            Status = (ProductStatus)999
        };

        // Act
        var validationResult = await _validator.ValidateAsync(request);

        // Assert & Log
        PrintValidationError("UTCID06", validationResult.Errors.First().ErrorMessage);
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Status");
    }

    private void PrintSuccess<T>(string tcid, T result, string status = "200 OK")
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine($"STATUS: {status}");
        _output.WriteLine("RESPONSE:");
        _output.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        _output.WriteLine("========================================");
    }

    private void PrintValidationError(string tcid, string message)
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 400 BadRequest (Validation Failed)");
        _output.WriteLine($"MESSAGE: {message}");
        _output.WriteLine("========================================");
    }
}
