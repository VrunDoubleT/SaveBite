using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.UserManagement;

/// <summary>
/// Unit Tests for Feature: View Users
/// Service: UserService.GetUsersAsync
/// Function Code: UT-32
/// Sheet Name: UT_ViewUsers
/// </summary>
public class ViewUsersTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock = new();
    private readonly Mock<ILogger<UserService>> _loggerMock = new();
    private readonly Mock<ICloudinaryService> _cloudinaryServiceMock = new();
    private readonly UserService _service;

    public ViewUsersTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new UserService(
            _userRepoMock.Object,
            _refreshTokenServiceMock.Object,
            TimeProvider.System,
            _loggerMock.Object,
            _cloudinaryServiceMock.Object
        );
    }

    private List<User> CreateSampleUsers()
    {
        return new List<User>
        {
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Email = "nguyen.an@gmail.com",
                FullName = "Nguyen Van An",
                Role = UserRole.User,
                Status = UserStatus.Active,
                CustomerStatus = CustomerStatus.Active,
                CreatedAt = DateTime.UtcNow.AddMonths(-2)
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Email = "bakery.owner@savebite.vn",
                FullName = "Tran Thi Bakery",
                Role = UserRole.StoreOwner,
                Status = UserStatus.Active,
                CustomerStatus = CustomerStatus.Active,
                CreatedAt = DateTime.UtcNow.AddMonths(-1)
            }
        };
    }

    // =========================================================================
    // TCID01: Danh sách người dùng có dữ liệu trong DB (Has items) -> Trả về danh sách (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID01_GetUsers_HasItems_ReturnsUsersList_Normal()
    {
        // Arrange
        var request = new GetUsersRequest();
        var users = CreateSampleUsers();
        var pagedResult = PagedResult<User>.Create(users, 1, 10, users.Count);

        _userRepoMock
            .Setup(r => r.GetPagedUsersAsync(It.IsAny<GetUsersRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _service.GetUsersAsync(request);

        // Assert & Log
        PrintSuccess("TCID01", result, "200 OK");
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.First().Email.Should().Be("nguyen.an@gmail.com");
        result.Items.First().Role.Should().Be(UserRole.User.ToString());
        result.Items.First().Status.Should().Be(UserStatus.Active.ToString());

        _userRepoMock.Verify(r => r.GetPagedUsersAsync(It.IsAny<GetUsersRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // TCID02: Danh sách người dùng không có dữ liệu (Empty) -> Trả về danh sách rỗng (Normal - N)
    // =========================================================================
    [Fact]
    public async Task TCID02_GetUsers_Empty_ReturnsEmptyList_Normal()
    {
        // Arrange
        var request = new GetUsersRequest();
        var pagedResult = PagedResult<User>.Create(new List<User>(), 1, 10, 0);

        _userRepoMock
            .Setup(r => r.GetPagedUsersAsync(It.IsAny<GetUsersRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _service.GetUsersAsync(request);

        // Assert & Log
        PrintSuccess("TCID02", result, "200 OK");
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(0);
        result.Items.Should().BeEmpty();

        _userRepoMock.Verify(r => r.GetPagedUsersAsync(It.IsAny<GetUsersRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================
    // Helper Methods for Formatting Log Output
    // =========================================================
    private void PrintSuccess<T>(string tcid, T result, string status = "200 OK")
    {
        _output.WriteLine($"================ {tcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine($"STATUS: {status}");
        _output.WriteLine("RESPONSE:");
        _output.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        _output.WriteLine("========================================");
    }
}
