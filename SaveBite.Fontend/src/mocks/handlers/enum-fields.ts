// Finite values extracted from the real DTO unions.
export const enumFields: Record<string, { path: string[]; values: (string | number)[] }[]> = {
  "addresses-addressApi_getAddresses_0": [],
  "addresses-addressApi_createAddress_1": [],
  "addresses-addressApi_updateAddress_2": [],
  "addresses-addressApi_setDefaultAddress_3": [],
  "admin-analytics-platformAnalyticsApi_getDashboard_0": [
    {
      path: ["data", "period", "granularity"],
      values: ["hour", "day", "month"],
    },
  ],
  "admin-analytics-platformAnalyticsApi_getPlatformFees_1": [],
  "admin-analytics-platformAnalyticsApi_getUsers_2": [],
  "admin-analytics-platformAnalyticsApi_getShops_3": [],
  "admin-analytics-platformAnalyticsApi_getRevenueOrders_4": [],
  "admin-analytics-platformAnalyticsApi_getFlashDeals_5": [],
  "admin-analytics-platformAnalyticsApi_getPaymentsRefunds_6": [],
  "audit-logs-auditLogApi_getLogs_0": [
    {
      path: ["data", "page", "items", "*", "actorType"],
      values: ["User", "System"],
    },
  ],
  "audit-logs-auditLogApi_getLogDetail_1": [
    {
      path: ["data", "actorType"],
      values: ["User", "System"],
    },
  ],
  "auth-authApi_changePassword_0": [],
  "auth-authApi_login_1": [],
  "auth-authApi_getMe_2": [
    {
      path: ["data", "customerStatus"],
      values: ["active", "suspended"],
    },
  ],
  "auth-authApi_logout_3": [],
  "auth-authApi_register_4": [],
  "auth-authApi_verifyRegistration_5": [],
  "auth-authApi_forgotPassword_6": [],
  "auth-authApi_resetPassword_7": [],
  "cart-cartApi_getPage_0": [],
  "cart-cartApi_summary_1": [],
  "cart-cartApi_addItem_2": [],
  "cart-cartApi_updateQuantity_3": [],
  "cart-cartApi_removeItem_4": [],
  "categories-adminCategoryApi_getCategories_0": [],
  "categories-adminCategoryApi_createCategory_1": [],
  "categories-adminCategoryApi_updateCategory_2": [],
  "categories-adminCategoryApi_toggleStatus_3": [],
  "categories-adminCategoryApi_deleteCategory_4": [],
  "categories-categoryApi_getCategories_0": [],
  "categories-categoryApi_getCategories_1": [],
  "checkout-checkoutApi_context_0": [],
  "checkout-checkoutApi_quote_1": [],
  "checkout-checkoutApi_place_2": [
    {
      path: ["data", "paymentMethod"],
      values: ["OnlinePayment", "PayAtShop"],
    },
  ],
  "feedback-feedbackApi_list_0": [
    {
      path: ["data", "items", "*", "status"],
      values: ["Visible", "HiddenByCustomer", "HiddenByAdmin"],
    },
  ],
  "feedbacks-feedbackApi_addFeedback_0": [],
  "feedbacks-feedbackApi_getHistory_1": [],
  "feedbacks-feedbackApi_getFeedback_2": [],
  "feedbacks-feedbackApi_editFeedback_3": [],
  "feedbacks-feedbackApi_uploadImage_5": [],
  "feedbacks-feedbackApi_getShopFeedbacks_6": [],
  "feedbacks-feedbackApi_getAdminFeedbacks_7": [],
  "feedbacks-feedbackApi_replyFeedback_8": [],
  "feedbacks-feedbackApi_editReply_9": [],
  "flash-deals-flashDealApi_getNearbyDeals_0": [],
  "flash-deals-flashDealApi_getDealsByShop_1": [],
  "flash-deals-flashDealApi_getDealById_2": [],
  "flash-deals-flashDealApi_getCategories_3": [],
  "flash-deals-flashDealApi_getShopFlashDeals_4": [],
  "flash-deals-flashDealApi_getShopFlashDealDetail_5": [],
  "flash-deals-flashDealApi_getEligibleProducts_6": [],
  "flash-deals-flashDealApi_createFlashDeal_7": [],
  "flash-deals-flashDealApi_updateFlashDeal_8": [],
  "notifications-notificationApi_getNotifications_0": [],
  "notifications-notificationApi_markAsRead_1": [],
  "orders-orderApi_orderPage_0": [
    {
      path: ["data", "page", "items", "*", "paymentMethod"],
      values: ["OnlinePayment", "PayAtShop"],
    },
  ],
  "orders-orderApi_orders_1": [
    {
      path: ["data", "*", "paymentMethod"],
      values: ["OnlinePayment", "PayAtShop"],
    },
  ],
  "orders-orderApi_order_2": [
    {
      path: ["data", "paymentMethod"],
      values: ["OnlinePayment", "PayAtShop"],
    },
  ],
  "orders-orderApi_cancel_3": [
    {
      path: ["data", "paymentMethod"],
      values: ["OnlinePayment", "PayAtShop"],
    },
  ],
  "payments-paymentApi_page_0": [
    {
      path: ["data", "items", "*", "type"],
      values: ["Full", "Deposit", "Remaining"],
    },
  ],
  "payments-paymentApi_detail_1": [
    {
      path: ["data", "payment", "type"],
      values: ["Full", "Deposit", "Remaining"],
    },
    {
      path: ["data", "order", "paymentMethod"],
      values: ["OnlinePayment", "PayAtShop"],
    },
  ],
  "payments-paymentApi_simulation_2": [
    {
      path: ["data", "paymentMethod"],
      values: ["OnlinePayment", "PayAtShop"],
    },
  ],
  "payments-paymentApi_simulate_3": [
    {
      path: ["data", "paymentMethod"],
      values: ["OnlinePayment", "PayAtShop"],
    },
  ],
  "payments-paymentApi_payment_4": [
    {
      path: ["data", "paymentMethod"],
      values: ["OnlinePayment", "PayAtShop"],
    },
  ],
  "platform-fees-platformFeeApi_getActiveConfig_0": [],
  "platform-fees-platformFeeApi_getAllConfigs_1": [],
  "platform-fees-platformFeeApi_getAdminStatements_2": [
    {
      path: ["data", "items", "*", "status"],
      values: ["Draft", "Issued", "Paid", "Overdue"],
    },
  ],
  "platform-fees-platformFeeApi_getAdminStatementDetail_3": [
    {
      path: ["data", "status"],
      values: ["Draft", "Issued", "Paid", "Overdue"],
    },
  ],
  "platform-fees-platformFeeApi_updateConfig_4": [],
  "platform-fees-platformFeeApi_cancelScheduledConfig_5": [],
  "platform-fees-platformFeeApi_processOverdue_6": [],
  "platform-fees-platformFeeApi_generateMonthly_7": [],
  "platform-fees-platformFeeApi_recordExternalPayment_8": [],
  "platform-fees-platformFeeApi_getShopStatements_9": [
    {
      path: ["data", "items", "*", "status"],
      values: ["Draft", "Issued", "Paid", "Overdue"],
    },
  ],
  "platform-fees-platformFeeApi_getStatementDetail_10": [
    {
      path: ["data", "status"],
      values: ["Draft", "Issued", "Paid", "Overdue"],
    },
  ],
  "platform-fees-platformFeeApi_createPaymentLink_11": [],
  "products-productApi_summary_0": [],
  "products-productApi_options_1": [],
  "products-productApi_list_2": [
    {
      path: ["data", "items", "*", "images", "*", "source"],
      values: ["product", "value"],
    },
  ],
  "products-productApi_get_3": [
    {
      path: ["data", "images", "*", "source"],
      values: ["product", "value"],
    },
  ],
  "products-productApi_create_4": [
    {
      path: ["data", "images", "*", "source"],
      values: ["product", "value"],
    },
  ],
  "products-productApi_update_5": [
    {
      path: ["data", "images", "*", "source"],
      values: ["product", "value"],
    },
  ],
  "products-productApi_images_7": [
    {
      path: ["data", "images", "*", "source"],
      values: ["product", "value"],
    },
  ],
  "products-productApi_addValues_8": [
    {
      path: ["data", "images", "*", "source"],
      values: ["product", "value"],
    },
  ],
  "products-productApi_valueImage_9": [
    {
      path: ["data", "images", "*", "source"],
      values: ["product", "value"],
    },
  ],
  "products-productApi_deals_10": [],
  "products-productApi_updateVariant_11": [
    {
      path: ["data", "images", "*", "source"],
      values: ["product", "value"],
    },
  ],
  "products-productApi_updateStatus_12": [
    {
      path: ["data", "images", "*", "source"],
      values: ["product", "value"],
    },
  ],
  "products-productApi_getProductsForManagement_13": [
    {
      path: ["data", "items", "*", "images", "*", "source"],
      values: ["product", "value"],
    },
  ],
  "products-productApi_getProductContentForManagement_14": [
    {
      path: ["data", "images", "*", "source"],
      values: ["product", "value"],
    },
  ],
  "products-productApi_removeForViolation_15": [],
  "products-productApi_approveContent_16": [],
  "profiles-profileApi_updateProfile_0": [
    {
      path: ["data", "customerStatus"],
      values: ["active", "suspended"],
    },
  ],
  "profiles-profileApi_uploadAvatar_1": [
    {
      path: ["data", "customerStatus"],
      values: ["active", "suspended"],
    },
  ],
  "refunds-refundApi_detail_0": [
    {
      path: ["data", "shopAction"],
      values: ["Closed", "WaitingCustomer", "DeadlinePassed", "ReviewRequired", "TransferRequired"],
    },
    {
      path: ["data", "type"],
      values: ["CustomerRequest", "ShopCancellation"],
    },
    {
      path: ["data", "receiverMethod"],
      values: ["BankAccount", "QrImage"],
    },
  ],
  "refunds-refundApi_shopPendingCount_1": [],
  "refunds-refundApi_shopDetail_2": [
    {
      path: ["data", "shopAction"],
      values: ["Closed", "WaitingCustomer", "DeadlinePassed", "ReviewRequired", "TransferRequired"],
    },
    {
      path: ["data", "type"],
      values: ["CustomerRequest", "ShopCancellation"],
    },
    {
      path: ["data", "receiverMethod"],
      values: ["BankAccount", "QrImage"],
    },
  ],
  "refunds-refundApi_upload_3": [],
  "refunds-refundApi_shopPage_4": [
    {
      path: ["data", "items", "*", "shopAction"],
      values: ["Closed", "WaitingCustomer", "DeadlinePassed", "ReviewRequired", "TransferRequired"],
    },
    {
      path: ["data", "items", "*", "type"],
      values: ["CustomerRequest", "ShopCancellation"],
    },
    {
      path: ["data", "items", "*", "receiverMethod"],
      values: ["BankAccount", "QrImage"],
    },
  ],
  "refunds-refundApi_shopOrder_5": [
    {
      path: ["data", "*", "shopAction"],
      values: ["Closed", "WaitingCustomer", "DeadlinePassed", "ReviewRequired", "TransferRequired"],
    },
    {
      path: ["data", "*", "type"],
      values: ["CustomerRequest", "ShopCancellation"],
    },
    {
      path: ["data", "*", "receiverMethod"],
      values: ["BankAccount", "QrImage"],
    },
  ],
  "refunds-refundApi_review_6": [
    {
      path: ["data", "shopAction"],
      values: ["Closed", "WaitingCustomer", "DeadlinePassed", "ReviewRequired", "TransferRequired"],
    },
    {
      path: ["data", "type"],
      values: ["CustomerRequest", "ShopCancellation"],
    },
    {
      path: ["data", "receiverMethod"],
      values: ["BankAccount", "QrImage"],
    },
  ],
  "refunds-refundApi_qr_7": [],
  "refunds-refundApi_transfer_8": [
    {
      path: ["data", "shopAction"],
      values: ["Closed", "WaitingCustomer", "DeadlinePassed", "ReviewRequired", "TransferRequired"],
    },
    {
      path: ["data", "type"],
      values: ["CustomerRequest", "ShopCancellation"],
    },
    {
      path: ["data", "receiverMethod"],
      values: ["BankAccount", "QrImage"],
    },
  ],
  "refunds-refundApi_pendingDetailsCount_9": [],
  "refunds-refundApi_page_10": [
    {
      path: ["data", "items", "*", "shopAction"],
      values: ["Closed", "WaitingCustomer", "DeadlinePassed", "ReviewRequired", "TransferRequired"],
    },
    {
      path: ["data", "items", "*", "type"],
      values: ["CustomerRequest", "ShopCancellation"],
    },
    {
      path: ["data", "items", "*", "receiverMethod"],
      values: ["BankAccount", "QrImage"],
    },
  ],
  "refunds-refundApi_forOrder_11": [
    {
      path: ["data", "*", "shopAction"],
      values: ["Closed", "WaitingCustomer", "DeadlinePassed", "ReviewRequired", "TransferRequired"],
    },
    {
      path: ["data", "*", "type"],
      values: ["CustomerRequest", "ShopCancellation"],
    },
    {
      path: ["data", "*", "receiverMethod"],
      values: ["BankAccount", "QrImage"],
    },
  ],
  "refunds-refundApi_request_12": [
    {
      path: ["data", "shopAction"],
      values: ["Closed", "WaitingCustomer", "DeadlinePassed", "ReviewRequired", "TransferRequired"],
    },
    {
      path: ["data", "type"],
      values: ["CustomerRequest", "ShopCancellation"],
    },
    {
      path: ["data", "receiverMethod"],
      values: ["BankAccount", "QrImage"],
    },
  ],
  "refunds-refundApi_destination_13": [
    {
      path: ["data", "shopAction"],
      values: ["Closed", "WaitingCustomer", "DeadlinePassed", "ReviewRequired", "TransferRequired"],
    },
    {
      path: ["data", "type"],
      values: ["CustomerRequest", "ShopCancellation"],
    },
    {
      path: ["data", "receiverMethod"],
      values: ["BankAccount", "QrImage"],
    },
  ],
  "refunds-refundApi_confirm_14": [
    {
      path: ["data", "shopAction"],
      values: ["Closed", "WaitingCustomer", "DeadlinePassed", "ReviewRequired", "TransferRequired"],
    },
    {
      path: ["data", "type"],
      values: ["CustomerRequest", "ShopCancellation"],
    },
    {
      path: ["data", "receiverMethod"],
      values: ["BankAccount", "QrImage"],
    },
  ],
  "refunds-refundApi_withdraw_15": [
    {
      path: ["data", "shopAction"],
      values: ["Closed", "WaitingCustomer", "DeadlinePassed", "ReviewRequired", "TransferRequired"],
    },
    {
      path: ["data", "type"],
      values: ["CustomerRequest", "ShopCancellation"],
    },
    {
      path: ["data", "receiverMethod"],
      values: ["BankAccount", "QrImage"],
    },
  ],
  "shop-applications-adminShopApplicationApi_getAll_0": [
    {
      path: ["data", "*", "status"],
      values: ["Pending", "Cancelled", "Approved", "Rejected", "NeedsRevision"],
    },
    {
      path: ["data", "*", "documents", "*", "documentType"],
      values: ["BusinessLicense", "FoodSafetyCertificate", "Other"],
    },
    {
      path: ["data", "*", "reviewLogs", "*", "fromStatus"],
      values: ["Pending", "Cancelled", "Approved", "Rejected", "NeedsRevision"],
    },
    {
      path: ["data", "*", "reviewLogs", "*", "toStatus"],
      values: ["Pending", "Cancelled", "Approved", "Rejected", "NeedsRevision"],
    },
  ],
  "shop-applications-adminShopApplicationApi_getById_1": [
    {
      path: ["data", "status"],
      values: ["Pending", "Cancelled", "Approved", "Rejected", "NeedsRevision"],
    },
    {
      path: ["data", "documents", "*", "documentType"],
      values: ["BusinessLicense", "FoodSafetyCertificate", "Other"],
    },
    {
      path: ["data", "reviewLogs", "*", "fromStatus"],
      values: ["Pending", "Cancelled", "Approved", "Rejected", "NeedsRevision"],
    },
    {
      path: ["data", "reviewLogs", "*", "toStatus"],
      values: ["Pending", "Cancelled", "Approved", "Rejected", "NeedsRevision"],
    },
  ],
  "shop-applications-adminShopApplicationApi_review_2": [
    {
      path: ["data", "status"],
      values: ["Pending", "Cancelled", "Approved", "Rejected", "NeedsRevision"],
    },
    {
      path: ["data", "documents", "*", "documentType"],
      values: ["BusinessLicense", "FoodSafetyCertificate", "Other"],
    },
    {
      path: ["data", "reviewLogs", "*", "fromStatus"],
      values: ["Pending", "Cancelled", "Approved", "Rejected", "NeedsRevision"],
    },
    {
      path: ["data", "reviewLogs", "*", "toStatus"],
      values: ["Pending", "Cancelled", "Approved", "Rejected", "NeedsRevision"],
    },
  ],
  "shop-applications-shopApplicationApi_getMine_0": [
    {
      path: ["data", "status"],
      values: ["Pending", "Cancelled", "Approved", "Rejected", "NeedsRevision"],
    },
    {
      path: ["data", "documents", "*", "documentType"],
      values: ["BusinessLicense", "FoodSafetyCertificate", "Other"],
    },
    {
      path: ["data", "reviewLogs", "*", "fromStatus"],
      values: ["Pending", "Cancelled", "Approved", "Rejected", "NeedsRevision"],
    },
    {
      path: ["data", "reviewLogs", "*", "toStatus"],
      values: ["Pending", "Cancelled", "Approved", "Rejected", "NeedsRevision"],
    },
  ],
  "shop-applications-shopApplicationApi_getHistory_1": [
    {
      path: ["data", "*", "status"],
      values: ["Pending", "Cancelled", "Approved", "Rejected", "NeedsRevision"],
    },
    {
      path: ["data", "*", "documents", "*", "documentType"],
      values: ["BusinessLicense", "FoodSafetyCertificate", "Other"],
    },
    {
      path: ["data", "*", "reviewLogs", "*", "fromStatus"],
      values: ["Pending", "Cancelled", "Approved", "Rejected", "NeedsRevision"],
    },
    {
      path: ["data", "*", "reviewLogs", "*", "toStatus"],
      values: ["Pending", "Cancelled", "Approved", "Rejected", "NeedsRevision"],
    },
  ],
  "shop-applications-shopApplicationApi_create_2": [
    {
      path: ["data", "status"],
      values: ["Pending", "Cancelled", "Approved", "Rejected", "NeedsRevision"],
    },
    {
      path: ["data", "documents", "*", "documentType"],
      values: ["BusinessLicense", "FoodSafetyCertificate", "Other"],
    },
    {
      path: ["data", "reviewLogs", "*", "fromStatus"],
      values: ["Pending", "Cancelled", "Approved", "Rejected", "NeedsRevision"],
    },
    {
      path: ["data", "reviewLogs", "*", "toStatus"],
      values: ["Pending", "Cancelled", "Approved", "Rejected", "NeedsRevision"],
    },
  ],
  "shop-applications-shopApplicationApi_resubmit_3": [
    {
      path: ["data", "status"],
      values: ["Pending", "Cancelled", "Approved", "Rejected", "NeedsRevision"],
    },
    {
      path: ["data", "documents", "*", "documentType"],
      values: ["BusinessLicense", "FoodSafetyCertificate", "Other"],
    },
    {
      path: ["data", "reviewLogs", "*", "fromStatus"],
      values: ["Pending", "Cancelled", "Approved", "Rejected", "NeedsRevision"],
    },
    {
      path: ["data", "reviewLogs", "*", "toStatus"],
      values: ["Pending", "Cancelled", "Approved", "Rejected", "NeedsRevision"],
    },
  ],
  "shop-orders-shopOrderApi_ownerShop_0": [],
  "shop-orders-shopOrderApi_page_1": [
    {
      path: ["data", "page", "items", "*", "order", "paymentMethod"],
      values: ["OnlinePayment", "PayAtShop"],
    },
  ],
  "shop-orders-shopOrderApi_pendingCount_2": [],
  "shop-orders-shopOrderApi_detail_3": [
    {
      path: ["data", "order", "paymentMethod"],
      values: ["OnlinePayment", "PayAtShop"],
    },
  ],
  "shop-orders-shopOrderApi_action_4": [
    {
      path: ["data", "order", "paymentMethod"],
      values: ["OnlinePayment", "PayAtShop"],
    },
  ],
  "shop-orders-shopOrderApi_refund_6": [],
  "shop-orders-shopOrderApi_destination_7": [],
  "shop-orders-shopOrderApi_verify_8": [],
  "shop-staffs-shopStaffApi_getStaffList_0": [
    {
      path: ["data", "items", "*", "status"],
      values: ["Active", "Suspended", "Removed"],
    },
  ],
  "shop-staffs-shopStaffApi_getStaffActivityLogs_1": [],
  "shop-staffs-shopStaffApi_updateStaff_2": [],
  "shop-staffs-shopStaffApi_removeStaff_3": [],
  "shop-staffs-shopStaffApi_inviteStaff_4": [],
  "shop-staffs-shopStaffApi_searchStaffCandidates_5": [],
  "shop-staffs-shopStaffApi_getInvitations_6": [],
  "shop-staffs-shopStaffApi_revokeInvitation_7": [],
  "shops-ownerShopApi_getMyShop_0": [],
  "shops-ownerShopApi_getMyShopProfile_1": [],
  "shops-ownerShopApi_updateBusinessInfo_2": [],
  "shops-ownerShopApi_updateImages_3": [],
  "shops-ownerShopApi_getMyPaymentConfig_4": [],
  "shops-ownerShopApi_updateMyPaymentConfig_5": [],
  "shops-shopApi_getNearbyShops_0": [],
  "shops-shopApi_getShopProfile_1": [],
  "shops-shopApi_getStoreReviews_2": [],
  "shops-shopApi_getShopsForManagement_3": [],
  "shops-shopApi_getShopContentForManagement_4": [],
  "shops-shopApi_updateModerationStatus_5": [],
  "staff-invitations-staffInvitationApi_getMyInvitations_0": [
    {
      path: ["data", "*", "status"],
      values: ["Pending", "Cancelled", "Accepted", "Declined", "Expired"],
    },
  ],
  "staff-shops-staffApi_getAssociatedShops_0": [
    {
      path: ["data", "*", "status"],
      values: ["Active", "Suspended", "Removed"],
    },
  ],
  "staff-shops-staffApi_getShopDetail_1": [
    {
      path: ["data", "staff", "status"],
      values: ["Active", "Suspended", "Removed"],
    },
  ],
  "store-analytics-storeAnalyticsApi_getDashboard_0": [
    {
      path: ["data", "period", "granularity"],
      values: ["hour", "day", "month"],
    },
  ],
  "store-analytics-storeAnalyticsApi_getRevenueOverview_1": [],
  "store-analytics-storeAnalyticsApi_getOrderStatistics_2": [],
  "store-analytics-storeAnalyticsApi_getFlashDealStatistics_3": [],
  "store-analytics-storeAnalyticsApi_getPaymentMethodRevenue_4": [],
  "trust-adminTrustApi_getLevels_0": [],
  "trust-adminTrustApi_getLevelHistory_1": [
    {
      path: ["data", "*", "changeType"],
      values: [
        "Created",
        "InfoUpdated",
        "RequirementUpdated",
        "BenefitsUpdated",
        "Deactivated",
        "Reactivated",
      ],
    },
  ],
  "trust-adminTrustApi_createLevel_2": [],
  "trust-adminTrustApi_updateLevelInfo_3": [],
  "trust-adminTrustApi_updateLevelStatus_4": [],
  "trust-adminTrustApi_updateLevelBenefits_5": [],
  "trust-adminTrustApi_updateLevelRequirement_6": [],
  "trust-adminTrustApi_getPenaltyRules_7": [],
  "trust-adminTrustApi_createPenaltyRule_8": [],
  "trust-adminTrustApi_updatePenaltyRule_9": [],
  "trust-adminTrustApi_getScoreRules_11": [
    {
      path: ["data", "*", "ruleType"],
      values: ["OnlineOrderCompleted", "PayAtShopOrderCompleted", "NoShow"],
    },
    {
      path: ["data", "*", "kind"],
      values: ["Earning", "Penalty"],
    },
  ],
  "trust-adminTrustApi_updateScoreRule_12": [
    {
      path: ["data", "ruleType"],
      values: ["OnlineOrderCompleted", "PayAtShopOrderCompleted", "NoShow"],
    },
    {
      path: ["data", "kind"],
      values: ["Earning", "Penalty"],
    },
  ],
  "trust-adminTrustApi_getEarningRules_13": [
    {
      path: ["data", "*", "ruleType"],
      values: ["OnlineOrderCompleted", "PayAtShopOrderCompleted", "NoShow"],
    },
    {
      path: ["data", "*", "kind"],
      values: ["Earning", "Penalty"],
    },
  ],
  "trust-adminTrustApi_updateEarningRule_14": [
    {
      path: ["data", "ruleType"],
      values: ["OnlineOrderCompleted", "PayAtShopOrderCompleted", "NoShow"],
    },
    {
      path: ["data", "kind"],
      values: ["Earning", "Penalty"],
    },
  ],
  "trust-trustApi_getOverview_0": [
    {
      path: ["data", "standing"],
      values: ["GoodStanding", "Restricted", "OfflineLocked"],
    },
  ],
  "trust-trustApi_getLevelProgress_1": [
    {
      path: ["data", "metrics", "*", "key"],
      values: ["score", "completedOrders", "orderValue", "onlineOrders", "onlineOrderValue"],
    },
  ],
  "trust-trustApi_getHistory_2": [
    {
      path: ["data", "items", "*", "eventType"],
      values: ["NoShow", "OrderCompleted"],
    },
  ],
  "trust-trustApi_getLevels_3": [],
  "trust-trustApi_getPenalties_4": [
    {
      path: ["data", "items", "*", "status"],
      values: ["Recovering", "Recovered"],
    },
  ],
  "trust-trustApi_getPenaltyRecovery_5": [
    {
      path: ["data", "penalty", "status"],
      values: ["Recovering", "Recovered"],
    },
    {
      path: ["data", "metrics", "*", "key"],
      values: ["score", "completedOrders", "orderValue", "onlineOrders", "onlineOrderValue"],
    },
  ],
  "users-userApi_getUsers_0": [],
  "users-userApi_getUserDetails_1": [
    {
      path: ["data", "logs", "*", "target"],
      values: ["Account", "Customer"],
    },
  ],
  "users-userApi_updateStatus_2": [],
  "warnings-warningApi_history_0": [],
  "warnings-warningApi_details_1": [],
  "warnings-warningApi_issueForProduct_2": [],
};
