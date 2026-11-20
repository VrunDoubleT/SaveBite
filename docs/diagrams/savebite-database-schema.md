# Database diagram schema

Source: `SaveBite.Backend/SaveBite.Api/Data/Migrations/AppDbContextModelSnapshot.cs`.

74 application tables and 127 foreign-key relationships. This describes the migration snapshot, not a live database inspection. Framework-managed tables such as Hangfire and EF migration history are outside this model.

The earlier sample had 50 tables. 25 current table names were absent; `trust_score_history` from the sample is absent from the snapshot. All columns and relationships were rebuilt from the snapshot, rather than only appending tables.

## Tables

- `account_status_logs`
- `audit_log_archives`
- `audit_logs`
- `cart_items`
- `categories`
- `checkout_sessions`
- `deal_notification_recipients`
- `email_deliveries`
- `event_outbox`
- `flash_deal_status_history`
- `flash_deal_variants`
- `flash_deals`
- `no_show_penalties`
- `no_show_penalty_order_history`
- `notifications`
- `order_items`
- `order_platform_fees`
- `order_refund_requests`
- `order_status_history`
- `orders`
- `payments`
- `payos_webhook_sync_states`
- `platform_fee_adjustments`
- `platform_fee_configs`
- `platform_fee_payments`
- `platform_fee_statement_histories`
- `platform_fee_statements`
- `platform_payos_config_history`
- `platform_payos_configs`
- `product_attribute_values`
- `product_attributes`
- `product_content_revisions`
- `product_feedback_images`
- `product_feedback_moderation_logs`
- `product_feedback_replies`
- `product_feedbacks`
- `product_images`
- `product_variant_values`
- `product_variants`
- `product_warnings`
- `products`
- `recovery_requirement_rules`
- `refresh_tokens`
- `refund_evidence_images`
- `refund_items`
- `refund_status_history`
- `refunds`
- `shop_application_documents`
- `shop_application_review_logs`
- `shop_applications`
- `shop_follows`
- `shop_payment_configs`
- `shop_staff`
- `shop_status_histories`
- `shops`
- `staff_activity_logs`
- `staff_invitations`
- `trust_level_deposit_policies`
- `trust_level_deposit_ranges`
- `trust_level_histories`
- `trust_level_requirements`
- `trust_levels`
- `trust_order_processing`
- `trust_policy_version`
- `trust_progress_history`
- `trust_score_rules`
- `upload_assets`
- `upload_sessions`
- `user_addresses`
- `user_level_progress`
- `user_role_change_logs`
- `user_trust_level_history`
- `user_trust_scores`
- `users`
