using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Controllers;
using System.Reflection;
using Xunit;

namespace SaveBite.Tests.Authentication;

public class AuthorizationAttributeTests
{
    [Fact]
    public void AdminAuditLogsController_MustBeDecoratedWith_AdminAccessAttribute()
    {
        typeof(AdminAuditLogsController)
            .Should().BeDecoratedWith<AdminAccessAttribute>(
                "Access to administrative audit logs must be restricted exclusively to Platform Administrators.");
    }

    [Fact]
    public void AdminAnalyticsController_MustBeDecoratedWith_AdminAccessAttribute()
    {
        typeof(AdminAnalyticsController)
            .Should().BeDecoratedWith<AdminAccessAttribute>(
                "Platform analytics must be protected under AdminAccess policy.");
    }

    [Fact]
    public void AdminPlatformFeeController_MustBeDecoratedWith_AdminAccessAttribute()
    {
        typeof(AdminPlatformFeeController)
            .Should().BeDecoratedWith<AdminAccessAttribute>(
                "Fee configuration endpoints must be guarded with AdminAccess.");
    }

    // 1. Kiểm tra controller của Store Analytics có gắn quyền chủ quán ở cấp Class:
    [Fact]
    public void StoreAnalyticsController_MustBeDecoratedWith_StoreOwnerAnyStatusAccess()
    {
        typeof(StoreAnalyticsController)
            .Should().BeDecoratedWith<StoreOwnerAnyStatusAccessAttribute>();
    }

// 2. Kiểm tra các action quản lý trong ShopsController có gắn quyền chủ quán:
    [Theory]
    [InlineData(nameof(ShopsController.GetOwnerProfile))]
    [InlineData(nameof(ShopsController.UpdateBusinessInfo))]
    [InlineData(nameof(ShopsController.UpdateShopImages))]
    public void ShopsController_OwnerEndpoints_MustHave_StoreOwnerAccess(string actionName)
    {
        var method = typeof(ShopsController).GetMethod(actionName);
        method.Should().NotBeNull();
        method!.GetCustomAttribute<StoreOwnerAnyStatusAccessAttribute>().Should().NotBeNull();
    }

    [Fact]
    public void AuthController_ChangePassword_MustHave_AccountAccessAttribute()
    {
        var method = typeof(AuthController).GetMethod(nameof(AuthController.ChangePassword));
        method.Should().NotBeNull();
        method!.GetCustomAttribute<AccountAccessAttribute>().Should().NotBeNull(
            "Change password requires an authenticated account context.");
    }

    [Theory]
    [InlineData(nameof(AuthController.Register))]
    [InlineData(nameof(AuthController.VerifyRegistration))]
    [InlineData(nameof(AuthController.Login))]
    [InlineData(nameof(AuthController.ForgotPassword))]
    [InlineData(nameof(AuthController.ResetPassword))]
    public void AuthController_PublicEndpoints_MustAllowAnonymous(string actionName)
    {
        var method = typeof(AuthController).GetMethod(actionName);
        method.Should().NotBeNull();
        method!.GetCustomAttribute<AllowAnonymousAttribute>().Should().NotBeNull(
            $"Endpoint {actionName} must allow unauthenticated guests to initiate authentication workflows.");
    }
}