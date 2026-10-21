using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SaveBite.Backend.Configurations;
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
/// Unit Tests for Feature: Create New Product
/// Service: ProductService.CreateAsync & CreateProductRequestValidator
/// Function Code: UT_CreateNewProduct
/// Sheet Name: UT_CreateNewProduct
/// Business Rules: BR-073, BR-074, BR-075, BR-078, BR-081, BR-183
/// </summary>
public class CreateNewProductTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IProductRepository> _repositoryMock = new();
    private readonly Mock<ICloudinaryService> _cloudinaryMock = new();
    private readonly Mock<ILogger<ProductService>> _loggerMock = new();
    private readonly ProductService _service;
    private readonly CreateProductRequestValidator _validator;

    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _shopId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _categoryId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private readonly Guid _invalidCategoryId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public CreateNewProductTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new ProductService(
            _repositoryMock.Object,
            _cloudinaryMock.Object,
            TimeProvider.System,
            _loggerMock.Object
        );

        var cloudinaryOptions = Options.Create(new CloudinarySettings
        {
            MaxFileSizeBytes = 5 * 1024 * 1024,
            AllowedContentTypes = new[] { "image/jpeg", "image/png", "image/webp" }
        });
        var imageValidator = new ProductImageFileValidator(cloudinaryOptions);
        var valueValidator = new CreateProductValueRequestValidator(imageValidator);
        var attributeValidator = new CreateProductAttributeRequestValidator(valueValidator);
        var variantPriceValidator = new ProductVariantPriceRequestValidator();
        _validator = new CreateProductRequestValidator(imageValidator, attributeValidator, variantPriceValidator);
    }

    // =========================================================================
    // UTCID01: Tạo sản phẩm mới thành công với danh mục Active và biến thể hợp lệ (Normal - N)
    // Business Rules: BR-073, BR-074, BR-075, BR-081, BR-183
    // =========================================================================
    [Fact]
    public async Task UTCID01_CreateProduct_ValidInput_CreatesSuccessfullyAndStartsActive_Normal()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            CategoryId = _categoryId,
            Name = "Bánh Mì Pate Đặc Biệt",
            Description = "Bánh mì nóng giòn kẹp pate nhà làm",
            Images = new(),
            Attributes = new(),
            Variants = new()
            {
                new ProductVariantPriceRequest
                {
                    ValueIds = new(),
                    OriginalPrice = 30000m,
                    DealPrice = 20000m
                }
            }
        };

        _repositoryMock
            .Setup(r => r.GetAvailableShopAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductShopOption(_shopId, "Tiệm Bánh Mì SaveBite"));

        _repositoryMock
            .Setup(r => r.ActiveCategoryExistsAsync(_categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repositoryMock
            .Setup(r => r.GetOwnedAsync(_userId, _shopId, It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid uId, Guid sId, Guid pId, bool incDel, CancellationToken ct) => new Product
            {
                Id = pId,
                ShopId = _shopId,
                CategoryId = _categoryId,
                Name = "Bánh Mì Pate Đặc Biệt",
                Description = "Bánh mì nóng giòn kẹp pate nhà làm",
                Status = ProductStatus.Active,
                Category = new Category { Id = _categoryId, Name = "Bánh Mì" },
                Variants = new List<ProductVariant>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ProductId = pId,
                        Sku = "BM-01",
                        OriginalPrice = 30000m,
                        DealPrice = 20000m,
                        IsActive = true
                    }
                }
            });

        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateAsync(_userId, request, CancellationToken.None);

        // Assert & Log
        PrintSuccess("UTCID01", result, "200 OK");
        result.Should().NotBeNull();
        result.Name.Should().Be("Bánh Mì Pate Đặc Biệt");
        result.Status.Should().Be(ProductStatus.Active);
        result.Variants.Should().HaveCount(1);
        result.Variants[0].OriginalPrice.Should().Be(30000m);
        result.Variants[0].DealPrice.Should().Be(20000m);

        _repositoryMock.Verify(r => r.Add(It.IsAny<Product>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID02: Người dùng không sở hữu cửa hàng hoặc cửa hàng không khả dụng -> Ném NotFoundException (Abnormal - A)
    // Business Rules: BR-073
    // =========================================================================
    [Fact]
    public async Task UTCID02_CreateProduct_NoShopAvailable_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            CategoryId = _categoryId,
            Name = "Bánh Mì Pate",
            Variants = new()
            {
                new ProductVariantPriceRequest
                {
                    ValueIds = new(),
                    OriginalPrice = 30000m,
                    DealPrice = 20000m
                }
            }
        };

        _repositoryMock
            .Setup(r => r.GetAvailableShopAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductShopOption?)null);

        // Act
        Func<Task> act = async () => await _service.CreateAsync(_userId, request, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.Message.Should().Be("Shop was not found or is unavailable.");

        _output.WriteLine("================ UTCID02 ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 404 Not Found");
        _output.WriteLine($"ERROR: {exception.Which.Message}");
        _output.WriteLine("========================================");
    }

    // =========================================================================
    // UTCID03: Danh mục không tồn tại hoặc không ở trạng thái Active -> Ném BadRequestException (Abnormal - A)
    // Business Rules: BR-074
    // =========================================================================
    [Fact]
    public async Task UTCID03_CreateProduct_CategoryNotActiveOrNotFound_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            CategoryId = _invalidCategoryId,
            Name = "Bánh Mì Pate",
            Variants = new()
            {
                new ProductVariantPriceRequest
                {
                    ValueIds = new(),
                    OriginalPrice = 30000m,
                    DealPrice = 20000m
                }
            }
        };

        _repositoryMock
            .Setup(r => r.GetAvailableShopAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductShopOption(_shopId, "Tiệm Bánh Mì SaveBite"));

        _repositoryMock
            .Setup(r => r.ActiveCategoryExistsAsync(_invalidCategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _service.CreateAsync(_userId, request, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.Message.Should().Be("An active category is required.");

        _output.WriteLine("================ UTCID03 ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 400 Bad Request");
        _output.WriteLine($"ERROR: {exception.Which.Message}");
        _output.WriteLine("========================================");
    }

    // =========================================================================
    // UTCID04: Tên sản phẩm để trống -> Validator từ chối (Boundary - B)
    // Business Rules: BR-074
    // =========================================================================
    [Fact]
    public async Task UTCID04_CreateProduct_EmptyName_ValidationFails_Boundary()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            CategoryId = _categoryId,
            Name = "",
            Variants = new()
            {
                new ProductVariantPriceRequest
                {
                    ValueIds = new(),
                    OriginalPrice = 30000m,
                    DealPrice = 20000m
                }
            }
        };

        // Act
        var validationResult = await _validator.ValidateAsync(request);

        // Assert & Log
        PrintValidationError("UTCID04", validationResult.Errors.First().ErrorMessage);
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    // =========================================================================
    // UTCID05: Sản phẩm không có biến thể nào -> Validator từ chối (Abnormal - A)
    // Business Rules: BR-183
    // =========================================================================
    [Fact]
    public async Task UTCID05_CreateProduct_NoVariants_ValidationFails_Abnormal()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            CategoryId = _categoryId,
            Name = "Bánh Mì Pate",
            Variants = new()
        };

        // Act
        var validationResult = await _validator.ValidateAsync(request);

        // Assert & Log
        PrintValidationError("UTCID05", validationResult.Errors.First().ErrorMessage);
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Variants");
    }

    // =========================================================================
    // UTCID06: Giá deal lớn hơn hoặc bằng giá gốc (DealPrice >= OriginalPrice) -> Validator từ chối (Boundary - B)
    // Business Rules: BR-081
    // =========================================================================
    [Fact]
    public async Task UTCID06_CreateProduct_DealPriceNotLessThanOriginalPrice_ValidationFails_Boundary()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            CategoryId = _categoryId,
            Name = "Bánh Mì Pate",
            Variants = new()
            {
                new ProductVariantPriceRequest
                {
                    ValueIds = new(),
                    OriginalPrice = 30000m,
                    DealPrice = 35000m
                }
            }
        };

        // Act
        var validationResult = await _validator.ValidateAsync(request);

        // Assert & Log
        PrintValidationError("UTCID06", validationResult.Errors.First().ErrorMessage);
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName.Contains("DealPrice"));
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
