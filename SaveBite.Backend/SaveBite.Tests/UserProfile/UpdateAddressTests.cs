using System.Net;
using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using Moq;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Validators;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.UserProfile;

/// <summary>
/// Unit Tests for Feature: Update Address
/// Service: UserAddressService.UpdateAddressAsync & UpdateAddressRequestValidator
/// Function Code: UT-20
/// Sheet Name: UT_UpdateAddress
/// </summary>
public class UpdateAddressTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IUserAddressRepository> _addressRepoMock = new();
    private readonly UserAddressService _service;
    private readonly UpdateAddressRequestValidator _validator = new();

    // Fixed Test Identifiers
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _addressId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _nonExistentAddressId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private readonly Guid _otherUserAddressId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public UpdateAddressTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new UserAddressService(_addressRepoMock.Object);
    }

    private UserAddress CreateExistingAddress(Guid id, Guid userId)
    {
        return new UserAddress
        {
            Id = id,
            UserId = userId,
            Label = "Old Label",
            AddressLine = "Old Address Line",
            Ward = "Old Ward",
            District = "Old District",
            City = "Old City",
            Latitude = 10.0000,
            Longitude = 106.0000,
            IsDefault = false,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-5)
        };
    }

    // =========================================================================
    // UTCID01: Cập nhật địa chỉ đầy đủ thông tin hợp lệ -> Thành công (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID01_UpdateAddress_FullInfo_ShouldSucceed_Normal()
    {
        // Arrange
        var existing = CreateExistingAddress(_addressId, _userId);
        _addressRepoMock
            .Setup(r => r.GetByIdAsync(_userId, _addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var request = new UserAddressRequests.UpdateAddressRequest
        {
            Label = "Home",
            AddressLine = "123 Le Loi Street",
            Ward = "Ben Nghe",
            District = "District 1",
            City = "Ho Chi Minh City",
            Latitude = 10.7769,
            Longitude = 106.7009,
            IsDefault = true
        };

        _addressRepoMock
            .Setup(r => r.UpdateAsync(existing, true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateAddressAsync(_userId, _addressId, request);

        // Assert & Log
        PrintSuccess("UTCID01", result);
        result.Should().NotBeNull();
        result.Label.Should().Be("Home");
        result.AddressLine.Should().Be("123 Le Loi Street");
        result.Ward.Should().Be("Ben Nghe");
        result.District.Should().Be("District 1");
        result.City.Should().Be("Ho Chi Minh City");
        result.Latitude.Should().Be(10.7769);
        result.Longitude.Should().Be(106.7009);

        _addressRepoMock.Verify(r => r.UpdateAsync(existing, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID02: Cập nhật với các trường tùy chọn là null hoặc khoảng trắng -> Chuyển thành null (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID02_UpdateAddress_OptionalFieldsNullOrWhitespace_ShouldSetToNull_Normal()
    {
        // Arrange
        var existing = CreateExistingAddress(_addressId, _userId);
        _addressRepoMock
            .Setup(r => r.GetByIdAsync(_userId, _addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var request = new UserAddressRequests.UpdateAddressRequest
        {
            Label = null,
            AddressLine = "456 Nguyen Hue Boulevard",
            Ward = "   ",
            District = null,
            City = "   ",
            Latitude = 10.7745,
            Longitude = 106.7025,
            IsDefault = false
        };

        _addressRepoMock
            .Setup(r => r.UpdateAsync(existing, false, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateAddressAsync(_userId, _addressId, request);

        // Assert & Log
        PrintSuccess("UTCID02", result);
        result.Should().NotBeNull();
        result.Label.Should().BeNull();
        result.Ward.Should().BeNull();
        result.District.Should().BeNull();
        result.City.Should().BeNull();
        result.AddressLine.Should().Be("456 Nguyen Hue Boulevard");

        _addressRepoMock.Verify(r => r.UpdateAsync(existing, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID03: Địa chỉ không tồn tại trong DB -> 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID03_UpdateAddress_AddressNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        _addressRepoMock
            .Setup(r => r.GetByIdAsync(_userId, _nonExistentAddressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserAddress?)null);

        var request = new UserAddressRequests.UpdateAddressRequest
        {
            AddressLine = "123 Le Loi Street",
            Latitude = 10.7769,
            Longitude = 106.7009
        };

        // Act
        var act = () => _service.UpdateAddressAsync(_userId, _nonExistentAddressId, request);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID03", ex.Which.Message, (int)ex.Which.StatusCode);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Address not found.");

        _addressRepoMock.Verify(r => r.UpdateAsync(It.IsAny<UserAddress>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID04: Địa chỉ thuộc sở hữu của người dùng khác -> 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID04_UpdateAddress_OtherUserAddress_ThrowsNotFound_Abnormal()
    {
        // Arrange
        _addressRepoMock
            .Setup(r => r.GetByIdAsync(_userId, _otherUserAddressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserAddress?)null);

        var request = new UserAddressRequests.UpdateAddressRequest
        {
            AddressLine = "123 Le Loi Street",
            Latitude = 10.7769,
            Longitude = 106.7009
        };

        // Act
        var act = () => _service.UpdateAddressAsync(_userId, _otherUserAddressId, request);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID04", ex.Which.Message, (int)ex.Which.StatusCode);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Address not found.");

        _addressRepoMock.Verify(r => r.UpdateAsync(It.IsAny<UserAddress>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID05: AddressLine rỗng ("") -> Validator từ chối (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID05_UpdateAddress_EmptyAddressLine_ValidationFails_Abnormal()
    {
        // Arrange
        var request = new UserAddressRequests.UpdateAddressRequest
        {
            AddressLine = "", // Bắt buộc không được để trống
            Latitude = 10.7769,
            Longitude = 106.7009
        };

        // Act
        var validationResult = await _validator.ValidateAsync(request);

        // Assert & Log
        PrintException("UTCID05", validationResult.Errors.First().ErrorMessage, 400);
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "AddressLine");
    }

    private void PrintSuccess<T>(string tcid, T result)
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 200 OK");
        _output.WriteLine("RESPONSE:");
        _output.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        _output.WriteLine("========================================");
    }

    private void PrintException(string tcid, string message, int statusCode)
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine($"STATUS: {statusCode} ERROR");
        _output.WriteLine($"MESSAGE: {message}");
        _output.WriteLine("========================================");
    }
}
