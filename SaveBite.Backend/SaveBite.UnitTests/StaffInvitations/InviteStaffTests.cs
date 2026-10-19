using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

using ShopStaffEntity = SaveBite.Backend.Models.Entities.ShopStaff;

namespace SaveBite.Tests.StaffInvitations;

/// <summary>
/// Unit Tests for Feature: Invite Customer as Staff
/// </summary>
public class InviteStaffTests
{
    private readonly Mock<IStaffInvitationRepository> _invitationRepoMock;
    private readonly Mock<IShopStaffRepository> _staffRepoMock;
    private readonly StaffInvitationService _service;
    private readonly ITestOutputHelper _output;

    // Fixed Identifiers for predictable, readable testing
    private readonly Guid _ownerId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly Guid _shopId =
        Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly Guid _validCandidateId =
        Guid.Parse("33333333-3333-3333-3333-333333333333");

    private readonly Guid _adminCandidateId =
        Guid.Parse("44444444-4444-4444-4444-444444444444");

    private readonly Guid _otherOwnerCandidateId =
        Guid.Parse("55555555-5555-5555-5555-555555555555");

    private readonly Guid _suspendedCandidateId =
        Guid.Parse("66666666-6666-6666-6666-666666666666");

    private readonly Guid _existingStaffCandidateId =
        Guid.Parse("77777777-7777-7777-7777-777777777777");

    private readonly Shop _ownerShop;

    public InviteStaffTests(ITestOutputHelper output)
    {
        _output = output;

        _invitationRepoMock =
            new Mock<IStaffInvitationRepository>();

        _staffRepoMock =
            new Mock<IShopStaffRepository>();

        _service = new StaffInvitationService(
            _invitationRepoMock.Object,
            _staffRepoMock.Object
        );

        // Precondition: Active shop owned by store owner
        _ownerShop = new Shop
        {
            Id = _shopId,
            OwnerUserId = _ownerId,
            Name = "SaveBite Bakery Central",
            Status = ShopStatus.Active
        };

        var mockTransaction =
            new Mock<IDbContextTransaction>();

        _staffRepoMock
            .Setup(r => r.BeginTransactionAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockTransaction.Object);

        _invitationRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(
                _ownerId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(_ownerShop);
    }

    // =========================================================
    // Helper methods
    // =========================================================

    private void PrintSuccess<T>(
        string tcid,
        T result,
        string status = "200 OK")
    {
        _output.WriteLine(
            $"================ {tcid} ================");

        _output.WriteLine("RESULT: PASS");
        _output.WriteLine($"STATUS: {status}");
        _output.WriteLine("RESPONSE:");

        _output.WriteLine(
            JsonSerializer.Serialize(
                result,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }
            )
        );

        _output.WriteLine(
            "========================================");
    }

    private void PrintException(
        string tcid,
        AppException ex)
    {
        _output.WriteLine(
            $"================ {tcid} ================");

        _output.WriteLine("RESULT: PASS");

        _output.WriteLine(
            $"STATUS: {(int)ex.StatusCode} {ex.StatusCode}");

        _output.WriteLine(
            $"ERROR CODE: {ex.ErrorCode}");

        _output.WriteLine(
            $"MESSAGE: {ex.Message}");

        _output.WriteLine(
            "========================================");
    }

    private void PrintGeneralException(
        string tcid,
        Exception ex)
    {
        _output.WriteLine(
            $"================ {tcid} ================");

        _output.WriteLine("RESULT: PASS");

        _output.WriteLine(
            $"EXCEPTION TYPE: {ex.GetType().Name}");

        _output.WriteLine(
            $"MESSAGE: {ex.Message}");

        _output.WriteLine(
            "========================================");
    }

    // =========================================================
    // TCID01
    // =========================================================

    [Fact]
    public async Task
        TCID01_InviteStaff_ValidCustomer_ReturnsPendingInvitation_Normal()
    {
        // Arrange
        var candidate = new User
        {
            Id = _validCandidateId,
            Email = "customer.valid@savebite.vn",
            FullName = "Nguyen Van Khach",
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            Role = UserRole.User
        };

        var createdInvitation = new StaffInvitation
        {
            Id = Guid.NewGuid(),
            ShopId = _shopId,
            Shop = _ownerShop,
            InvitedUserId = _validCandidateId,
            InvitedUser = candidate,
            InvitedBy = _ownerId,
            Status =
                nameof(StaffInvitationStatus.Pending),
            InvitedAt = DateTime.UtcNow
        };

        _invitationRepoMock
            .Setup(r => r.GetUserByIdAsync(
                _validCandidateId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidate);

        _staffRepoMock
            .Setup(r => r.GetStaffByShopAndUserAsync(
                _shopId,
                _validCandidateId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShopStaffEntity?)null);

        _invitationRepoMock
            .Setup(r => r.GetPendingInvitationAsync(
                _shopId,
                _validCandidateId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((StaffInvitation?)null);

        _invitationRepoMock
            .Setup(r => r.GetInvitationByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdInvitation);

        var request =
            new InviteStaffRequest(_validCandidateId);

        // Act
        var result =
            await _service.InviteStaffAsync(
                _ownerId,
                request);

        // Output
        PrintSuccess(
            "TCID01",
            result);

        // Assert
        Assert.NotNull(result);

        Assert.Equal(
            _shopId,
            result.ShopId);

        Assert.Equal(
            "SaveBite Bakery Central",
            result.ShopName);

        Assert.Equal(
            _validCandidateId,
            result.InvitedUserId);

        Assert.Equal(
            "customer.valid@savebite.vn",
            result.InvitedUserEmail);

        Assert.Equal(
            "Nguyen Van Khach",
            result.InvitedUserName);

        Assert.Equal(
            nameof(StaffInvitationStatus.Pending),
            result.Status);
    }

    // =========================================================
    // TCID02
    // =========================================================

    [Fact]
    public async Task
        TCID02_InviteStaff_CandidateDoesNotExist_ThrowsUserNotFound()
    {
        // Arrange
        var nonExistentId =
            Guid.Parse(
                "99999999-9999-9999-9999-999999999999");

        _invitationRepoMock
            .Setup(r => r.GetUserByIdAsync(
                nonExistentId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var request =
            new InviteStaffRequest(nonExistentId);

        // Act & Assert
        var ex =
            await Assert.ThrowsAsync<AppException>(
                () => _service.InviteStaffAsync(
                    _ownerId,
                    request));

        // Output
        PrintException(
            "TCID02",
            ex);

        Assert.Equal(
            HttpStatusCode.NotFound,
            ex.StatusCode);

        Assert.Equal(
            ErrorCodes.UserNotFound,
            ex.ErrorCode);
    }

    // =========================================================
    // TCID03
    // =========================================================

    [Fact]
    public async Task
        TCID03_InviteStaff_AccountSuspended_ThrowsAccountInactive()
    {
        // Arrange
        var suspendedUser = new User
        {
            Id = _suspendedCandidateId,
            Email = "suspended.account@savebite.vn",
            FullName = "Le Van Suspended",
            Status = UserStatus.Suspended,
            CustomerStatus = CustomerStatus.Active,
            Role = UserRole.User
        };

        _invitationRepoMock
            .Setup(r => r.GetUserByIdAsync(
                _suspendedCandidateId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(suspendedUser);

        var request =
            new InviteStaffRequest(
                _suspendedCandidateId);

        // Act & Assert
        var ex =
            await Assert.ThrowsAsync<AppException>(
                () => _service.InviteStaffAsync(
                    _ownerId,
                    request));

        PrintException(
            "TCID03",
            ex);

        Assert.Equal(
            HttpStatusCode.Conflict,
            ex.StatusCode);

        Assert.Equal(
            ErrorCodes.AccountInactive,
            ex.ErrorCode);
    }

    // =========================================================
    // TCID04
    // =========================================================

    [Fact]
    public async Task
        TCID04_InviteStaff_CustomerAccessSuspended_ThrowsAccountInactive()
    {
        // Arrange
        var inactiveCustomer = new User
        {
            Id = _suspendedCandidateId,
            Email = "inactive.customer@savebite.vn",
            FullName = "Tran Thi Inactive",
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Suspended,
            Role = UserRole.User
        };

        _invitationRepoMock
            .Setup(r => r.GetUserByIdAsync(
                _suspendedCandidateId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveCustomer);

        var request =
            new InviteStaffRequest(
                _suspendedCandidateId);

        // Act & Assert
        var ex =
            await Assert.ThrowsAsync<AppException>(
                () => _service.InviteStaffAsync(
                    _ownerId,
                    request));

        PrintException(
            "TCID04",
            ex);

        Assert.Equal(
            HttpStatusCode.Conflict,
            ex.StatusCode);

        Assert.Equal(
            ErrorCodes.AccountInactive,
            ex.ErrorCode);
    }

    // =========================================================
    // TCID05
    // =========================================================

    [Fact]
    public async Task
        TCID05_InviteStaff_CandidateIsAdmin_ThrowsInvalidStaffCandidate()
    {
        // Arrange
        var adminUser = new User
        {
            Id = _adminCandidateId,
            Email = "dashboard.admin@savebite.vn",
            FullName = "System Administrator",
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            Role = UserRole.Admin
        };

        _invitationRepoMock
            .Setup(r => r.GetUserByIdAsync(
                _adminCandidateId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUser);

        var request =
            new InviteStaffRequest(
                _adminCandidateId);

        // Act & Assert
        var ex =
            await Assert.ThrowsAsync<AppException>(
                () => _service.InviteStaffAsync(
                    _ownerId,
                    request));

        PrintException(
            "TCID05",
            ex);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            ex.StatusCode);

        Assert.Equal(
            ErrorCodes.InvalidStaffCandidate,
            ex.ErrorCode);

        Assert.Equal(
            "Admin accounts cannot be invited as staff",
            ex.Message);
    }

    // =========================================================
    // TCID06
    // =========================================================

    [Fact]
    public async Task
        TCID06_InviteStaff_CandidateIsOtherStoreOwner_ThrowsInvalidStaffCandidate()
    {
        // Arrange
        var otherStoreOwner = new User
        {
            Id = _otherOwnerCandidateId,
            Email = "other.owner@savebite.vn",
            FullName = "Other Store Owner",
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            Role = UserRole.StoreOwner
        };

        _invitationRepoMock
            .Setup(r => r.GetUserByIdAsync(
                _otherOwnerCandidateId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherStoreOwner);

        var request =
            new InviteStaffRequest(
                _otherOwnerCandidateId);

        // Act & Assert
        var ex =
            await Assert.ThrowsAsync<AppException>(
                () => _service.InviteStaffAsync(
                    _ownerId,
                    request));

        PrintException(
            "TCID06",
            ex);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            ex.StatusCode);

        Assert.Equal(
            ErrorCodes.InvalidStaffCandidate,
            ex.ErrorCode);

        Assert.Equal(
            "Store owner accounts cannot be invited as staff",
            ex.Message);
    }

    // =========================================================
    // TCID07
    // =========================================================

    [Fact]
    public async Task
        TCID07_InviteStaff_CandidateIsSelf_ThrowsCannotInviteSelf()
    {
        // Arrange
        var ownerUser = new User
        {
            Id = _ownerId,
            Email = "owner@savebite.vn",
            FullName = "Main Shop Owner",
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            Role = UserRole.StoreOwner
        };

        _invitationRepoMock
            .Setup(r => r.GetUserByIdAsync(
                _ownerId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ownerUser);

        var request =
            new InviteStaffRequest(_ownerId);

        // Act & Assert
        var ex =
            await Assert.ThrowsAsync<AppException>(
                () => _service.InviteStaffAsync(
                    _ownerId,
                    request));

        PrintException(
            "TCID07",
            ex);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            ex.StatusCode);

        Assert.Equal(
            ErrorCodes.CannotInviteSelf,
            ex.ErrorCode);

        Assert.Equal(
            "Cannot invite yourself as staff",
            ex.Message);
    }

    // =========================================================
    // TCID08
    // =========================================================

    [Fact]
    public async Task
        TCID08_InviteStaff_CandidateAlreadyActiveStaff_ThrowsAlreadyActiveStaff()
    {
        // Arrange
        var candidate = new User
        {
            Id = _existingStaffCandidateId,
            Email = "active.staff@savebite.vn",
            FullName = "Pham Thi Staff",
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            Role = UserRole.User
        };

        var existingStaff =
            new ShopStaffEntity
            {
                Id = Guid.NewGuid(),
                ShopId = _shopId,
                UserId = _existingStaffCandidateId,
                DisplayName = "Pham Thi Staff",
                Status = ShopStaffStatus.Active
            };

        _invitationRepoMock
            .Setup(r => r.GetUserByIdAsync(
                _existingStaffCandidateId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidate);

        _staffRepoMock
            .Setup(r => r.GetStaffByShopAndUserAsync(
                _shopId,
                _existingStaffCandidateId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingStaff);

        var request =
            new InviteStaffRequest(
                _existingStaffCandidateId);

        // Act & Assert
        var ex =
            await Assert.ThrowsAsync<AppException>(
                () => _service.InviteStaffAsync(
                    _ownerId,
                    request));

        PrintException(
            "TCID08",
            ex);

        Assert.Equal(
            HttpStatusCode.Conflict,
            ex.StatusCode);

        Assert.Equal(
            ErrorCodes.AlreadyActiveStaff,
            ex.ErrorCode);

        Assert.Equal(
            "User is already an active staff member of this shop",
            ex.Message);
    }

    // =========================================================
    // TCID09
    // =========================================================

    [Fact]
    public async Task
        TCID09_InviteStaff_CandidateSuspendedByShop_ThrowsStaffSuspended()
    {
        // Arrange
        var candidate = new User
        {
            Id = _existingStaffCandidateId,
            Email = "suspended.staff@savebite.vn",
            FullName = "Hoang Van Suspended",
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            Role = UserRole.User
        };

        var suspendedMembership =
            new ShopStaffEntity
            {
                Id = Guid.NewGuid(),
                ShopId = _shopId,
                UserId = _existingStaffCandidateId,
                DisplayName =
                    "Hoang Van Suspended",
                Status =
                    ShopStaffStatus.Suspended
            };

        _invitationRepoMock
            .Setup(r => r.GetUserByIdAsync(
                _existingStaffCandidateId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidate);

        _staffRepoMock
            .Setup(r => r.GetStaffByShopAndUserAsync(
                _shopId,
                _existingStaffCandidateId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(suspendedMembership);

        var request =
            new InviteStaffRequest(
                _existingStaffCandidateId);

        // Act & Assert
        var ex =
            await Assert.ThrowsAsync<AppException>(
                () => _service.InviteStaffAsync(
                    _ownerId,
                    request));

        PrintException(
            "TCID09",
            ex);

        Assert.Equal(
            HttpStatusCode.Conflict,
            ex.StatusCode);

        Assert.Equal(
            ErrorCodes.StaffSuspended,
            ex.ErrorCode);

        Assert.Equal(
            "This user has been suspended from working at this shop",
            ex.Message);
    }

    // =========================================================
    // TCID10
    // =========================================================

    [Fact]
    public async Task
        TCID10_InviteStaff_UnexpiredPendingInvitationExists_ThrowsPendingInvitationExists()
    {
        // Arrange
        var candidate = new User
        {
            Id = _validCandidateId,
            Email = "customer.pending@savebite.vn",
            FullName = "Nguyen Thi Pending",
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            Role = UserRole.User
        };

        var activePendingInvitation =
            new StaffInvitation
            {
                Id = Guid.NewGuid(),
                ShopId = _shopId,
                InvitedUserId =
                    _validCandidateId,
                Status =
                    nameof(
                        StaffInvitationStatus.Pending),
                InvitedAt =
                    DateTime.UtcNow.AddDays(-1)
            };

        _invitationRepoMock
            .Setup(r => r.GetUserByIdAsync(
                _validCandidateId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidate);

        _staffRepoMock
            .Setup(r => r.GetStaffByShopAndUserAsync(
                _shopId,
                _validCandidateId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (ShopStaffEntity?)null);

        _invitationRepoMock
            .Setup(r => r.GetPendingInvitationAsync(
                _shopId,
                _validCandidateId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                activePendingInvitation);

        var request =
            new InviteStaffRequest(
                _validCandidateId);

        // Act & Assert
        var ex =
            await Assert.ThrowsAsync<AppException>(
                () => _service.InviteStaffAsync(
                    _ownerId,
                    request));

        PrintException(
            "TCID10",
            ex);

        Assert.Equal(
            HttpStatusCode.Conflict,
            ex.StatusCode);

        Assert.Equal(
            ErrorCodes.PendingInvitationExists,
            ex.ErrorCode);

        Assert.Equal(
            "A pending invitation already exists for this user",
            ex.Message);
    }

    // =========================================================
    // TCID11
    // =========================================================

    [Fact]
    public async Task
        TCID11_InviteStaff_ExpiredPendingInvitation_CancelsOldAndCreatesNew()
    {
        // Arrange
        var candidate = new User
        {
            Id = _validCandidateId,
            Email = "customer.expired@savebite.vn",
            FullName = "Vu Thi Expired",
            Status = UserStatus.Active,
            CustomerStatus = CustomerStatus.Active,
            Role = UserRole.User
        };

        var expiredInvitation =
            new StaffInvitation
            {
                Id = Guid.NewGuid(),
                ShopId = _shopId,
                InvitedUserId =
                    _validCandidateId,
                Status =
                    nameof(
                        StaffInvitationStatus.Pending),
                InvitedAt =
                    DateTime.UtcNow.AddDays(-5)
            };

        var newlyCreatedInvitation =
            new StaffInvitation
            {
                Id = Guid.NewGuid(),
                ShopId = _shopId,
                Shop = _ownerShop,
                InvitedUserId =
                    _validCandidateId,
                InvitedUser = candidate,
                InvitedBy = _ownerId,
                Status =
                    nameof(
                        StaffInvitationStatus.Pending),
                InvitedAt = DateTime.UtcNow
            };

        _invitationRepoMock
            .Setup(r => r.GetUserByIdAsync(
                _validCandidateId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidate);

        _staffRepoMock
            .Setup(r => r.GetStaffByShopAndUserAsync(
                _shopId,
                _validCandidateId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                (ShopStaffEntity?)null);

        _invitationRepoMock
            .Setup(r => r.GetPendingInvitationAsync(
                _shopId,
                _validCandidateId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                expiredInvitation);

        _invitationRepoMock
            .Setup(r => r.GetInvitationByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                newlyCreatedInvitation);

        var request =
            new InviteStaffRequest(
                _validCandidateId);

        // Act
        var result =
            await _service.InviteStaffAsync(
                _ownerId,
                request);

        // Output
        PrintSuccess(
            "TCID11",
            result);

        _output.WriteLine(
            $"OLD INVITATION STATUS: {expiredInvitation.Status}");

        // Assert
        Assert.NotNull(result);

        Assert.Equal(
            nameof(
                StaffInvitationStatus.Pending),
            result.Status);

        Assert.Equal(
            nameof(
                StaffInvitationStatus.Cancelled),
            expiredInvitation.Status);
    }

    // =========================================================
    // TCID12
    // =========================================================

    [Fact]
    public async Task
        TCID12_InviteStaff_OwnerShopNotFound_ThrowsNotFound()
    {
        // Arrange
        _invitationRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(
                _ownerId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shop?)null);

        var request =
            new InviteStaffRequest(
                _validCandidateId);

        // Act & Assert
        var ex =
            await Assert.ThrowsAsync<AppException>(
                () => _service.InviteStaffAsync(
                    _ownerId,
                    request));

        PrintException(
            "TCID12",
            ex);

        Assert.Equal(
            HttpStatusCode.NotFound,
            ex.StatusCode);
    }

    // =========================================================
    // TCID13
    // =========================================================

    [Fact]
    public async Task
        TCID13_InviteStaff_EmptyGuidUserId_ThrowsUserNotFound_Boundary()
    {
        // Arrange
        _invitationRepoMock
            .Setup(r => r.GetUserByIdAsync(
                Guid.Empty,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var request =
            new InviteStaffRequest(
                Guid.Empty);

        // Act & Assert
        var ex =
            await Assert.ThrowsAsync<AppException>(
                () => _service.InviteStaffAsync(
                    _ownerId,
                    request));

        PrintException(
            "TCID13",
            ex);

        Assert.Equal(
            HttpStatusCode.NotFound,
            ex.StatusCode);

        Assert.Equal(
            ErrorCodes.UserNotFound,
            ex.ErrorCode);
    }

    // =========================================================
    // TCID14
    // =========================================================

    [Fact]
    public async Task
        TCID14_InviteStaff_NullRequest_ThrowsException_Boundary()
    {
        // Arrange
        InviteStaffRequest? request = null;

        // Act & Assert
        var ex =
            await Assert.ThrowsAnyAsync<Exception>(
                () => _service.InviteStaffAsync(
                    _ownerId,
                    request!));

        PrintGeneralException(
            "TCID14",
            ex);
    }
}