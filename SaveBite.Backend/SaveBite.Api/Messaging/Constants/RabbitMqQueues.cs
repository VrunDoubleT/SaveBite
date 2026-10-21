namespace SaveBite.Backend.Messaging.Constants;

public static class RabbitMqQueues
{
    public const string CartFlashDealDeadLetterQueueName = "cart_flash_deal_dead_letter_queue";
    public const string CartFlashDealQueueName = "cart_flash_deal_queue";
    public const string OrderNoShowQueueName = "order_noshow_queue";
    public const string OrderPendingQueueName = "order_pending_queue";
    public const string OrderPendingDeadLetterQueueName = "order_pending_dead_letter_queue";
    public const string EmailQueueName = "email_queue";
    public const string FlashDealQueueName = "flashdeal_queue"; 
}