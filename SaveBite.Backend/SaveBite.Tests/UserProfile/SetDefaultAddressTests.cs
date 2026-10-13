using System.Net;
using System.Text.Json;
using FluentAssertions;
using Moq;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.UserProfile;

/// <summary>
/// Unit Tests for Feature: Set Default Address
/// Service: UserAddressService.SetDefaultAddressAsync
/// Function Code: UT-19
/// Sheet Name: UT_SetDefaultAddress
/// </summary>
public class SetDefaultAddressTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IUserAddressRepository> _addressRepoMock = new();
    private readonly UserAddressService _service;

    // Fixed Test Identifiers
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _addressId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _nonExistentAddressId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private readonly Guid _otherUserAddressId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public SetDefaultAddressTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new UserAddressService(_addressRepoMock.Object);
    }

    private UserAddress CreateAddress(Guid id, Guid userId, bool isDefault = false)
    {
        return new UserAddress
        {
            Id = id,
            UserId = userId,
            Label = "Home",
            AddressLine = "123 Le Loi Street",
            Ward = "Ben Nghe",
            District = "District 1",
            City = "Ho Chi Minh City",
            Latitude = 10.7769,
            Longitude = 106.7009,
            IsDefault = isDefault,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
    }

    // =========================================================================
    // UTCID01: Đặt địa chỉ phụ thành địa chỉ mặc định thành công (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID01_SetDefaultAddress_SecondaryAddress_ShouldSucceed_Normal()
    {
        // Arrange
        var address = CreateAddress(_addressId, _userId, isDefault: false);

        _addressRepoMock
            .Setup(r => r.GetByIdAsync(_userId, _addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);

        _addressRepoMock
            .Setup(r => r.SetDefaultAsync(_userId, _addressId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.SetDefaultAddressAsync(_userId, _addressId);

        // Assert & Log
        PrintSuccess("UTCID01", result);
        result.Should().NotBeNull();
        result.Id.Should().Be(_addressId);
        result.IsDefault.Should().BeTrue();

        address.IsDefault.Should().BeTrue();
        _addressRepoMock.Verify(r => r.SetDefaultAsync(_userId, _addressId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID02: Địa chỉ đã là địa chỉ mặc định từ trước -> 409 Conflict (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID02_SetDefaultAddress_AlreadyDefault_ThrowsConflict_Abnormal()
    {
        // Arrange
        var alreadyDefaultAddress = CreateAddress(_addressId, _userId, isDefault: true);

        _addressRepoMock
            .Setup(r => r.GetByIdAsync(_userId, _addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(alreadyDefaultAddress);

        // Act
        var act = () => _service.SetDefaultAddressAsync(_userId, _addressId);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID02", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("The default address is already the default address.");

        _addressRepoMock.Verify(r => r.SetDefaultAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID03: Địa chỉ không tồn tại trong hệ thống -> 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID03_SetDefaultAddress_AddressNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        _addressRepoMock
            .Setup(r => r.GetByIdAsync(_userId, _nonExistentAddressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserAddress?)null);

        // Act
        var act = () => _service.SetDefaultAddressAsync(_userId, _nonExistentAddressId);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID03", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Address not found.");

        _addressRepoMock.Verify(r => r.SetDefaultAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID04: Địa chỉ thuộc sở hữu của người dùng khác -> 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID04_SetDefaultAddress_OtherUserAddress_ThrowsNotFound_Abnormal()
    {
        // Arrange
        _addressRepoMock
            .Setup(r => r.GetByIdAsync(_userId, _otherUserAddressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserAddress?)null);

        // Act
        var act = () => _service.SetDefaultAddressAsync(_userId, _otherUserAddressId);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID04", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Address not found.");

        _addressRepoMock.Verify(r => r.SetDefaultAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
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

    private void PrintException(string tcid, AppException ex)
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine($"STATUS: {(int)ex.StatusCode} {ex.StatusCode}");
        _output.WriteLine($"ERROR CODE: {ex.ErrorCode}");
        _output.WriteLine($"MESSAGE: {ex.Message}");
        _output.WriteLine("========================================");
    }
}
