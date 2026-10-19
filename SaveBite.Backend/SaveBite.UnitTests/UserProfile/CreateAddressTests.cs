using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using Moq;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Validators;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.UserProfile;

/// <summary>
/// Unit Tests for Feature: Create Address
/// Service: UserAddressService.CreateAddressAsync & CreateAddressRequestValidator
/// Function Code: UT-18
/// Sheet Name: UT_CreateAddress
/// </summary>
public class CreateAddressTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IUserAddressRepository> _addressRepoMock = new();
    private readonly UserAddressService _service;
    private readonly CreateAddressRequestValidator _validator = new();

    // Fixed Test Identifiers
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public CreateAddressTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new UserAddressService(_addressRepoMock.Object);
    }

    // =========================================================================
    // UTCID01: Tạo địa chỉ đầy đủ thông tin hợp lệ và đặt làm mặc định (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID01_CreateAddress_FullInfo_SetDefault_ShouldSucceed_Normal()
    {
        // Arrange
        var request = new UserAddressRequests.CreateAddressRequest
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
            .Setup(r => r.CreateAsync(It.IsAny<UserAddress>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserAddress addr, bool isDef, CancellationToken _) =>
            {
                addr.Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
                addr.IsDefault = isDef;
                return addr;
            });

        // Act
        var result = await _service.CreateAddressAsync(_userId, request);

        // Assert & Log
        PrintSuccess("UTCID01", result);
        result.Should().NotBeNull();
        result.Label.Should().Be("Home");
        result.AddressLine.Should().Be("123 Le Loi Street");
        result.IsDefault.Should().BeTrue();

        _addressRepoMock.Verify(r => r.CreateAsync(
            It.Is<UserAddress>(a => a.UserId == _userId && a.Label == "Home"),
            true,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID02: Tạo địa chỉ phụ (IsDefault = false) với các trường tùy chọn là null (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID02_CreateAddress_SecondaryAddress_OptionalFieldsNull_Normal()
    {
        // Arrange
        var request = new UserAddressRequests.CreateAddressRequest
        {
            Label = null,
            AddressLine = "456 Nguyen Hue Boulevard",
            Ward = null,
            District = null,
            City = null,
            Latitude = 10.7745,
            Longitude = 106.7025,
            IsDefault = false
        };

        _addressRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<UserAddress>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserAddress addr, bool isDef, CancellationToken _) =>
            {
                addr.Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
                addr.IsDefault = isDef;
                return addr;
            });

        // Act
        var result = await _service.CreateAddressAsync(_userId, request);

        // Assert & Log
        PrintSuccess("UTCID02", result);
        result.Should().NotBeNull();
        result.AddressLine.Should().Be("456 Nguyen Hue Boulevard");
        result.Label.Should().BeNull();
        result.IsDefault.Should().BeFalse();

        _addressRepoMock.Verify(r => r.CreateAsync(
            It.Is<UserAddress>(a => a.UserId == _userId && a.Label == null),
            false,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID03: Request null truyền vào service -> Ném NullReferenceException (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID03_CreateAddress_NullRequest_ThrowsException_Abnormal()
    {
        // Arrange
        UserAddressRequests.CreateAddressRequest? nullRequest = null;

        // Act
        var act = () => _service.CreateAddressAsync(_userId, nullRequest!);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<NullReferenceException>();
        PrintException("UTCID03", ex.Which.Message);

        _addressRepoMock.Verify(r => r.CreateAsync(It.IsAny<UserAddress>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID04: Tọa độ vượt ngoài biên hợp lệ (Latitude = 95.0 > 90) -> Validator từ chối (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID04_CreateAddress_LatitudeOutOfRange_ValidationFails_Abnormal()
    {
        // Arrange
        var request = new UserAddressRequests.CreateAddressRequest
        {
            Label = "Invalid Spot",
            AddressLine = "123 Le Loi Street",
            Latitude = 95.0, // Vượt ngoài khoảng [-90, 90]
            Longitude = 106.7009,
            IsDefault = false
        };

        // Act
        var validationResult = await _validator.ValidateAsync(request);

        // Assert & Log
        PrintException("UTCID04", validationResult.Errors.First().ErrorMessage);
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == "Latitude" && e.ErrorMessage.Contains("Latitude must be between -90 and 90"));
    }

    // =========================================================================
    // UTCID05: AddressLine rỗng (chuỗi rỗng "") -> Validator từ chối (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID05_CreateAddress_EmptyAddressLine_ValidationFails_Abnormal()
    {
        // Arrange
        var request = new UserAddressRequests.CreateAddressRequest
        {
            Label = "Home",
            AddressLine = "", // Bắt buộc không được để trống
            Latitude = 10.7769,
            Longitude = 106.7009,
            IsDefault = false
        };

        // Act
        var validationResult = await _validator.ValidateAsync(request);

        // Assert & Log
        PrintException("UTCID05", validationResult.Errors.First().ErrorMessage);
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

    private void PrintException(string tcid, string message)
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: ERROR / REJECTED");
        _output.WriteLine($"MESSAGE: {message}");
        _output.WriteLine("========================================");
    }
}
