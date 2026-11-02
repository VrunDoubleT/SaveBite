# UI redesign progress

Ngày audit: 2026-11-02 (Asia/Bangkok). Nhánh: ui-redesign (đã tồn tại).
Audit và bước tokens/primitives đã hoàn tất. Đã cập nhật theme, shared shell và UI reference; đã tạo mock backend theo DTO thật, redesign trang `/` và flash deal detail.
Chi tiết: [ROUTES.md](./ROUTES.md). Trang `/` và flash deal detail (hai đường dẫn) đã hoàn tất; các route còn lại giữ trạng thái todo. Redirect cần regression, không có page riêng.

| Route/task | Status | Commit | Notes |
|---|---|---|---|
| Shell/layout + shared components | done | ui(tokens): theme and primitives | Responsive shell; desktop collapse, mobile drawer, primitives; xem TOKENS.md |
| Tokens | done | ui(tokens): theme and primitives | Giữ 10 primary greens; màu phụ theo reference; xem TOKENS.md |
| Mock layer | done | chore(mock): frontend mock backend | MSW; typed DTO fixtures; session role/auth; realtime; search/filter/pagination; mutations + session persistence; xem MOCKS.md |
| `/` | done | ui(/): redesign | R1; CustomerHomePage; discovery, filters, cards, collapsible map, location/error/loading/empty/partial states; refined subtle color accents; 360/768/1280px; xem HOME.md |
| `/shops/:id` | todo | — | R2; CustomerShopPage; guest, user, staff, storeOwner (admin redirected) |
| `/shop-profile` | todo | — | R3; CustomerShopPage; guest, user, staff, storeOwner (admin redirected) |
| `/flash-deals/:id` | done | ui(/flash-deals/:id): redesign | R4; approved home accents; gallery 5 ảnh + 4 demo variants, title without green rule, variant/quantity/cart, pickup, all states; 360/768/1280px; xem DEAL-DETAIL.md |
| `/deals/:id` | done | ui(/flash-deals/:id): redesign | R5; alias cùng CustomerDealDetailPage; responsive/navigation regression passed; xem DEAL-DETAIL.md |
| `/login` | todo | — | R6; LoginPage; guest, user, staff, storeOwner (admin redirected) |
| `/register` | todo | — | R7; RegisterPage; guest, user, staff, storeOwner (admin redirected) |
| `/register/verify` | todo | — | R8; OtpVerificationPage; guest, user, staff, storeOwner (admin redirected) |
| `/forgot-password` | todo | — | R9; ForgotPasswordPage; guest, user, staff, storeOwner (admin redirected) |
| `/forgot-password/verify` | todo | — | R10; OtpVerificationPage; guest, user, staff, storeOwner (admin redirected) |
| `/customer` | todo | — | R11; Redirect regression |
| `/cart` | todo | — | R12; CartPage; user, staff, storeOwner |
| `/checkout/simulation` | todo | — | R13; CheckoutSimulationPage; user, staff, storeOwner |
| `/orders/:orderId` | todo | — | R14; OrderDetailPage; user, staff, storeOwner |
| `/checkout` | todo | — | R15; CheckoutPage; user, staff, storeOwner |
| `/account` | todo | — | R16; Redirect regression |
| `/account/profile` | todo | — | R17; ProfilePage; user, staff, storeOwner |
| `/account/orders` | todo | — | R18; OrdersPage; user, staff, storeOwner |
| `/account/payments` | todo | — | R19; PaymentsPage; user, staff, storeOwner |
| `/account/refunds` | todo | — | R20; RefundsPage; user, staff, storeOwner |
| `/account/refunds/:refundId` | todo | — | R21; CustomerRefundDetailPage; user, staff, storeOwner |
| `/account/payments/:paymentId` | todo | — | R22; PaymentDetailPage; user, staff, storeOwner |
| `/account/reviews` | todo | — | R23; ReviewsPage; user, staff, storeOwner |
| `/account/trust-score/:tab?` | todo | — | R24; TrustScorePage; user, staff, storeOwner |
| `/account/shop-application` | todo | — | R25; ShopApplicationPage; user, staff, storeOwner |
| `/account/shop-application/new` | todo | — | R26; ShopApplicationFormPage; user, staff, storeOwner |
| `/account/shop-application/edit/:applicationId` | todo | — | R27; ShopApplicationFormPage; user, staff, storeOwner |
| `/account/workspace` | todo | — | R28; WorkspacePage; user, staff, storeOwner |
| `/account/staff-invitations` | todo | — | R29; CustomerStaffInvitationsPage; user, staff, storeOwner |
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

Showcase `/__ui`: chỉ có trong DEV khi VITE_ENABLE_MOCK=true hoặc VITE_USE_MOCK=true. VITE_USE_MOCK đồng thời bật mock backend.

Chi tiết và cách xem: [TOKENS.md](./TOKENS.md). Chi tiết mock: [MOCKS.md](./MOCKS.md). Bước tiếp theo: ui-redesign-page khi người dùng yêu cầu (cần ui-ux-pro-max trước khi redesign từng trang).
