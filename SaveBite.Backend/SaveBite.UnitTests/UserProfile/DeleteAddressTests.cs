using System.Net;
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
/// Unit Tests for Feature: Delete Address
/// Service: UserAddressService.DeleteAddressAsync
/// Function Code: UT-21
/// Sheet Name: UT_DeleteAddress
/// </summary>
public class DeleteAddressTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IUserAddressRepository> _addressRepoMock = new();
    private readonly UserAddressService _service;

    // Fixed Test Identifiers
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _secondaryAddressId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _defaultAddressId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private readonly Guid _nonExistentAddressId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private readonly Guid _otherUserAddressId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    public DeleteAddressTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new UserAddressService(_addressRepoMock.Object);
    }

    private UserAddress CreateAddress(Guid id, Guid userId, bool isDefault)
    {
        return new UserAddress
        {
            Id = id,
            UserId = userId,
            Label = isDefault ? "Home" : "Office",
            AddressLine = "123 Le Loi Street",
            Ward = "Ben Nghe",
            District = "District 1",
            City = "Ho Chi Minh City",
            Latitude = 10.7769,
            Longitude = 106.7009,
            IsDefault = isDefault
        };
    }

    // =========================================================================
    // UTCID01: Xóa địa chỉ phụ thành công (IsDefault = false) -> Thành công (Normal - N)
    // =========================================================================
    [Fact]
    public async Task UTCID01_DeleteAddress_SecondaryAddress_ShouldSucceed_Normal()
    {
        // Arrange
        var secondaryAddress = CreateAddress(_secondaryAddressId, _userId, isDefault: false);
        _addressRepoMock
            .Setup(r => r.GetByIdAsync(_userId, _secondaryAddressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(secondaryAddress);

        _addressRepoMock
            .Setup(r => r.DeleteAsync(secondaryAddress, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteAddressAsync(_userId, _secondaryAddressId);

        // Assert & Log
        PrintSuccess("UTCID01", "Address deleted successfully.");
        _addressRepoMock.Verify(r => r.DeleteAsync(secondaryAddress, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID02: Cố gắng xóa địa chỉ mặc định (IsDefault = true) -> 409 Conflict (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID02_DeleteAddress_DefaultAddress_ThrowsConflict_Abnormal()
    {
        // Arrange
        var defaultAddress = CreateAddress(_defaultAddressId, _userId, isDefault: true);
        _addressRepoMock
            .Setup(r => r.GetByIdAsync(_userId, _defaultAddressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultAddress);

        // Act
        var act = () => _service.DeleteAddressAsync(_userId, _defaultAddressId);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID02", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.Which.Message.Should().Be("The default address cannot be deleted.");

        _addressRepoMock.Verify(r => r.DeleteAsync(It.IsAny<UserAddress>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // UTCID03: Địa chỉ không tồn tại trong DB -> 404 NotFound (Abnormal - A)
    // =========================================================================
    [Fact]
    public async Task UTCID03_DeleteAddress_AddressNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange
        _addressRepoMock
            .Setup(r => r.GetByIdAsync(_userId, _nonExistentAddressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserAddress?)null);

        // Act
        var act = () => _service.DeleteAddressAsync(_userId, _nonExistentAddressId);

        // Assert & Log
        var ex = await act.Should().ThrowAsync<AppException>();
        PrintException("UTCID03", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
        ex.Which.Message.Should().Be("Address not found.");

        _addressRepoMock.Verify(r => r.DeleteAsync(It.IsAny<UserAddress>(), It.IsAny<CancellationToken>()), Times.Never);
    }

   
    private void PrintSuccess(string tcid, string message)
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 200 OK");
        _output.WriteLine($"MESSAGE: {message}");
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
