using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Validators;
using Xunit;

namespace SaveBite.Tests;

public class FlashDealValidationAuditTests
{
    [Fact]
    public void DuplicateVariantsAreRejectedOnCreate()
    {
        var id = Guid.NewGuid();
        var request = new CreateFlashDealRequest { ProductId = Guid.NewGuid(), IsImmediate = true,
            DurationMinutes = 30, OrderEndTime = DateTime.UtcNow.AddMinutes(30), ShopClosingTime = DateTime.UtcNow.AddMinutes(40),
            Variants = [new() { VariantId = id, DealPrice = 2000, TotalQuantity = 1 }, new() { VariantId = id, DealPrice = 2000, TotalQuantity = 1 }] };
        Assert.Contains(new CreateFlashDealRequestValidator().Validate(request).Errors, e => e.PropertyName == "Variants");
    }

    [Fact]
    public void DuplicateVariantsAndUnknownStatusAreRejectedOnUpdate()
    {
        var id = Guid.NewGuid();
        var request = new UpdateFlashDealRequest { SaleStartTime = DateTime.UtcNow, OrderEndTime = DateTime.UtcNow.AddMinutes(30),
            ShopClosingTime = DateTime.UtcNow.AddMinutes(40), Variants = [
                new() { VariantId = id, DealPrice = 2000, TotalQuantity = 1, Status = (FlashDealVariantStatus)99 },
                new() { VariantId = id, DealPrice = 2000, TotalQuantity = 1 }] };
        var errors = new UpdateFlashDealRequestValidator().Validate(request).Errors;
        Assert.Contains(errors, e => e.PropertyName == "Variants");
        Assert.Contains(errors, e => e.PropertyName == "Variants[0].Status");
    }
}
