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
/// Unit Tests for Feature: Add Attributes Values
/// Service: ProductService.AddValuesAsync & AddProductValuesRequestValidator
/// Function Code: UT_AddAttributesValues
/// Sheet Name: UT_AddAttributesValues
/// Business Rules: BR-073, BR-078, BR-081, BR-183
/// </summary>
public class AddAttributesValuesTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IProductRepository> _repositoryMock = new();
    private readonly Mock<ICloudinaryService> _cloudinaryMock = new();
    private readonly Mock<ILogger<ProductService>> _loggerMock = new();
    private readonly ProductService _service;
    private readonly AddProductValuesRequestValidator _validator;

    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _shopId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _productId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private readonly Guid _attributeId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private readonly Guid _existingValueId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private readonly Guid _newValueId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private readonly Guid _invalidAttributeId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private readonly Guid _nonExistentProductId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    public AddAttributesValuesTests(ITestOutputHelper output)
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
        var attributeValidator = new AddProductAttributeValuesRequestValidator(valueValidator);
        var variantPriceValidator = new ProductVariantPriceRequestValidator();
        _validator = new AddProductValuesRequestValidator(attributeValidator, variantPriceValidator);
    }

    private Product CreateSampleProduct()
    {
        var categoryId = Guid.NewGuid();
        var product = new Product
        {
            Id = _productId,
            ShopId = _shopId,
            CategoryId = categoryId,
            Name = "Signature Pate Bread",
            Status = ProductStatus.Active,
            Category = new Category { Id = categoryId, Name = "Bakery" },
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var attribute = new ProductAttribute
        {
            Id = _attributeId,
            ProductId = _productId,
            Name = "Size",
            SortOrder = 0
        };

        var existingValue = new ProductAttributeValue
        {
            Id = _existingValueId,
            AttributeId = _attributeId,
            Value = "Regular",
            SortOrder = 0,
            Attribute = attribute
        };

        attribute.Values.Add(existingValue);
        product.Attributes.Add(attribute);

        var variant = new ProductVariant
        {
            Id = Guid.NewGuid(),
            ProductId = _productId,
            Sku = "SKU-REGULAR",
            OriginalPrice = 30000m,
            DealPrice = 20000m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        variant.VariantValues.Add(new ProductVariantValue
        {
            VariantId = variant.Id,
            AttributeValueId = _existingValueId,
            AttributeValue = existingValue
        });

        product.Variants.Add(variant);
        return product;
    }

    // =========================================================================
    // UTCID01: Store owner adds new attribute value with new variant price successfully (Normal - N)
    // Business Rules: BR-073, BR-078, BR-081
    // =========================================================================
    [Fact]
    public async Task UTCID01_AddValues_ValidNewValue_AddsValueAndVariantSuccessfully_Normal()
    {
        // Arrange
        var product = CreateSampleProduct();
        var request = new AddProductValuesRequest
        {
            Attributes = new List<AddProductAttributeValuesRequest>
            {
                new()
                {
                    AttributeId = _attributeId,
                    Values = new List<CreateProductValueRequest>
                    {
                        new() { Id = _newValueId, Value = "Large" }
                    }
                }
            },
            Variants = new List<ProductVariantPriceRequest>
            {
                new()
                {
                    ValueIds = new List<Guid> { _newValueId },
                    OriginalPrice = 35000m,
                    DealPrice = 25000m
                }
            }
        };

        _repositoryMock
            .Setup(r => r.GetAvailableShopAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductShopOption(_shopId, "SaveBite Bakery"));

        _repositoryMock
            .Setup(r => r.GetOwnedAsync(_userId, _shopId, _productId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _repositoryMock
            .Setup(r => r.AnyValueIdsExistAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.AddValuesAsync(_userId, _productId, request, CancellationToken.None);

        // Assert & Log
        PrintSuccess("UTCID01", result, "200 OK");
        result.Should().NotBeNull();
        product.Attributes.First().Values.Should().HaveCount(2);
        product.Variants.Should().HaveCount(2);

        _repositoryMock.Verify(r => r.AddValues(It.IsAny<IEnumerable<ProductAttributeValue>>()), Times.Once);
        _repositoryMock.Verify(r => r.AddVariants(It.IsAny<IEnumerable<ProductVariant>>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID02: Attribute ID does not belong to product -> Throws BadRequest (Abnormal - A)
    // Business Rules: BR-078
    // =========================================================================
    [Fact]
    public async Task UTCID02_AddValues_AttributeNotBelongingToProduct_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var product = CreateSampleProduct();
        var request = new AddProductValuesRequest
        {
            Attributes = new List<AddProductAttributeValuesRequest>
            {
                new()
                {
                    AttributeId = _invalidAttributeId,
                    Values = new List<CreateProductValueRequest>
                    {
                        new() { Id = _newValueId, Value = "Extra Spicy" }
                    }
                }
            },
            Variants = new List<ProductVariantPriceRequest>
            {
                new()
                {
                    ValueIds = new List<Guid> { _newValueId },
                    OriginalPrice = 35000m,
                    DealPrice = 25000m
                }
            }
        };

        _repositoryMock
            .Setup(r => r.GetAvailableShopAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductShopOption(_shopId, "SaveBite Bakery"));

        _repositoryMock
            .Setup(r => r.GetOwnedAsync(_userId, _shopId, _productId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _repositoryMock
            .Setup(r => r.AnyValueIdsExistAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _service.AddValuesAsync(_userId, _productId, request, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.Message.Should().Be("Values can only be added to existing attributes of this product.");

        _output.WriteLine("================ UTCID02 ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 400 Bad Request");
        _output.WriteLine($"ERROR: {exception.Which.Message}");
        _output.WriteLine("========================================");
    }

    // =========================================================================
    // UTCID03: Value name already exists in attribute -> Throws BadRequest (Abnormal - A)
    // Business Rules: BR-078
    // =========================================================================
    [Fact]
    public async Task UTCID03_AddValues_DuplicateValueName_ThrowsBadRequest_Abnormal()
    {
        // Arrange
        var product = CreateSampleProduct();
        var request = new AddProductValuesRequest
        {
            Attributes = new List<AddProductAttributeValuesRequest>
            {
                new()
                {
                    AttributeId = _attributeId,
                    Values = new List<CreateProductValueRequest>
                    {
                        new() { Id = _newValueId, Value = "Regular" } // duplicate name
                    }
                }
            },
            Variants = new List<ProductVariantPriceRequest>
            {
                new()
                {
                    ValueIds = new List<Guid> { _newValueId },
                    OriginalPrice = 35000m,
                    DealPrice = 25000m
                }
            }
        };

        _repositoryMock
            .Setup(r => r.GetAvailableShopAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductShopOption(_shopId, "SaveBite Bakery"));

        _repositoryMock
            .Setup(r => r.GetOwnedAsync(_userId, _shopId, _productId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _repositoryMock
            .Setup(r => r.AnyValueIdsExistAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _service.AddValuesAsync(_userId, _productId, request, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.Message.Should().Contain("must be unique");

        _output.WriteLine("================ UTCID03 ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 400 Bad Request");
        _output.WriteLine($"ERROR: {exception.Which.Message}");
        _output.WriteLine("========================================");
    }

    // =========================================================================
    // UTCID04: Product not found or does not belong to shop -> Throws NotFound (Abnormal - A)
    // Business Rules: BR-073
    // =========================================================================
    [Fact]
    public async Task UTCID04_AddValues_ProductNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        var request = new AddProductValuesRequest
        {
            Attributes = new List<AddProductAttributeValuesRequest>
            {
                new()
                {
                    AttributeId = _attributeId,
                    Values = new List<CreateProductValueRequest>
                    {
                        new() { Id = _newValueId, Value = "Large" }
                    }
                }
            },
            Variants = new List<ProductVariantPriceRequest>
            {
                new()
                {
                    ValueIds = new List<Guid> { _newValueId },
                    OriginalPrice = 35000m,
                    DealPrice = 25000m
                }
            }
        };

        _repositoryMock
            .Setup(r => r.GetAvailableShopAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductShopOption(_shopId, "SaveBite Bakery"));

        _repositoryMock
            .Setup(r => r.GetOwnedAsync(_userId, _shopId, _nonExistentProductId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        Func<Task> act = async () => await _service.AddValuesAsync(_userId, _nonExistentProductId, request, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<AppException>();
        exception.Which.Message.Should().Be("Product was not found or is unavailable.");

        _output.WriteLine("================ UTCID04 ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 404 Not Found");
        _output.WriteLine($"ERROR: {exception.Which.Message}");
        _output.WriteLine("========================================");
    }

    // =========================================================================
    // UTCID05: Value string is empty -> Validator rejects (Boundary - B)
    // Business Rules: BR-078
    // =========================================================================
    [Fact]
    public async Task UTCID05_AddValues_EmptyValueString_ValidationFails_Boundary()
    {
        // Arrange
        var request = new AddProductValuesRequest
        {
            Attributes = new List<AddProductAttributeValuesRequest>
            {
                new()
                {
                    AttributeId = _attributeId,
                    Values = new List<CreateProductValueRequest>
                    {
                        new() { Id = _newValueId, Value = "" }
                    }
                }
            },
            Variants = new List<ProductVariantPriceRequest>
            {
                new()
                {
                    ValueIds = new List<Guid> { _newValueId },
                    OriginalPrice = 35000m,
                    DealPrice = 25000m
                }
            }
        };

        // Act
        var validationResult = await _validator.ValidateAsync(request);

        // Assert & Log
        PrintValidationError("UTCID05", validationResult.Errors.First().ErrorMessage);
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName.Contains("Value"));
    }

    // =========================================================================
    // UTCID06: Variant deal price >= original price -> Validator rejects (Boundary - B)
    // Business Rules: BR-081
    // =========================================================================
    [Fact]
    public async Task UTCID06_AddValues_DealPriceNotLessThanOriginalPrice_ValidationFails_Boundary()
    {
        // Arrange
        var request = new AddProductValuesRequest
        {
            Attributes = new List<AddProductAttributeValuesRequest>
            {
                new()
                {
                    AttributeId = _attributeId,
                    Values = new List<CreateProductValueRequest>
                    {
                        new() { Id = _newValueId, Value = "Large" }
                    }
                }
            },
            Variants = new List<ProductVariantPriceRequest>
            {
                new()
                {
                    ValueIds = new List<Guid> { _newValueId },
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
