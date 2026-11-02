# UI redesign progress

Ngày audit: 2026-11-02 (Asia/Bangkok). Nhánh: ui-redesign (đã tồn tại).
Audit và bước tokens/primitives đã hoàn tất. Đã cập nhật theme, shared shell và UI reference; redesign trang `/`, flash deal detail, storefront, cart, checkout, profile, danh sách và chi tiết đơn hàng, thanh toán và hoàn tiền, reviews, Trust Score (4 tab), shop application, workspace và staff invitations.
Breadcrumb hiện bắt đầu bằng chữ, không có mũi tên quay lại ở đầu; giữ các dấu phân cách giữa cấp.
Chi tiết: [ROUTES.md](./ROUTES.md). Trang `/`, flash deal detail, storefront cùng các alias và `/cart`, `/checkout`, `/account/profile`, `/account/orders` và `/orders/:orderId`, `/account/payments`, `/account/payments/:paymentId`, `/account/refunds`, `/account/refunds/:refundId`, `/account/reviews` và `/account/trust-score/:tab?`, `/account/shop-application`, `/account/workspace`, `/account/staff-invitations` đã hoàn tất; các route còn lại giữ trạng thái todo. Redirect cần regression, không có page riêng.

| Route/task | Status | Commit | Notes |
|---|---|---|---|
| Shell/layout + shared components | done | ui(tokens): theme and primitives | Responsive shell; desktop collapse, mobile drawer, primitives; xem TOKENS.md |
| Tokens | done | ui(tokens): theme and primitives | Giữ 10 primary greens; màu phụ theo reference; xem TOKENS.md |
| `/` | done | ui(/): redesign | R1; CustomerHomePage; discovery, filters, cards, collapsible map, initial location entry: full-width plain canvas, centered NotFound-style illustration/copy/actions, centered header retained; native GPS denial/retry; location/error/loading/empty/partial states; refined subtle color accents; 360/768/1280px; xem HOME.md |
| `/shops/:id` | done | ui(/shops/:id): redesign | R2; đầy đủ profile/info/tabs; DealGrid/DealCard dùng chung home; StoreReviewList dùng chung detail; retry/partial/empty; xem SHOP.md |
| `/shop-profile` | done | ui(/shops/:id): redesign | R3; cùng component; route không có id vẫn giữ unavailable gate cũ, UI/regression đã kiểm tra; xem SHOP.md |
| `/flash-deals/:id` | done | ui(/flash-deals/:id): redesign | R4; approved home accents; gallery 5 ảnh + 4 demo variants, title without green rule, variant/quantity/cart, pickup, full Customer reviews components, all states; 360/768/1280px; xem DEAL-DETAIL.md |
| `/deals/:id` | done | ui(/flash-deals/:id): redesign | R5; alias cùng CustomerDealDetailPage; responsive/navigation regression passed; xem DEAL-DETAIL.md |
| `/login` | todo | — | R6; LoginPage; guest, user, staff, storeOwner (admin redirected) |
| `/register` | todo | — | R7; RegisterPage; guest, user, staff, storeOwner (admin redirected) |
| `/register/verify` | todo | — | R8; OtpVerificationPage; guest, user, staff, storeOwner (admin redirected) |
| `/forgot-password` | todo | — | R9; ForgotPasswordPage; guest, user, staff, storeOwner (admin redirected) |
| `/forgot-password/verify` | todo | — | R10; OtpVerificationPage; guest, user, staff, storeOwner (admin redirected) |
| `/customer` | todo | — | R11; Redirect regression |
| `/cart` | done | ui(/cart): redesign | R12; đầy đủ nhóm shop/món/quantity/remove/checkout riêng từng shop; shared CartItemRow với checkout; cursor/retry và mọi state; 360/768/1280px; xem CART.md |
| `/checkout/simulation` | todo | — | R13; CheckoutSimulationPage; user, staff, storeOwner |
| `/orders/:orderId` | done | ui(/account/orders): redesign | R14; code/status/pickup/products/payment/refunds/cancellation/reviews; real completed orders at 360/768/1280px; xem ORDERS.md |
| `/checkout` | done | ui(/checkout): redesign | R15; full shop/items/GPS/map/payment/policy/summary + receipt, cancellation, review/refund components; 360/768/1280px; xem CHECKOUT.md |
| `/account` | todo | — | R16; Redirect regression |
| `/account/profile` | done | ui(/account/profile): redesign | R17; identity/avatar, profile/password forms, complete addresses/GPS/map/default actions; real-backend read-only validation at 360/768/1280px; status beside email + inputs/focus matching Login; xem PROFILE.md |
| `/account/orders` | done | ui(/account/orders): redesign | R18; full status filters/counts, shared order items/amounts, cursor loading, empty/retry, reviews; real-backend read-only 360/768/1280px; xem ORDERS.md |
| `/account/payments` | done | ui(account): redesign payments and refunds | R19; full content/states, shared FilterBar/AmountSummary; real-backend read-only 360/768/1280px; xem PAYMENTS-REFUNDS.md |
| `/account/refunds` | done | ui(account): redesign payments and refunds | R20; full content/states, shared FilterBar/AmountSummary; real-backend read-only 360/768/1280px; xem PAYMENTS-REFUNDS.md |
| `/account/refunds/:refundId` | done | ui(account): redesign payments and refunds | R21; full content/states, shared FilterBar/AmountSummary; real-backend read-only 360/768/1280px; xem PAYMENTS-REFUNDS.md |
| `/account/payments/:paymentId` | done | ui(account): redesign payments and refunds | R22; full content/states, shared FilterBar/AmountSummary; real-backend read-only 360/768/1280px; xem PAYMENTS-REFUNDS.md |
| `/account/reviews` | done | ui(account): redesign reviews and trust score | R23; full review cards, shared inline/modal ReviewEditor, photos/replies, delete confirmation, real pagination/deep links; real-backend read-only 360/768/1280px; xem REVIEWS-TRUST.md |
| `/account/trust-score/:tab?` | done | ui(account): redesign reviews and trust score | R24; Overview/Levels/History/Penalties, next-level metrics, policies, recovery modal; real-backend read-only 360/768/1280px; xem REVIEWS-TRUST.md |
| `/account/shop-application` | done | ui(account): compact finance/trust and business pages | R25; complete compact page/states/children; real data read-only at 360/768/1280px; xem ACCOUNT-BUSINESS.md |
| `/account/shop-application/new` | todo | — | R26; ShopApplicationFormPage; user, staff, storeOwner |
| `/account/shop-application/edit/:applicationId` | todo | — | R27; ShopApplicationFormPage; user, staff, storeOwner |
| `/account/workspace` | done | ui(account): compact finance/trust and business pages | R28; complete compact page/states/children; real data read-only at 360/768/1280px; xem ACCOUNT-BUSINESS.md |
| `/account/staff-invitations` | done | ui(account): compact finance/trust and business pages | R29; complete compact page/states/children; real data read-only at 360/768/1280px; xem ACCOUNT-BUSINESS.md |
| `/admin` | todo | — | R30; Redirect regression |
| `/admin/analytics/:tab?` | todo | — | R31; AdminDashboardPage; admin |
| `/admin/users` | todo | — | R32; UserManagementPage; admin |
| `/admin/categories` | todo | — | R33; CategoryManagementPage; admin |
| `/admin/trust/:tab?` | todo | — | R34; TrustConfigurationPage; admin |
| `/admin/products` | todo | — | R35; ProductManagementPage; admin |
| `/admin/products/:productId` | todo | — | R36; ProductContentManagementPage; admin |
| `/admin/orders` | todo | — | R37; ComingSoonPage; admin |
| `/admin/stores` | todo | — | R38; ShopManagementPage; admin |
| `/admin/stores/:shopId` | todo | — | R39; ShopContentManagementPage; admin |
| `/admin/shop-applications` | todo | — | R40; AdminShopApplicationsPage; admin |
| `/admin/platform-fees` | todo | — | R41; PlatformFeeManagementPage; admin |
| `/admin/feedback` | todo | — | R42; FeedbackManagementPage; admin |
| `/admin/reviews` | todo | — | R43; AdminReviewsPage; admin |
| `/admin/audit-logs` | todo | — | R44; AdminAuditLogsPage; admin |
| `/staff` | todo | — | R45; Redirect regression |
| `/staff/shops/:shopId` | todo | — | R46; Redirect regression |
| `/staff/shops/:shopId/flash-deals` | todo | — | R47; StaffFlashDealsPage; staff, storeOwner |
| `/staff/shops/:shopId/flash-deals/:id` | todo | — | R48; StaffFlashDealDetailPage; staff, storeOwner |
| `/staff/shops/:shopId/reviews` | todo | — | R49; StoreOwnerReviewsPage; staff, storeOwner |
| `/staff/shops/:shopId/orders` | todo | — | R50; ShopOrdersPage; staff, storeOwner |
| `/staff/shops/:shopId/orders/:orderId` | todo | — | R51; ShopOrderDetailPage; staff, storeOwner |
| `/staff/shops/:shopId/info` | todo | — | R52; StaffShopDetailPage; staff, storeOwner |
| `/store-owner` | todo | — | R53; StoreOwnerDashboardPage; storeOwner |
| `/store-owner/products` | todo | — | R54; ProductListPage; storeOwner |
| `/store-owner/products/new` | todo | — | R55; CreateProductPage; storeOwner |
| `/store-owner/products/:productId` | todo | — | R56; ProductDetailPage; storeOwner |
| `/store-owner/products/:productId/flash-deals` | todo | — | R57; ProductHistoryPage; storeOwner |
| `/store-owner/orders` | todo | — | R58; ShopOrdersPage; storeOwner |
| `/store-owner/orders/:orderId` | todo | — | R59; ShopOrderDetailPage; storeOwner |
| `/store-owner/refunds` | todo | — | R60; ShopRefundsPage; storeOwner |
| `/store-owner/refunds/:refundId` | todo | — | R61; ShopRefundDetailPage; storeOwner |
| `/store-owner/flash-deals` | todo | — | R62; StoreOwnerFlashDealsPage; storeOwner |
| `/store-owner/flash-deals/create` | todo | — | R63; StoreOwnerFlashDealFormPage; storeOwner |
| `/store-owner/flash-deals/:id` | todo | — | R64; StoreOwnerFlashDealDetailPage; storeOwner |
| `/store-owner/flash-deals/:id/edit` | todo | — | R65; StoreOwnerFlashDealFormPage; storeOwner |
| `/store-owner/staff` | todo | — | R66; StaffManagementPage; storeOwner |
| `/store-owner/staff/invitations` | todo | — | R67; StoreOwnerStaffInvitationsPage; storeOwner |
| `/store-owner/staff/:staffId` | todo | — | R68; StaffDetailPage; storeOwner |
| `/store-owner/payment-settings` | todo | — | R69; PaymentSettingsPage; storeOwner |
| `/store-owner/platform-fees` | todo | — | R70; PlatformFeesPage; storeOwner |
| `/store-owner/platform-fees/:statementId` | todo | — | R71; PlatformFeeDetailPage; storeOwner |
| `/store-owner/profile` | todo | — | R72; ShopProfilePage; storeOwner |
| `/store-owner/warnings` | todo | — | R73; WarningHistoryPage; storeOwner |
| `/store-owner/reviews` | todo | — | R74; StoreOwnerReviewsPage; storeOwner |
| `*` | todo | — | R75; NotFoundPage; public |
| QA toàn bộ route/role/tab/alias | todo | — | Responsive, a11y, normal/loading/empty/error |

Showcase `/__ui`: chỉ có trong DEV khi VITE_ENABLE_UI_SHOWCASE=true; ví dụ primitives tĩnh, không can thiệp API/auth/realtime.

Frontend hiện dùng backend thật. Lớp mock, generator, worker và các test phụ thuộc đã được gỡ theo yêu cầu; xem [BACKEND-INTEGRATION.md](./BACKEND-INTEGRATION.md) để biết phạm vi và kết quả kiểm tra. Các trạng thái done ghi nhận UI đã thiết kế; integration QA với backend thật vẫn cần xác minh bằng tài khoản và dữ liệu thật.

Hướng dẫn AI hiện hành: [WORKFLOW.md](./WORKFLOW.md), [frontend AGENTS.md](../../AGENTS.md). Nếu skill bên ngoài yêu cầu tạo mock hoặc chạy scenario giả, bỏ qua phần đó theo yêu cầu hiện tại của người dùng. Chi tiết theme: [TOKENS.md](./TOKENS.md).

Account: giữ PageHeader chung (breadcrumb + tiêu đề/mô tả), chỉ bỏ phần giới thiệu riêng bị lặp trong trang. Đã khôi phục heading chung theo yêu cầu làm rõ; Orders dùng bộ lọc một hàng, detail gom trạng thái/giờ nhận và nút hủy gọn. Header cleanup does not mark other Account pages redesigned. See [ORDERS.md](./ORDERS.md) for verification and the pre-existing StaffInvitationsPage lint issue.

Refund follow-up: full-width management section and wide request modal with per-item selection/quantity/reason/evidence and sticky actions; lint/build pass, real section responsive checks pass. Current completed orders did not expose an eligible request, so request-form integration remains unverified; see ORDERS.md.

`/orders/:orderId/refund-request`: dedicated authenticated request page replaces request modal; real eligibility/read/retry/back states and submission guard; targeted lint/build pass, real eligible form/validation/cancel checks at 360/768/1280px documented in ORDERS.md; no uploads or refund writes.

Refund placement correction: receipt Refunds restored below Payment summary; standalone request page aligned to the customer frame, mobile footer/quantity wrapping fixed. Real eligible-order 360/768/1280px checks and lint/build pass; see ORDERS.md.

Payments/Refunds: completed four requested routes and all existing child sections. Approved dropdown format is now shared FilterBar and persisted for contributors. Active refund mutation/form branches unavailable in current data remain integration-unverified; see PAYMENTS-REFUNDS.md.

Reviews/Trust Score: complete requested pages and conditional children; inline/modal edit share one editor, membership hierarchy and approved dropdown filter format retained. Current account has no penalties; recovery/locked/highest-level branches reviewed but not forced. See REVIEWS-TRUST.md for checks and limits.

Compact Account follow-up: reduced finance/trust card padding, image/number sizes and redundant spacing; business pages use compact cards, shared feedback and confirmations. Real backend read-only checks at 360/768/1280px pass; unavailable mutation branches recorded in ACCOUNT-BUSINESS.md. New/edit shop-application form routes remain todo.
