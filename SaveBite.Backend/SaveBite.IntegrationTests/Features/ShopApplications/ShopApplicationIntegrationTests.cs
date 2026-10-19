using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.IntegrationTests.Infrastructure;
using Xunit;

namespace SaveBite.IntegrationTests.Features.ShopApplications;

public class ShopApplicationIntegrationTests : BaseIntegrationTest
{
    public ShopApplicationIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetMyShopApplication_WhenUnauthenticated_ReturnsUnauthorized_401()
    {
        // Act
        var response = await Client.GetAsync("/api/shop-applications/me/latest");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyShopApplication_WhenNoApplication_ReturnsNotFound_404()
    {
        // Arrange
        await AuthenticateAsCustomerAsync();

        // Act
        var response = await Client.GetAsync("/api/shop-applications/me/latest");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WhenValidMultipartFormData_CreatesPendingApplication_200()
    {
        // Arrange
        var customer = await AuthenticateAsCustomerAsync();

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("Tiệm Bánh Mì Ngon"), "Name");
        form.Add(new StringContent("Chuyên các loại bánh mì"), "Description");
        form.Add(new StringContent("0123456789"), "BusinessLicenseNo");
        form.Add(new StringContent("123 Phố Huế"), "AddressLine");
        form.Add(new StringContent("Hàng Bài"), "Ward");
        form.Add(new StringContent("Hoàn Kiếm"), "District");
        form.Add(new StringContent("Hà Nội"), "City");
        form.Add(new StringContent("21.0285"), "Latitude");
        form.Add(new StringContent("105.8542"), "Longitude");
        form.Add(new StringContent("Vietcombank"), "BankName");
        form.Add(new StringContent("001100223344"), "BankAccountNumber");
        form.Add(new StringContent(customer.FullName), "BankAccountHolder");
        form.Add(new StringContent("test-payos-client-id"), "PayosClientId");
        form.Add(new StringContent("test-payos-api-key"), "PayosApiKey");
        form.Add(new StringContent("test-payos-checksum"), "PayosChecksumKey");

        // Giả lập file ảnh logo hợp lệ
        var logoContent = new ByteArrayContent(Encoding.UTF8.GetBytes("fake_logo_bytes"));
        logoContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        form.Add(logoContent, "logo", "logo.jpg");

        // Act
        var response = await Client.PostAsync("/api/shop-applications", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ShopApplicationResponse>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Name.Should().Be("Tiệm Bánh Mì Ngon");
        body.Data.Status.Should().Be(ShopApplicationStatus.Pending.ToString());

        // Verify Database
        DbContext.ChangeTracker.Clear();
        var appInDb = await DbContext.ShopApplications.AsNoTracking().FirstOrDefaultAsync(a => a.ApplicantUserId == customer.Id);
        appInDb.Should().NotBeNull();
        appInDb!.Status.Should().Be(ShopApplicationStatus.Pending);
    }

    [Fact]
    public async Task Create_WhenCustomerAlreadyHasPendingApplication_ReturnsConflict_409()
    {
        // Arrange
        var customer = await AuthenticateAsCustomerAsync();

        var existingPendingApp = new ShopApplication
        {
            Id = Guid.NewGuid(),
            ApplicantUserId = customer.Id,
            Name = "Đơn Đang Chờ Duyệt",
            AddressLine = "123 Đang Chờ",
            BankName = "Bank",
            BankAccountNumber = "123",
            BankAccountHolder = "Holder",
            PayosClientId = "cid",
            PayosApiKey = "key",
            PayosChecksumKey = "checksum",
            Status = ShopApplicationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.ShopApplications.Add(existingPendingApp);
        await DbContext.SaveChangesAsync();

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("Đơn Nộp Thêm Lần 2"), "Name");
        form.Add(new StringContent("123 Phố Huế"), "AddressLine");
        form.Add(new StringContent("Bank"), "BankName");
        form.Add(new StringContent("123"), "BankAccountNumber");
        form.Add(new StringContent("Holder"), "BankAccountHolder");
        form.Add(new StringContent("cid"), "PayosClientId");
        form.Add(new StringContent("key"), "PayosApiKey");
        form.Add(new StringContent("checksum"), "PayosChecksumKey");

        var logoContent = new ByteArrayContent(Encoding.UTF8.GetBytes("fake_logo"));
        logoContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        form.Add(logoContent, "logo", "logo.jpg");

        // Act: Đã có 1 đơn pending -> Không thể tạo thêm đơn nữa
        var response = await Client.PostAsync("/api/shop-applications", form);

        // Assert: 409 Conflict ("You already have an active shop application.")
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Cancel_WhenPendingApplicationExists_CancelsApplication_200()
    {
        // Arrange
        var customer = await AuthenticateAsCustomerAsync();

        var pendingApp = new ShopApplication
        {
            Id = Guid.NewGuid(),
            ApplicantUserId = customer.Id,
            Name = "Đơn Muốn Hủy",
            AddressLine = "123 Hủy",
            BankName = "Bank",
            BankAccountNumber = "123",
            BankAccountHolder = "Holder",
            PayosClientId = "cid",
            PayosApiKey = "key",
            PayosChecksumKey = "checksum",
            Status = ShopApplicationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.ShopApplications.Add(pendingApp);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.PostAsync($"/api/shop-applications/{pendingApp.Id}/cancellations", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify Database: Trạng thái thành Cancelled
        DbContext.ChangeTracker.Clear();
        var appInDb = await DbContext.ShopApplications.AsNoTracking().FirstOrDefaultAsync(a => a.Id == pendingApp.Id);
        appInDb.Should().NotBeNull();
        appInDb!.Status.Should().Be(ShopApplicationStatus.Cancelled);
    }

    [Fact]
    public async Task GetAll_WhenCustomerCallsAdminEndpoint_ReturnsForbidden_403()
    {
        // Arrange
        await AuthenticateAsCustomerAsync();

        // Act
        var response = await Client.GetAsync("/api/shop-applications");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAll_WhenAdminCalls_ReturnsApplicationList_200()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/shop-applications");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<ShopApplicationResponse>>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task Review_WhenAdminApproves_UpgradesUserToStoreOwnerAndCreatesShop_200()
    {
        // Arrange
        var admin = await AuthenticateAsAdminAsync();

        var customer = new User
        {
            Id = Guid.NewGuid(),
            Email = $"applicant_{Guid.NewGuid():N}@test.com",
            FullName = "Nguyen Applicant",
            PasswordHash = "dummyhash",
            Role = UserRole.User,
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            ShopStatus = ShopAccessStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Users.Add(customer);

        var pendingApp = new ShopApplication
        {
            Id = Guid.NewGuid(),
            ApplicantUserId = customer.Id,
            Name = "Cửa Hàng Được Duyệt",
            AddressLine = "789 Phố Lớn",
            BankName = "MBBank",
            BankAccountNumber = "999888777",
            BankAccountHolder = "Nguyen Applicant",
            PayosClientId = "payos_cid",
            PayosApiKey = "payos_key",
            PayosChecksumKey = "payos_checksum",
            Status = ShopApplicationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.ShopApplications.Add(pendingApp);
        await DbContext.SaveChangesAsync();

        var reviewRequest = new ReviewShopApplicationRequest
        {
            Decision = "Approved",
            Note = "Hồ sơ kinh doanh đầy đủ và hợp lệ."
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/shop-applications/{pendingApp.Id}/reviews", reviewRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify Database:
        DbContext.ChangeTracker.Clear();
        // 1. Application chuyển thành Approved
        var updatedApp = await DbContext.ShopApplications.AsNoTracking().FirstOrDefaultAsync(a => a.Id == pendingApp.Id);
        updatedApp.Should().NotBeNull();
        updatedApp!.Status.Should().Be(ShopApplicationStatus.Approved);

        // 2. User được nâng role thành StoreOwner
        var updatedUser = await DbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == customer.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.Role.Should().Be(UserRole.StoreOwner);

        // 3. Shop mới được tự động tạo trong DB
        var createdShop = await DbContext.Shops.AsNoTracking().FirstOrDefaultAsync(s => s.ApplicationId == pendingApp.Id);
        createdShop.Should().NotBeNull();
        createdShop!.OwnerUserId.Should().Be(customer.Id);
        createdShop.Name.Should().Be("Cửa Hàng Được Duyệt");
    }

    [Fact]
    public async Task Review_WhenAdminRejectsWithoutNote_ReturnsBadRequest_400()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        var applicantId = Guid.NewGuid();
        var applicant = new User
        {
            Id = applicantId,
            Email = $"reject_applicant_{Guid.NewGuid():N}@test.com",
            FullName = "Reject Applicant",
            PasswordHash = "dummyhash",
            Role = UserRole.User,
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.Users.Add(applicant);

        var pendingApp = new ShopApplication
        {
            Id = Guid.NewGuid(),
            ApplicantUserId = applicantId,
            Name = "Đơn Sắp Bị Từ Chối",
            AddressLine = "123 Test",
            BankName = "Bank",
            BankAccountNumber = "123",
            BankAccountHolder = "Holder",
            PayosClientId = "cid",
            PayosApiKey = "key",
            PayosChecksumKey = "checksum",
            Status = ShopApplicationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        DbContext.ShopApplications.Add(pendingApp);
        await DbContext.SaveChangesAsync();

        // Từ chối nhưng KHÔNG ghi lý do
        var invalidReview = new ReviewShopApplicationRequest
        {
            Decision = "Rejected",
            Note = "" // Rỗng
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/shop-applications/{pendingApp.Id}/reviews", invalidReview);

        // Assert: 400 Bad Request ("A rejection reason is required.")
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
