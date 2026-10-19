using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.IntegrationTests.Infrastructure;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.IntegrationTests.Features.Authentication;

public class AuthIntegrationTests : BaseIntegrationTest
{
    private readonly ITestOutputHelper _output;

    public AuthIntegrationTests(
        CustomWebApplicationFactory factory,
        ITestOutputHelper output) : base(factory)
    {
        _output = output;
    }

    [Fact]
    public async Task GetMe_WithoutToken_ReturnsUnauthorized_401()
    {
        // 1. Act: Gọi GET /api/auth/me không có Bearer token
        var response = await Client.GetAsync("/api/auth/me");
        var responseBody = await response.Content.ReadAsStringAsync();

        _output.WriteLine("[TEST LOG] URL: GET /api/auth/me (No Token)");
        _output.WriteLine($"[TEST LOG] HTTP Status nhận được: {response.StatusCode} ({(int)response.StatusCode})");
        _output.WriteLine($"[TEST LOG] Response Body: {responseBody}");

        // 2. Assert: Phải bị từ chối 401
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_WithValidToken_ReturnsCurrentUserProfile_200()
    {
        // 1. Arrange: Tạo User trong DB và đăng nhập
        var user = await AuthenticateAsCustomerAsync(
            email: "hoangnam@savebite.com",
            fullName: "Tran Hoang Nam");

        // 2. Act: Gọi GET /api/auth/me
        var response = await Client.GetAsync("/api/auth/me");
        var responseBody = await response.Content.ReadAsStringAsync();

        _output.WriteLine("[TEST LOG] URL: GET /api/auth/me (With Valid Token)");
        _output.WriteLine($"[TEST LOG] HTTP Status nhận được: {response.StatusCode} ({(int)response.StatusCode})");
        _output.WriteLine($"[TEST LOG] Response Body: {responseBody}");

        // 3. Assert: 200 OK và trả về thông tin đúng user đó
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CurrentUserResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(user.Id);
        result.Data.Email.Should().Be("hoangnam@savebite.com");
        result.Data.FullName.Should().Be("Tran Hoang Nam");
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokensAndSavesRefreshTokenToDatabase_200()
    {
        // 1. Arrange: Tạo user có mật khẩu băm chuẩn BCrypt và đã verify email
        var rawPassword = "Password@123";
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword, workFactor: 10);
        var email = "login.success@savebite.com";

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Phone = "0909999991",
            FullName = "Nguyen Van A",
            PasswordHash = passwordHash,
            EmailVerifiedAt = DateTime.UtcNow,
            Role = UserRole.User,
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            ShopStatus = ShopAccessStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = rawPassword
        };

        // 2. Act: Gửi POST /api/auth/sessions (Đăng nhập)
        var response = await Client.PostAsJsonAsync("/api/auth/sessions", loginRequest);
        var responseBody = await response.Content.ReadAsStringAsync();

        _output.WriteLine("[TEST LOG] URL: POST /api/auth/sessions (Valid Credentials)");
        _output.WriteLine($"[TEST LOG] HTTP Status nhận được: {response.StatusCode} ({(int)response.StatusCode})");
        _output.WriteLine($"[TEST LOG] Response Body: {responseBody}");

        // 3. Assert: 200 OK và có AccessToken, RefreshToken
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<TokenPairResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Data.RefreshToken.Should().NotBeNullOrWhiteSpace();

        // 4. Assert: Kiểm tra Database thật bảng RefreshTokens đã lưu phiên đăng nhập này chưa
        DbContext.ChangeTracker.Clear();
        var savedRefreshToken = await DbContext.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == user.Id);

        savedRefreshToken.Should().NotBeNull();
        savedRefreshToken!.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_WithIncorrectPassword_ReturnsUnauthorized_401()
    {
        // 1. Arrange: Tạo user có mật khẩu "Password@123"
        var email = "wrong.pass@savebite.com";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Phone = "0909999992",
            FullName = "Le Thi B",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password@123", workFactor: 10),
            EmailVerifiedAt = DateTime.UtcNow,
            Role = UserRole.User,
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            ShopStatus = ShopAccessStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        // 2. Act: Gửi sai mật khẩu
        var response = await Client.PostAsJsonAsync("/api/auth/sessions", new LoginRequest
        {
            Email = email,
            Password = "WrongPassword@999"
        });
        var responseBody = await response.Content.ReadAsStringAsync();

        _output.WriteLine("[TEST LOG] URL: POST /api/auth/sessions (Wrong Password)");
        _output.WriteLine($"[TEST LOG] HTTP Status nhận được: {response.StatusCode} ({(int)response.StatusCode})");
        _output.WriteLine($"[TEST LOG] Response Body: {responseBody}");

        // 3. Assert: Phải bị 401 Unauthorized
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WhenEmailNotVerified_ReturnsUnauthorized_401()
    {
        // 1. Arrange: Tạo user nhưng CHƯA verify email (EmailVerifiedAt = null)
        var email = "unverified@savebite.com";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Phone = "0909999993",
            FullName = "Tran Van C",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password@123", workFactor: 10),
            EmailVerifiedAt = null, // Chưa xác thực email
            Role = UserRole.User,
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            ShopStatus = ShopAccessStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        // 2. Act: Đăng nhập
        var response = await Client.PostAsJsonAsync("/api/auth/sessions", new LoginRequest
        {
            Email = email,
            Password = "Password@123"
        });
        var responseBody = await response.Content.ReadAsStringAsync();

        _output.WriteLine("[TEST LOG] URL: POST /api/auth/sessions (Unverified Email)");
        _output.WriteLine($"[TEST LOG] HTTP Status nhận được: {response.StatusCode} ({(int)response.StatusCode})");
        _output.WriteLine($"[TEST LOG] Response Body: {responseBody}");

        // 3. Assert: Bị 401 Unauthorized
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WhenEmailIsEmpty_ReturnsBadRequest_400()
    {
        // 1. Act: Test FluentValidation chặn dữ liệu rỗng ở cửa ngõ
        var response = await Client.PostAsJsonAsync("/api/auth/sessions", new LoginRequest
        {
            Email = "", // Email rỗng
            Password = "Password@123"
        });
        var responseBody = await response.Content.ReadAsStringAsync();

        _output.WriteLine("[TEST LOG] URL: POST /api/auth/sessions (Empty Email)");
        _output.WriteLine($"[TEST LOG] HTTP Status nhận được: {response.StatusCode} ({(int)response.StatusCode})");
        _output.WriteLine($"[TEST LOG] Response Body: {responseBody}");

        // 2. Assert: Bị 400 Bad Request
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangePassword_WithCorrectCurrentPassword_ReturnsOkAndUpdatesHash_200()
    {
        // 1. Arrange: Tạo user có mật khẩu hiện tại là "OldPassword@123"
        var oldPassword = "OldPassword@123";
        var newPassword = "NewPassword@456";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "changepass.user@savebite.com",
            Phone = "0909999994",
            FullName = "Pham Van D",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(oldPassword, workFactor: 10),
            EmailVerifiedAt = DateTime.UtcNow,
            Role = UserRole.User,
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            ShopStatus = ShopAccessStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        // Gắn Bearer Token của user này
        SetBearerToken(user.Id, user.Email, user.Role);

        var request = new ChangePasswordRequest
        {
            CurrentPassword = oldPassword,
            NewPassword = newPassword
        };

        // 2. Act: Gửi PUT /api/auth/me/password
        var response = await Client.PutAsJsonAsync("/api/auth/me/password", request);
        var responseBody = await response.Content.ReadAsStringAsync();

        _output.WriteLine("[TEST LOG] URL: PUT /api/auth/me/password (Correct Current Password)");
        _output.WriteLine($"[TEST LOG] HTTP Status nhận được: {response.StatusCode} ({(int)response.StatusCode})");
        _output.WriteLine($"[TEST LOG] Response Body: {responseBody}");

        // 3. Assert: Status = 200 OK
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Assert: Kiểm tra Database thật xem mật khẩu mới đã băm và khớp chưa
        DbContext.ChangeTracker.Clear();
        var updatedUser = await DbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id);
        updatedUser.Should().NotBeNull();
        BCrypt.Net.BCrypt.Verify(newPassword, updatedUser!.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task ChangePassword_WithIncorrectCurrentPassword_ReturnsBadRequest_400()
    {
        // 1. Arrange: User có password là "RealPassword@123"
        var realPassword = "RealPassword@123";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"user_{Guid.NewGuid():N}@savebite.com",
            Phone = "0909999995",
            FullName = "Test Wrong Current Password",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(realPassword, workFactor: 10),
            EmailVerifiedAt = DateTime.UtcNow,
            Role = UserRole.User,
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            ShopStatus = ShopAccessStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        SetBearerToken(user.Id, user.Email, user.Role);

        // Gửi sai mật khẩu hiện tại
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "TotallyWrongPassword@999",
            NewPassword = "BrandNewPassword@456"
        };

        // 2. Act
        var response = await Client.PutAsJsonAsync("/api/auth/me/password", request);
        var responseBody = await response.Content.ReadAsStringAsync();

        _output.WriteLine("[TEST LOG] URL: PUT /api/auth/me/password (Incorrect Current Password)");
        _output.WriteLine($"[TEST LOG] HTTP Status nhận được: {response.StatusCode} ({(int)response.StatusCode})");
        _output.WriteLine($"[TEST LOG] Response Body: {responseBody}");

        // 3. Assert: Service ném BadRequest("Current password is incorrect.") -> 400
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 4. Assert: DB không bị thay đổi, mật khẩu cũ vẫn dùng được
        DbContext.ChangeTracker.Clear();
        var unchangedUser = await DbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id);
        unchangedUser.Should().NotBeNull();
        BCrypt.Net.BCrypt.Verify(realPassword, unchangedUser!.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task ChangePassword_WhenUnauthenticated_ReturnsUnauthorized_401()
    {
        // 1. Arrange: Không gắn token
        var request = new ChangePasswordRequest
        {
            CurrentPassword = "AnyPassword@123",
            NewPassword = "NewPassword@456"
        };

        // 2. Act
        var response = await Client.PutAsJsonAsync("/api/auth/me/password", request);

        // 3. Assert: 401 Unauthorized
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

