using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using Xunit;
using Xunit.Abstractions;

namespace SaveBite.Tests.StaffInvitations;

/// <summary>
/// Feature: Search Staff Candidates
/// </summary>
public class SearchStaffCandidatesTests
{
    private readonly Mock<IStaffInvitationRepository> _invitationRepoMock = new();
    private readonly Mock<IShopStaffRepository> _staffRepoMock = new();
    private readonly StaffInvitationService _service;
    private readonly ITestOutputHelper _output;

    // Fixed Identifiers để dễ đối chiếu với Bảng Ma trận Excel
    private readonly Guid _ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _shopId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Shop _ownerShop;

    public SearchStaffCandidatesTests(ITestOutputHelper output)
    {
        _output = output;
        _service = new StaffInvitationService(_invitationRepoMock.Object, _staffRepoMock.Object);

        _ownerShop = new Shop
        {
            Id = _shopId,
            OwnerUserId = _ownerId,
            Name = "SaveBite Bakery Central",
            Status = ShopStatus.Active
        };
    }

    [Fact]
    public async Task UT01_SearchStaffCandidates_WithKeyword_ReturnsMatchingCandidates_Normal()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Nguyen Van A",
            Email = "candidate.a@savebite.vn"
        };
        var pagedResult = PagedResult<User>.Create(new List<User> { user }, 1, 10, 1);

        _invitationRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_ownerShop);

        _invitationRepoMock
            .Setup(r => r.SearchStaffCandidatesAsync(_shopId, _ownerId, "Nguyen", 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<User>)new List<User> { user }, 1));

        // Act
        var result = await _service.SearchStaffCandidatesAsync(_ownerId, keyword: " Nguyen ", page: 1, pageSize: 10);

        // Output & Assert
        PrintSuccess("UT01", result);
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(1);
        result.Items.First().FullName.Should().Be("Nguyen Van A");
        result.Items.First().Email.Should().Be("candidate.a@savebite.vn");
    }

    [Fact]
    public async Task UT02_SearchStaffCandidates_NullKeyword_ReturnsAllCandidates_Normal()
    {
        // Arrange
        _invitationRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_ownerShop);

        _invitationRepoMock
            .Setup(r => r.SearchStaffCandidatesAsync(_shopId, _ownerId, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<User>)new List<User>(), 0));

        // Act
        var result = await _service.SearchStaffCandidatesAsync(_ownerId, keyword: null, page: 1, pageSize: 10);

        // Output & Assert
        PrintSuccess("UT02", result);
        result.Should().NotBeNull();
        result.TotalItems.Should().Be(0);
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task UT03_SearchStaffCandidates_OwnerShopNotFound_ThrowsNotFound_Abnormal()
    {
        // Arrange: Chủ quán chưa đăng ký shop (shop == null)
        _invitationRepoMock
            .Setup(r => r.GetShopByOwnerUserIdAsync(_ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Shop?)null);

        // Act & Assert
        var act = () => _service.SearchStaffCandidatesAsync(_ownerId, keyword: "test", page: 1, pageSize: 10);
        var ex = await act.Should().ThrowAsync<AppException>();

        PrintException("UT03", ex.Which);
        ex.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private void PrintSuccess<T>(string utcid, T result)
    {
        _output.WriteLine($"================ {utcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine("STATUS: 200 OK");
        _output.WriteLine("RESPONSE:");
        _output.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        _output.WriteLine("========================================");
    }

    private void PrintException(string utcid, AppException ex)
    {
        _output.WriteLine($"================ {utcid} ================");
        _output.WriteLine("RESULT: PASS");
        _output.WriteLine($"STATUS: {(int)ex.StatusCode} {ex.StatusCode}");
        _output.WriteLine($"MESSAGE: {ex.Message}");
        _output.WriteLine("========================================");
    }
}
