using Microsoft.AspNetCore.Authorization;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Controllers;
using Xunit;

namespace SaveBite.Tests;

public sealed class FeedbackReplyAuthorizationTests
{
    [Theory]
    [InlineData(nameof(FeedbacksController.ReplyFeedback))]
    [InlineData(nameof(FeedbacksController.EditReply))]
    [InlineData(nameof(FeedbacksController.DeleteReply))]
    public void Reply_mutations_require_active_store_owner_or_staff(string action)
    {
        var method = typeof(FeedbacksController).GetMethod(action)!;
        var authorization = Assert.Single(method.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>());
        Assert.Equal(AuthorizationPolicies.StoreOwnerOrStaffActive, authorization.Policy);
        Assert.Empty(method.GetCustomAttributes(typeof(AllowAnonymousAttribute), true));
    }
}
