using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using Xunit;

namespace SaveBite.Tests;

public class RefundExpiryTests
{
    [Theory]
    [InlineData(RefundType.ShopCancellation)]
    [InlineData(RefundType.CustomerRequest)]
    public void MissingCustomerDetailsHasItsOwnClosedStatus(RefundType type)
    {
        var refund = new Refund { RefundType = type, Status = RefundStatus.AwaitingCustomerDetails };
        Assert.Equal(RefundStatus.CustomerDetailsExpired, RefundRules.ExpiredStatus(refund));
        Assert.False(RefundRules.Active(RefundStatus.CustomerDetailsExpired));
    }

    [Fact]
    public void MandatoryTransferStillBecomesOverdue()
    {
        Assert.Equal(RefundStatus.Overdue, RefundRules.ExpiredStatus(new Refund
        { RefundType = RefundType.ShopCancellation, Status = RefundStatus.AwaitingTransfer, RequestedAmount = 20000 }));
    }

    [Fact]
    public void QrDestinationDoesNotRequireManuallyEnteredHolder()
    {
        var request = new RefundBankRequest { ReceiverMethod = "QrImage", BankBin = "970436",
            BankName = "Bank", AccountNumber = "123456", AccountHolder = "" };
        RefundRules.ValidateBank(request);
        request.ReceiverMethod = "BankAccount";
        Assert.Throws<SaveBite.Backend.Exceptions.AppException>(() => RefundRules.ValidateBank(request));
    }
}
