namespace SaveBite.Backend.Models.Enums;

// Action recorded in staff_activity_logs. The first ten names mirror OrderStatus so existing rows
// (stored as strings) keep working; the rest are non-order staff activities.
public enum StaffActivityAction
{
    Pending,
    Confirmed,
    Preparing,
    ReadyForPickup,
    Completed,
    Cancelled,
    Expired,
    NoShow,
    Rejected,
    AwaitingPayment,
    FeedbackReplied
}
