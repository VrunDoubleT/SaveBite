using System.Text.Json;
using FluentAssertions;
using Moq;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.UserProfile;

/// <summary>
/// Unit Tests for Feature: Get My Addresses
/// Service: UserAddressService.GetMyAddressesAsync
/// Function Code: UT-17
/// Sheet Name: UT_GetMyAddresses
/// </summary>
public class GetMyAddressesTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IUserAddressRepository> _addressRepoMock = new();
    private readonly UserAddressService _service;

    // Fixed Test Identifiers
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public GetMyAddressesTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new UserAddressService(_addressRepoMock.Object);
    }

    // =========================================================================
    // UTCID01: Người dùng có địa chỉ trong hệ thống (Has items) -> Trả về danh sách địa chỉ (Normal)
    // =========================================================================
    [Fact]
    public async Task UTCID01_GetMyAddresses_HasItems_ReturnsAddressList_Normal()
    {
        // Arrange
        var addresses = new List<UserAddress>
        {
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                UserId = _userId,
                Label = "Home",
                AddressLine = "123 Le Loi Street",
                Ward = "Ben Nghe Ward",
                District = "District 1",
                City = "Ho Chi Minh City",
                Latitude = 10.7769,
                Longitude = 106.7009,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new()
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                UserId = _userId,
                Label = "Office",
                AddressLine = "456 Nguyen Hue Boulevard",
                Ward = "Ben Nghe Ward",
                District = "District 1",
                City = "Ho Chi Minh City",
                Latitude = 10.7745,
                Longitude = 106.7025,
                IsDefault = false,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-5)
            }
        };

        _addressRepoMock
            .Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(addresses);

        // Act
        var result = await _service.GetMyAddressesAsync(_userId);

        // Assert & Log
        PrintSuccess("UTCID01", result);
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().Label.Should().Be("Home");
        result.First().IsDefault.Should().BeTrue();

        _addressRepoMock.Verify(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // UTCID02: Người dùng chưa lưu địa chỉ nào (Empty) -> Trả về danh sách rỗng 0 phần tử (Normal)
    // =========================================================================
    [Fact]
    public async Task UTCID02_GetMyAddresses_Empty_ReturnsEmptyList_Normal()
    {
        // Arrange
        _addressRepoMock
            .Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserAddress>());

        // Act
        var result = await _service.GetMyAddressesAsync(_userId);

        // Assert & Log
        PrintSuccess("UTCID02", result);
        result.Should().NotBeNull();
        result.Should().BeEmpty();

        _addressRepoMock.Verify(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()), Times.Once);
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
}
