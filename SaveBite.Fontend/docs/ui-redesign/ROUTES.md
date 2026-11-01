# UI redesign audit — SaveBite

Ngày: 2026-11-02 (Asia/Bangkok). Phạm vi: `SaveBite.Fontend`. Chỉ đọc source; chỉ tạo ROUTES.md và PROGRESS.md. Nhánh hiện có: `ui-redesign`. Không chạy API, không thực hiện thanh toán hoặc mutation.

## Stack

- React 19 + TypeScript 6 + Vite 8; React Router DOM 7, `createBrowserRouter` trong src/app/router/AppRouter.tsx; cấu hình src/app/router/routes.tsx + paths.ts. Phiên bản theo package.json, không phải kiểm chứng runtime.
- Tailwind CSS v4 qua @tailwindcss/vite. Token hiện có trong src/index.css (@theme): primary xanh lá, nền trắng/sáng, accent cam, Inter/system fallback; trust còn có src/features/trust/styles/trust.css.
- Axios 1; gọi service/hook bằng useEffect/useCallback, không có TanStack Query trong dependencies. Zustand 5 cho auth/cart/notification/toast/UI; React Hook Form + Zod cho form. SignalR 8 cho realtime.
- npm (package-lock.json). Chạy: `cd SaveBite.Fontend && npm run dev`. Build: `npm run build`; lint: `npm run lint`. Có Playwright cho products và các test Node chuyên biệt. Audit không cần chạy build/test vì không đổi source.
- Copy đang chủ yếu tiếng Anh, một số nội dung tiếng Việt; giữ nguyên ngôn ngữ từng màn hình.

## API client và điểm tích hợp mock

- src/shared/api/httpClient.ts: Axios httpClient, baseURL `VITE_API_URL ?? "/api"`, timeout 15s, withCredentials, JSON; bearer lấy từ localStorage. refreshClient riêng, đồng bộ request renew token; retry 401 một lần, ngoại lệ login; phát auth:unauthorized khi hết phiên.
- src/features/auth/api/authApi.ts: getMe chuẩn hóa role và customerStatus. AppProvider gọi initializeSession; authStore là Zustand. Mock cần phục vụ session/me/renewal đúng contract để guard hoạt động, giữ nguyên flow auth.
- src/shared/types/api.types.ts: ApiResponse<T> = { data, message?, success }; ApiErrorResponse = { message, errorCode?, errors?, statusCode? }. Không áp dụng một pagination chung: từng feature có DTO riêng (đặc biệt ShopOrderPage.page được validate).
- src/shared/api/endpoints.ts chỉ là hằng URL; một số service dùng URL inline (refunds/payments/analytics...). Bảng service ở cuối liệt kê lời gọi HTTP thực tế trong source. URL function/template giữ nguyên biểu thức để thấy params, encodeURIComponent và nhánh runtime. Các đường dẫn tương đối được ghép với baseURL.
- Đề xuất bước mock: ưu tiên intercept ở tầng network (MSW nếu bước sau được phép thêm dependency); nếu adapter Axios thì phải bao phủ cả httpClient và refreshClient, giữ envelope/errors/upload. Hiện chưa thiết lập mock mode trong audit này.
- src/app/providers/RealtimeProvider.tsx quản lý SignalR theo phiên; src/shared/realtime/realtimeClient.ts nhận ReceiveEvent. Sự kiện: shop.orders.pending.changed, customer.orders.changed, shop.orders.changed, cart.changed, notification.received/read/deleted. HTTP mock riêng không mô phỏng được SignalR; cần transport/event fixture cho bước mock.
- Ngoại dịch vụ: VietMap route/style/reverse, OSM tiles, VietQR banks, ảnh remote. Cần fixture/fallback cho bản đồ/geolocation/bank/payment QR nếu muốn frontend xem được hoàn toàn offline. Không ghi giá trị key hoặc .env vào tài liệu.

## Quyền và layout

- CustomerRoute: guest được xem public; admin đăng nhập được điều hướng về admin. Protected customer chấp nhận user/staff/storeOwner; thiếu phiên về login.
- Admin: admin. Store owner: storeOwner. Staff: staff hoặc storeOwner; truy cập shop cụ thể vẫn phụ thuộc quyền backend và dữ liệu shop. Không suy diễn quyền từ sidebar.
- CustomerLayout → CustomerHeader; AccountLayout lồng trong CustomerLayout → AccountSidebar. AdminLayout/StaffLayout/StoreOwnerLayout dùng WorkspaceLayout → WorkspaceHeader + WorkspaceSidebar. ProductsWorkspace bổ sung options/shop/categories qua Outlet context, cùng loading/error/no-shop gate.
- API dùng chung ngoài bảng route: session auth/me, cart summary, notifications và pending-order badges qua header/layout/store. Phải mock các dữ liệu nền này cho từng role.

## Routes

Kiểm kê: **75 route leaf/index: 70 route nội dung + 5 redirect** (không phải 70 component duy nhất; có alias và component dùng chung).

Các hàng dưới đây là route leaf/index khai báo thật, bao gồm alias và redirect. Các layout pathless và layout cha được mô tả phía trên. Params nằm trong path; query/interactions có trong chi tiết.

| # | Path | Page file/component | Layout | Roles | API/hook và dữ liệu |
|---|---|---|---|---|---|
| 1 | `/` | `src/pages/customer/CustomerHomePage.tsx` | CustomerLayout | guest, user, staff, storeOwner (admin redirected) | `flashDealApi.getNearbyDeals`, `shopApi.getNearbyShops` |
| 2 | `/shops/:id` | `src/pages/customer/CustomerShopPage.tsx` | CustomerLayout | guest, user, staff, storeOwner (admin redirected) | Không tìm thấy service HTTP trực tiếp trong dependency đọc được |
| 3 | `/shop-profile` | `src/pages/customer/CustomerShopPage.tsx` | CustomerLayout | guest, user, staff, storeOwner (admin redirected) | Không tìm thấy service HTTP trực tiếp trong dependency đọc được |
| 4 | `/flash-deals/:id` | `src/pages/customer/CustomerDealDetailPage.tsx` | CustomerLayout | guest, user, staff, storeOwner (admin redirected) | `cartApi.addItem`, `cartApi.getPage`, `cartApi.removeItem`, `cartApi.summary`, `cartApi.updateQuantity` |
| 5 | `/deals/:id` | `src/pages/customer/CustomerDealDetailPage.tsx` | CustomerLayout | guest, user, staff, storeOwner (admin redirected) | `cartApi.addItem`, `cartApi.getPage`, `cartApi.removeItem`, `cartApi.summary`, `cartApi.updateQuantity` |
| 6 | `/login` | `src/pages/auth/LoginPage.tsx` | CustomerLayout | guest, user, staff, storeOwner (admin redirected) | `authApi.getMe`, `authApi.login` |
| 7 | `/register` | `src/pages/auth/RegisterPage.tsx` | CustomerLayout | guest, user, staff, storeOwner (admin redirected) | `authApi.register` |
| 8 | `/register/verify` | `src/pages/auth/OtpVerificationPage.tsx` | CustomerLayout | guest, user, staff, storeOwner (admin redirected) | `authApi.resetPassword`, `authApi.verifyRegistration` |
| 9 | `/forgot-password` | `src/pages/auth/ForgotPasswordPage.tsx` | CustomerLayout | guest, user, staff, storeOwner (admin redirected) | `authApi.forgotPassword` |
| 10 | `/forgot-password/verify` | `src/pages/auth/OtpVerificationPage.tsx` | CustomerLayout | guest, user, staff, storeOwner (admin redirected) | `authApi.resetPassword`, `authApi.verifyRegistration` |
| 11 | `/customer` | `Navigate` | CustomerLayout | user, staff, storeOwner | Redirect; xem chi tiết |
| 12 | `/cart` | `src/pages/customer/CartPage.tsx` | CustomerLayout | user, staff, storeOwner | `cartApi.addItem`, `cartApi.getPage`, `cartApi.removeItem`, `cartApi.summary`, `cartApi.updateQuantity` |
| 13 | `/checkout/simulation` | `src/pages/customer/CheckoutSimulationPage.tsx` | CustomerLayout | user, staff, storeOwner | `paymentApi.simulate` |
| 14 | `/orders/:orderId` | `src/pages/customer/OrderDetailPage.tsx` | CustomerLayout | user, staff, storeOwner | `cartApi.addItem`, `cartApi.getPage`, `cartApi.removeItem`, `cartApi.summary`, `cartApi.updateQuantity`, `feedbackApi.addFeedback`, `feedbackApi.editFeedback`, `feedbackApi.uploadImage`, `orderApi.cancel`, `orderApi.order`, `paymentApi.payment`, `refundApi.confirm`, `refundApi.destination`, `refundApi.forOrder`, `refundApi.request`, `refundApi.upload`, `refundApi.withdraw` |
| 15 | `/checkout` | `src/pages/customer/CheckoutPage.tsx` | CustomerLayout | user, staff, storeOwner | `cartApi.addItem`, `cartApi.getPage`, `cartApi.removeItem`, `cartApi.summary`, `cartApi.updateQuantity`, `checkoutApi.context`, `checkoutApi.place`, `checkoutApi.quote`, `feedbackApi.addFeedback`, `feedbackApi.editFeedback`, `feedbackApi.uploadImage`, `orderApi.cancel`, `orderApi.order`, `paymentApi.payment`, `refundApi.confirm`, `refundApi.destination`, `refundApi.forOrder`, `refundApi.request`, `refundApi.upload`, `refundApi.withdraw` |
| 16 | `/account` | `Navigate` | CustomerLayout → AccountLayout | user, staff, storeOwner | Redirect; xem chi tiết |
| 17 | `/account/profile` | `src/pages/customer/ProfilePage.tsx` | CustomerLayout → AccountLayout | user, staff, storeOwner | `addressApi.createAddress`, `addressApi.deleteAddress`, `addressApi.getAddresses`, `addressApi.setDefaultAddress`, `addressApi.updateAddress`, `authApi.changePassword`, `profileApi.updateProfile`, `profileApi.uploadAvatar` |
| 18 | `/account/orders` | `src/pages/customer/OrdersPage.tsx` | CustomerLayout → AccountLayout | user, staff, storeOwner | `feedbackApi.addFeedback`, `feedbackApi.editFeedback`, `feedbackApi.uploadImage`, `orderApi.orderPage` |
| 19 | `/account/payments` | `src/pages/customer/PaymentsPage.tsx` | CustomerLayout → AccountLayout | user, staff, storeOwner | `paymentApi.page` |
| 20 | `/account/refunds` | `src/pages/customer/RefundsPage.tsx` | CustomerLayout → AccountLayout | user, staff, storeOwner | Không tìm thấy service HTTP trực tiếp trong dependency đọc được |
| 21 | `/account/refunds/:refundId` | `src/pages/customer/RefundDetailPage.tsx` | CustomerLayout → AccountLayout | user, staff, storeOwner | `refundApi.confirm`, `refundApi.destination`, `refundApi.upload`, `refundApi.withdraw` |
| 22 | `/account/payments/:paymentId` | `src/pages/customer/PaymentDetailPage.tsx` | CustomerLayout → AccountLayout | user, staff, storeOwner | `paymentApi.detail` |
| 23 | `/account/reviews` | `src/pages/customer/ReviewsPage.tsx` | CustomerLayout → AccountLayout | user, staff, storeOwner | `feedbackApi.deleteFeedback`, `feedbackApi.editFeedback`, `feedbackApi.getHistory`, `feedbackApi.uploadImage` |
| 24 | `/account/trust-score/:tab?` | `src/pages/customer/TrustScorePage.tsx` | CustomerLayout → AccountLayout | user, staff, storeOwner | `trustApi.getHistory`, `trustApi.getLevelProgress`, `trustApi.getLevels`, `trustApi.getOverview`, `trustApi.getPenalties`, `trustApi.getPenaltyRecovery` |
| 25 | `/account/shop-application` | `src/pages/customer/ShopApplicationPage.tsx` | CustomerLayout → AccountLayout | user, staff, storeOwner | `shopApplicationApi.cancel`, `shopApplicationApi.getMine` |
| 26 | `/account/shop-application/new` | `src/pages/customer/ShopApplicationFormPage.tsx` | CustomerLayout → AccountLayout | user, staff, storeOwner | `shopApplicationApi.create`, `shopApplicationApi.resubmit` |
| 27 | `/account/shop-application/edit/:applicationId` | `src/pages/customer/ShopApplicationFormPage.tsx` | CustomerLayout → AccountLayout | user, staff, storeOwner | `shopApplicationApi.create`, `shopApplicationApi.resubmit` |
| 28 | `/account/workspace` | `src/pages/customer/WorkspacePage.tsx` | CustomerLayout → AccountLayout | user, staff, storeOwner | `staffApi.getAssociatedShops`, `staffApi.leaveShop` |
| 29 | `/account/staff-invitations` | `src/pages/customer/StaffInvitationsPage.tsx` | CustomerLayout → AccountLayout | user, staff, storeOwner | `staffInvitationApi.acceptInvitation`, `staffInvitationApi.declineInvitation`, `staffInvitationApi.getMyInvitations` |
| 30 | `/admin` | `Navigate` | AdminLayout | admin | Redirect; xem chi tiết |
| 31 | `/admin/analytics/:tab?` | `src/pages/admin/AdminDashboardPage.tsx` | AdminLayout | admin | `platformAnalyticsApi.getDashboard` |
| 32 | `/admin/users` | `src/pages/admin/UserManagementPage.tsx` | AdminLayout | admin | `userApi.getUserDetails`, `userApi.getUsers`, `userApi.updateStatus` |
| 33 | `/admin/categories` | `src/pages/admin/CategoryManagementPage.tsx` | AdminLayout | admin | Không tìm thấy service HTTP trực tiếp trong dependency đọc được |
| 34 | `/admin/trust/:tab?` | `src/pages/admin/TrustConfigurationPage.tsx` | AdminLayout | admin | `adminTrustApi.createLevel`, `adminTrustApi.createPenaltyRule`, `adminTrustApi.deletePenaltyRule`, `adminTrustApi.getEarningRules`, `adminTrustApi.getLevelHistory`, `adminTrustApi.getLevels`, `adminTrustApi.getPenaltyRules`, `adminTrustApi.getScoreRules`, `adminTrustApi.updateEarningRule`, `adminTrustApi.updateLevelBenefits`, `adminTrustApi.updateLevelInfo`, `adminTrustApi.updateLevelRequirement`, `adminTrustApi.updateLevelStatus`, `adminTrustApi.updatePenaltyRule`, `adminTrustApi.updateScoreRule` |
| 35 | `/admin/products` | `src/pages/admin/ProductManagementPage.tsx` | AdminLayout | admin | Không tìm thấy service HTTP trực tiếp trong dependency đọc được |
| 36 | `/admin/products/:productId` | `src/pages/admin/ProductContentManagementPage.tsx` | AdminLayout | admin | `productApi.approveContent`, `productApi.removeForViolation`, `warningApi.issueForProduct` |
| 37 | `/admin/orders` | `src/pages/commons/ComingSoonPage.tsx` | AdminLayout | admin | Không tìm thấy service HTTP trực tiếp trong dependency đọc được |
| 38 | `/admin/stores` | `src/pages/admin/ShopManagementPage.tsx` | AdminLayout | admin | Không tìm thấy service HTTP trực tiếp trong dependency đọc được |
| 39 | `/admin/stores/:shopId` | `src/pages/admin/ShopContentManagementPage.tsx` | AdminLayout | admin | `shopApi.updateModerationStatus` |
| 40 | `/admin/shop-applications` | `src/pages/admin/AdminShopApplicationsPage.tsx` | AdminLayout | admin | `adminShopApplicationApi.getAll`, `adminShopApplicationApi.getById`, `adminShopApplicationApi.review` |
| 41 | `/admin/platform-fees` | `src/pages/admin/PlatformFeeManagementPage.tsx` | AdminLayout | admin | `platformFeeApi.generateMonthly`, `platformFeeApi.getActiveConfig`, `platformFeeApi.getAdminStatements`, `platformFeeApi.getAllConfigs`, `platformFeeApi.processOverdue`, `platformFeeApi.recordExternalPayment`, `platformFeeApi.updateConfig` |
| 42 | `/admin/feedback` | `src/pages/admin/FeedbackManagementPage.tsx` | AdminLayout | admin | `feedbackApi.remove`, `feedbackApi.restore` |
| 43 | `/admin/reviews` | `src/pages/admin/AdminReviewsPage.tsx` | AdminLayout | admin | `feedbackApi.deleteReply`, `feedbackApi.editReply`, `feedbackApi.getAdminFeedbacks`, `feedbackApi.replyFeedback` |
| 44 | `/admin/audit-logs` | `src/pages/admin/AdminAuditLogsPage.tsx` | AdminLayout | admin | `auditLogApi.getLogs` |
| 45 | `/staff` | `Navigate` | — | staff, storeOwner | Redirect; xem chi tiết |
| 46 | `/staff/shops/:shopId` | `Navigate` | StaffLayout | staff, storeOwner | Redirect; xem chi tiết |
| 47 | `/staff/shops/:shopId/flash-deals` | `src/pages/staff/StaffFlashDealsPage.tsx` | StaffLayout | staff, storeOwner | `flashDealApi.getShopFlashDeals` |
| 48 | `/staff/shops/:shopId/flash-deals/:id` | `src/pages/staff/StaffFlashDealDetailPage.tsx` | StaffLayout | staff, storeOwner | `flashDealApi.getShopFlashDealDetail` |
| 49 | `/staff/shops/:shopId/reviews` | `src/pages/store-owner/StoreOwnerReviewsPage.tsx` | StaffLayout | staff, storeOwner | `feedbackApi.deleteReply`, `feedbackApi.editReply`, `feedbackApi.getShopFeedbacks`, `feedbackApi.replyFeedback` |
| 50 | `/staff/shops/:shopId/orders` | `src/pages/shop/ShopOrdersPage.tsx` | StaffLayout | staff, storeOwner | `shopOrderApi.page` |
| 51 | `/staff/shops/:shopId/orders/:orderId` | `src/pages/shop/ShopOrderDetailPage.tsx` | StaffLayout | staff, storeOwner | `refundApi.qr`, `refundApi.review`, `refundApi.transfer`, `refundApi.upload`, `shopOrderApi.action`, `shopOrderApi.detail`, `shopOrderApi.simulateRemaining` |
| 52 | `/staff/shops/:shopId/info` | `src/pages/staff/StaffShopDetailPage.tsx` | StaffLayout | staff, storeOwner | `staffApi.getShopDetail` |
| 53 | `/store-owner` | `src/pages/store-owner/StoreOwnerDashboardPage.tsx` | StoreOwnerLayout | storeOwner | `storeAnalyticsApi.getDashboard` |
| 54 | `/store-owner/products` | `src/features/products/pages/ProductListPage.tsx` | StoreOwnerLayout → ProductsWorkspace | storeOwner | `productApi.list`, `productApi.options`, `productApi.summary` |
| 55 | `/store-owner/products/new` | `src/features/products/pages/CreateProductPage.tsx` | StoreOwnerLayout → ProductsWorkspace | storeOwner | `productApi.create`, `productApi.options` |
| 56 | `/store-owner/products/:productId` | `src/features/products/pages/ProductDetailPage.tsx` | StoreOwnerLayout → ProductsWorkspace | storeOwner | `productApi.addValues`, `productApi.deals`, `productApi.get`, `productApi.images`, `productApi.options`, `productApi.remove`, `productApi.update`, `productApi.updateStatus`, `productApi.updateVariant`, `productApi.valueImage` |
| 57 | `/store-owner/products/:productId/flash-deals` | `src/features/products/pages/ProductHistoryPage.tsx` | StoreOwnerLayout → ProductsWorkspace | storeOwner | `productApi.deals`, `productApi.options` |
| 58 | `/store-owner/orders` | `src/pages/shop/ShopOrdersPage.tsx` | StoreOwnerLayout | storeOwner | `shopOrderApi.page` |
| 59 | `/store-owner/orders/:orderId` | `src/pages/shop/ShopOrderDetailPage.tsx` | StoreOwnerLayout | storeOwner | `refundApi.qr`, `refundApi.review`, `refundApi.transfer`, `refundApi.upload`, `shopOrderApi.action`, `shopOrderApi.detail`, `shopOrderApi.simulateRemaining` |
| 60 | `/store-owner/refunds` | `src/pages/store-owner/RefundsPage.tsx` | StoreOwnerLayout | storeOwner | Không tìm thấy service HTTP trực tiếp trong dependency đọc được |
| 61 | `/store-owner/refunds/:refundId` | `src/pages/store-owner/RefundDetailPage.tsx` | StoreOwnerLayout | storeOwner | `refundApi.qr`, `refundApi.review`, `refundApi.transfer`, `refundApi.upload` |
| 62 | `/store-owner/flash-deals` | `src/pages/store-owner/StoreOwnerFlashDealsPage.tsx` | StoreOwnerLayout | storeOwner | `flashDealApi.cancelFlashDeal`, `flashDealApi.getShopFlashDeals`, `flashDealApi.hideFlashDeal`, `flashDealApi.unhideFlashDeal` |
| 63 | `/store-owner/flash-deals/create` | `src/pages/store-owner/StoreOwnerFlashDealFormPage.tsx` | StoreOwnerLayout | storeOwner | `flashDealApi.createFlashDeal`, `flashDealApi.getEligibleProducts`, `flashDealApi.getShopFlashDealDetail`, `flashDealApi.updateFlashDeal` |
| 64 | `/store-owner/flash-deals/:id` | `src/pages/store-owner/StoreOwnerFlashDealDetailPage.tsx` | StoreOwnerLayout | storeOwner | `flashDealApi.cancelFlashDeal`, `flashDealApi.getShopFlashDealDetail`, `flashDealApi.hideFlashDeal`, `flashDealApi.unhideFlashDeal` |
| 65 | `/store-owner/flash-deals/:id/edit` | `src/pages/store-owner/StoreOwnerFlashDealFormPage.tsx` | StoreOwnerLayout | storeOwner | `flashDealApi.createFlashDeal`, `flashDealApi.getEligibleProducts`, `flashDealApi.getShopFlashDealDetail`, `flashDealApi.updateFlashDeal` |
| 66 | `/store-owner/staff` | `src/pages/store-owner/StaffManagementPage.tsx` | StoreOwnerLayout | storeOwner | `shopStaffApi.getStaffList`, `shopStaffApi.inviteStaff`, `shopStaffApi.removeStaff`, `shopStaffApi.searchStaffCandidates`, `shopStaffApi.updateStaff` |
| 67 | `/store-owner/staff/invitations` | `src/pages/store-owner/StaffInvitationsPage.tsx` | StoreOwnerLayout | storeOwner | `shopStaffApi.getInvitations`, `shopStaffApi.revokeInvitation` |
| 68 | `/store-owner/staff/:staffId` | `src/pages/store-owner/StaffDetailPage.tsx` | StoreOwnerLayout | storeOwner | `shopStaffApi.getStaffActivityLogs` |
| 69 | `/store-owner/payment-settings` | `src/pages/store-owner/PaymentSettingsPage.tsx` | StoreOwnerLayout | storeOwner | `ownerShopApi.getMyPaymentConfig`, `ownerShopApi.updateMyPaymentConfig` |
| 70 | `/store-owner/platform-fees` | `src/pages/store-owner/PlatformFeesPage.tsx` | StoreOwnerLayout | storeOwner | `ownerShopApi.getMyShopProfile`, `platformFeeApi.createPaymentLink`, `platformFeeApi.getShopStatements`, `platformFeeApi.simulateWebhook` |
| 71 | `/store-owner/platform-fees/:statementId` | `src/pages/store-owner/PlatformFeeDetailPage.tsx` | StoreOwnerLayout | storeOwner | `ownerShopApi.getMyShopProfile`, `platformFeeApi.createPaymentLink`, `platformFeeApi.getStatementDetail`, `platformFeeApi.simulateWebhook` |
| 72 | `/store-owner/profile` | `src/pages/store-owner/ShopProfilePage.tsx` | StoreOwnerLayout | storeOwner | `ownerShopApi.getMyShopProfile`, `ownerShopApi.updateBusinessInfo`, `ownerShopApi.updateImages` |
| 73 | `/store-owner/warnings` | `src/features/warnings/pages/WarningHistoryPage.tsx` | StoreOwnerLayout | storeOwner | `productApi.get` |
| 74 | `/store-owner/reviews` | `src/pages/store-owner/StoreOwnerReviewsPage.tsx` | StoreOwnerLayout | storeOwner | `feedbackApi.deleteReply`, `feedbackApi.editReply`, `feedbackApi.getShopFeedbacks`, `feedbackApi.replyFeedback` |
| 75 | `*` | `src/pages/commons/NotFoundPage.tsx` | — | public | Không tìm thấy service HTTP trực tiếp trong dependency đọc được |

## Chi tiết dữ liệu và tương tác theo route

Phương pháp: đọc AST TypeScript của route và dependency local (page → child/hook/store/service). API liệt kê là các method được tham chiếu trong module dependency, gồm các thao tác điều kiện/modal; không có nghĩa tất cả gọi ngay khi mount. Shared barrel hoặc component chứa nhiều nhánh có thể tạo tập bao phủ rộng hơn nhánh đang render. Không chạy server nên quyền backend và response runtime chưa được xác nhận.

“Field đọc trong JSX” là biểu thức được source dùng ở page/child (gồm DTO, dữ liệu tính toán, props và state), không phải danh sách field API mới. Các field đã destructure có thể xuất hiện dưới tên biến; dùng DTO nguồn được dẫn để xác nhận khi tạo fixture. Không suy diễn field API từ tên UI.

### R1. /

- Page: `src/pages/customer/CustomerHomePage.tsx`. Layout: CustomerLayout. Quyền: guest, user, staff, storeOwner (admin redirected).
- Service được tham chiếu: `flashDealApi.getNearbyDeals`, `shopApi.getNearbyShops`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/categories/types/category.types.ts` (`Category`); `src/features/flash-deals/types/flashDeal.types.ts` (`DealAttributeGroup`, `FlashDealVariant`, `FlashDeal`, `CursorMode`, `FlashDealCursorParams`, `FlashDealCursorResult`, `DealFilterState`, `FlashDealStatusEnum`, `FlashDealVariantStatusEnum`, `ShopFlashDealListItem`, `ShopFlashDealVariantDetail`, `ShopFlashDealDetail`, `ShopEligibleProductVariant`, `ShopEligibleProduct`, `CreateFlashDealVariantPayload`, `CreateFlashDealPayload`, `UpdateFlashDealVariantPayload`, `UpdateFlashDealPayload`, `ShopFlashDealQueryParams`); `src/features/shops/types/ownerShop.types.ts` (`ShopStatus`, `ShopProfileDocument`, `OwnerShop`, `UpdateBusinessInfo`, `ShopPaymentConfig`, `UpdateShopPaymentConfigRequest`); `src/features/shops/types/shop.types.ts` (`NearbyShop`, `ShopProfile`, `StoreReviewReply`, `StoreReview`, `StoreReviewsSummary`, `NearbyShopsPagedResponse`, `NearbyShopsRequest`, `StoreReviewsQueryRequest`); `src/features/shops/types/shopManagement.types.ts` (`ShopOwnerManagement`, `ShopListManagement`, `ShopProfileDocument`, `ShopStatusHistory`, `ShopContentManagement`, `UpdateShopModerationStatus`); `src/shared/types/index.ts` (); `src/shared/types/moderationAction.types.ts` (`ModerationActionResult`).
- Hook/state/router: `useAuthStore`, `useAuthStore.getState`, `useAuthStore.getState().refreshUser`, `useDebounce`, `useNearbyFlashDeals`.
- Child component: `src/features/flash-deals/components/FilterSidebar.tsx`, `src/features/flash-deals/components/DealSection.tsx`, `src/features/flash-deals/components/DealGrid.tsx`, `src/features/flash-deals/components/DealCard.tsx`, `src/features/shops/components/NearbyStoresMapSection.tsx`.
- Tương tác source: `onApplyNewDeals`, `onChange`, `onClearSearch`, `onClearShop`, `onClick`, `onError`, `onLoadOlder`, `onRefresh`, `onRequestGps`, `onSearchChange`, `onSelectDefaultLocation`, `onSelectSavedAddress`.
- Field đọc trong JSX — `src/pages/customer/CustomerHomePage.tsx`: `coords?.latitude`, `coords?.longitude`, `filters.category`, `filters.category.length`, `searchKeyword.trim().length`, `userDefaultAddress.label`, `userDefaultAddress?.addressLine`, `userDefaultAddress?.label`.
- Field đọc trong JSX — `src/features/flash-deals/components/FilterSidebar.tsx`: `categoryList.length`, `filters.category`, `filters.category.length`, `filters.distance`, `filters.price`.
- Field đọc trong JSX — `src/features/flash-deals/components/DealSection.tsx`: `deals.length`, `e.target`.
- Field đọc trong JSX — `src/features/flash-deals/components/DealGrid.tsx`: `deal.id`.
- Field đọc trong JSX — `src/features/flash-deals/components/DealCard.tsx`: `deal.categoryName`, `deal.distanceInKm`, `deal.id`, `deal.isNew`, `deal.isPurchasable`, `deal.maxDiscountPercent`, `deal.maxOriginalPrice`, `deal.minDealPrice`, `deal.productImageUrl`, `deal.productName`, `deal.shopId`, `deal.shopName`, `deal.variants`, `deal.variants.length`, `e.currentTarget`, `e.currentTarget.src`.
- Field đọc trong JSX — `src/features/shops/components/NearbyStoresMapSection.tsx`: `e.currentTarget`, `e.currentTarget.src`, `routeResult.distanceFormatted`, `routeResult.durationMinutes`, `routeResult.instructions`, `routeResult.instructions.length`, `selectedShop.address`, `selectedShop.averageRating`, `selectedShop.closingTime`, `selectedShop.coverImageUrl`, `selectedShop.distanceInKm`, `selectedShop.flashDealCount`, `selectedShop.id`, `selectedShop.isOpen`, `selectedShop.logoUrl`, `selectedShop.name`, `selectedShop.openingTime`, `selectedShop.totalReviews`.
- Nhóm: list/workspace. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R2. /shops/:id

- Page: `src/pages/customer/CustomerShopPage.tsx`. Layout: CustomerLayout. Quyền: guest, user, staff, storeOwner (admin redirected).
- Service được tham chiếu: Không có service HTTP được phát hiện. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/flash-deals/types/flashDeal.types.ts` (`DealAttributeGroup`, `FlashDealVariant`, `FlashDeal`, `CursorMode`, `FlashDealCursorParams`, `FlashDealCursorResult`, `DealFilterState`, `FlashDealStatusEnum`, `FlashDealVariantStatusEnum`, `ShopFlashDealListItem`, `ShopFlashDealVariantDetail`, `ShopFlashDealDetail`, `ShopEligibleProductVariant`, `ShopEligibleProduct`, `CreateFlashDealVariantPayload`, `CreateFlashDealPayload`, `UpdateFlashDealVariantPayload`, `UpdateFlashDealPayload`, `ShopFlashDealQueryParams`); `src/features/shops/types/ownerShop.types.ts` (`ShopStatus`, `ShopProfileDocument`, `OwnerShop`, `UpdateBusinessInfo`, `ShopPaymentConfig`, `UpdateShopPaymentConfigRequest`); `src/features/shops/types/shop.types.ts` (`NearbyShop`, `ShopProfile`, `StoreReviewReply`, `StoreReview`, `StoreReviewsSummary`, `NearbyShopsPagedResponse`, `NearbyShopsRequest`, `StoreReviewsQueryRequest`); `src/features/shops/types/shopManagement.types.ts` (`ShopOwnerManagement`, `ShopListManagement`, `ShopProfileDocument`, `ShopStatusHistory`, `ShopContentManagement`, `UpdateShopModerationStatus`); `src/shared/types/index.ts` (); `src/shared/types/moderationAction.types.ts` (`ModerationActionResult`).
- Hook/state/router: `useNavigate`, `usePagination`, `useParams`.
- Child component: `src/features/flash-deals/components/ShopDealCard.tsx`, `src/features/shops/components/StoreReviewList.tsx`, `src/shared/components/pagination/index.ts`.
- Tương tác source: `onAddToCart`, `onClick`, `onError`, `onHasImagesFilterChange`, `onPageChange`, `onRatingFilterChange`, `onSelectImage`.
- Field đọc trong JSX — `src/pages/customer/CustomerShopPage.tsx`: `deal.id`, `deals.length`, `deals[0]?.description`, `e.currentTarget`, `e.currentTarget.src`, `e.currentTarget.style`, `e.currentTarget.style.display`, `profile?.description`, `profile?.isOpen`, `reviewsSummary?.totalReviews`.
- Field đọc trong JSX — `src/features/flash-deals/components/ShopDealCard.tsx`: `deal.categoryName`, `deal.id`, `deal.isPurchasable`, `deal.maxDiscountPercent`, `deal.maxOriginalPrice`, `deal.minDealPrice`, `deal.productImageUrl`, `deal.productName`, `deal.variants`, `deal.variants.length`, `e.currentTarget`, `e.currentTarget.src`.
- Field đọc trong JSX — `src/features/shops/components/StoreReviewList.tsx`: `e.currentTarget`, `e.currentTarget.src`, `filteredReviews.length`, `rev.id`, `review.comment`, `review.images`, `review.images.length`, `review.productImageUrl`, `review.productName`, `review.rating`, `review.reply`, `review.reply.content`, `review.userAvatarUrl`, `review.userFullName`, `review.userTier`, `review.variantName`.
- Nhóm: detail/tabs. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R3. /shop-profile

- Page: `src/pages/customer/CustomerShopPage.tsx`. Layout: CustomerLayout. Quyền: guest, user, staff, storeOwner (admin redirected).
- Service được tham chiếu: Không có service HTTP được phát hiện. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/flash-deals/types/flashDeal.types.ts` (`DealAttributeGroup`, `FlashDealVariant`, `FlashDeal`, `CursorMode`, `FlashDealCursorParams`, `FlashDealCursorResult`, `DealFilterState`, `FlashDealStatusEnum`, `FlashDealVariantStatusEnum`, `ShopFlashDealListItem`, `ShopFlashDealVariantDetail`, `ShopFlashDealDetail`, `ShopEligibleProductVariant`, `ShopEligibleProduct`, `CreateFlashDealVariantPayload`, `CreateFlashDealPayload`, `UpdateFlashDealVariantPayload`, `UpdateFlashDealPayload`, `ShopFlashDealQueryParams`); `src/features/shops/types/ownerShop.types.ts` (`ShopStatus`, `ShopProfileDocument`, `OwnerShop`, `UpdateBusinessInfo`, `ShopPaymentConfig`, `UpdateShopPaymentConfigRequest`); `src/features/shops/types/shop.types.ts` (`NearbyShop`, `ShopProfile`, `StoreReviewReply`, `StoreReview`, `StoreReviewsSummary`, `NearbyShopsPagedResponse`, `NearbyShopsRequest`, `StoreReviewsQueryRequest`); `src/features/shops/types/shopManagement.types.ts` (`ShopOwnerManagement`, `ShopListManagement`, `ShopProfileDocument`, `ShopStatusHistory`, `ShopContentManagement`, `UpdateShopModerationStatus`); `src/shared/types/index.ts` (); `src/shared/types/moderationAction.types.ts` (`ModerationActionResult`).
- Hook/state/router: `useNavigate`, `usePagination`, `useParams`.
- Child component: `src/features/flash-deals/components/ShopDealCard.tsx`, `src/features/shops/components/StoreReviewList.tsx`, `src/shared/components/pagination/index.ts`.
- Tương tác source: `onAddToCart`, `onClick`, `onError`, `onHasImagesFilterChange`, `onPageChange`, `onRatingFilterChange`, `onSelectImage`.
- Field đọc trong JSX — `src/pages/customer/CustomerShopPage.tsx`: `deal.id`, `deals.length`, `deals[0]?.description`, `e.currentTarget`, `e.currentTarget.src`, `e.currentTarget.style`, `e.currentTarget.style.display`, `profile?.description`, `profile?.isOpen`, `reviewsSummary?.totalReviews`.
- Field đọc trong JSX — `src/features/flash-deals/components/ShopDealCard.tsx`: `deal.categoryName`, `deal.id`, `deal.isPurchasable`, `deal.maxDiscountPercent`, `deal.maxOriginalPrice`, `deal.minDealPrice`, `deal.productImageUrl`, `deal.productName`, `deal.variants`, `deal.variants.length`, `e.currentTarget`, `e.currentTarget.src`.
- Field đọc trong JSX — `src/features/shops/components/StoreReviewList.tsx`: `e.currentTarget`, `e.currentTarget.src`, `filteredReviews.length`, `rev.id`, `review.comment`, `review.images`, `review.images.length`, `review.productImageUrl`, `review.productName`, `review.rating`, `review.reply`, `review.reply.content`, `review.userAvatarUrl`, `review.userFullName`, `review.userTier`, `review.variantName`.
- Nhóm: form/settings. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R4. /flash-deals/:id

- Page: `src/pages/customer/CustomerDealDetailPage.tsx`. Layout: CustomerLayout. Quyền: guest, user, staff, storeOwner (admin redirected).
- Service được tham chiếu: `cartApi.addItem`, `cartApi.getPage`, `cartApi.removeItem`, `cartApi.summary`, `cartApi.updateQuantity`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/cart/types/cart.types.ts` (`CartItem`, `CartShop`, `Cart`, `CursorPagedResult`, `CartSummary`, `CartPage`, `CartMutationResponse`); `src/features/flash-deals/types/flashDeal.types.ts` (`DealAttributeGroup`, `FlashDealVariant`, `FlashDeal`, `CursorMode`, `FlashDealCursorParams`, `FlashDealCursorResult`, `DealFilterState`, `FlashDealStatusEnum`, `FlashDealVariantStatusEnum`, `ShopFlashDealListItem`, `ShopFlashDealVariantDetail`, `ShopFlashDealDetail`, `ShopEligibleProductVariant`, `ShopEligibleProduct`, `CreateFlashDealVariantPayload`, `CreateFlashDealPayload`, `UpdateFlashDealVariantPayload`, `UpdateFlashDealPayload`, `ShopFlashDealQueryParams`); `src/shared/types/index.ts` ().
- Hook/state/router: `useAuthStore.getState`, `useAuthStore.subscribe`, `useCartStore`, `useCartStore.setState`, `useNavigate`, `useParams`, `useToastStore.getState`, `useToastStore.getState().removeToast`.
- Child component: Inline trong page.
- Tương tác source: `onClick`, `onError`.
- Field đọc trong JSX — `src/pages/customer/CustomerDealDetailPage.tsx`: `activeVariant.availableQuantity`, `activeVariant?.id`, `attributeGroups.length`, `deal.categoryName`, `deal.description`, `deal.distanceInKm`, `deal.productName`, `deal.shopId`, `deal.shopLogoUrl`, `deal.shopName`, `deal.variants`, `deal.variants.length`, `e.currentTarget`, `e.currentTarget.src`, `galleryImages.length`, `group.name`, `group.values`, `matchedVar.attributes`, `v.availableQuantity`, `v.id`, `v.imageUrl`, `v.sku`, `v.variantName`.
- Nhóm: detail/tabs. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=false, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R5. /deals/:id

- Page: `src/pages/customer/CustomerDealDetailPage.tsx`. Layout: CustomerLayout. Quyền: guest, user, staff, storeOwner (admin redirected).
- Service được tham chiếu: `cartApi.addItem`, `cartApi.getPage`, `cartApi.removeItem`, `cartApi.summary`, `cartApi.updateQuantity`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/cart/types/cart.types.ts` (`CartItem`, `CartShop`, `Cart`, `CursorPagedResult`, `CartSummary`, `CartPage`, `CartMutationResponse`); `src/features/flash-deals/types/flashDeal.types.ts` (`DealAttributeGroup`, `FlashDealVariant`, `FlashDeal`, `CursorMode`, `FlashDealCursorParams`, `FlashDealCursorResult`, `DealFilterState`, `FlashDealStatusEnum`, `FlashDealVariantStatusEnum`, `ShopFlashDealListItem`, `ShopFlashDealVariantDetail`, `ShopFlashDealDetail`, `ShopEligibleProductVariant`, `ShopEligibleProduct`, `CreateFlashDealVariantPayload`, `CreateFlashDealPayload`, `UpdateFlashDealVariantPayload`, `UpdateFlashDealPayload`, `ShopFlashDealQueryParams`); `src/shared/types/index.ts` ().
- Hook/state/router: `useAuthStore.getState`, `useAuthStore.subscribe`, `useCartStore`, `useCartStore.setState`, `useNavigate`, `useParams`, `useToastStore.getState`, `useToastStore.getState().removeToast`.
- Child component: Inline trong page.
- Tương tác source: `onClick`, `onError`.
- Field đọc trong JSX — `src/pages/customer/CustomerDealDetailPage.tsx`: `activeVariant.availableQuantity`, `activeVariant?.id`, `attributeGroups.length`, `deal.categoryName`, `deal.description`, `deal.distanceInKm`, `deal.productName`, `deal.shopId`, `deal.shopLogoUrl`, `deal.shopName`, `deal.variants`, `deal.variants.length`, `e.currentTarget`, `e.currentTarget.src`, `galleryImages.length`, `group.name`, `group.values`, `matchedVar.attributes`, `v.availableQuantity`, `v.id`, `v.imageUrl`, `v.sku`, `v.variantName`.
- Nhóm: detail/tabs. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=false, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R6. /login

- Page: `src/pages/auth/LoginPage.tsx`. Layout: CustomerLayout. Quyền: guest, user, staff, storeOwner (admin redirected).
- Service được tham chiếu: `authApi.getMe`, `authApi.login`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/auth/types/auth.types.ts` (`AuthRole`, `CustomerStatus`, `UserAddress`, `AuthUser`, `LoginCredentials`, `RegisterInput`, `VerifyOtpInput`, `ForgotPasswordInput`, `ChangePasswordInput`, `TokenPair`, `CurrentUserResponse`); `src/shared/types/index.ts` ().
- Hook/state/router: `useAuthStore`, `useForm`, `useLocation`, `useNavigate`.
- Child component: `src/features/auth/components/AuthFormCard.tsx`, `src/features/auth/components/PasswordField.tsx`, `src/shared/components/SessionCheckingScreen.tsx`.
- Tương tác source: `onClick`, `onSubmit`.
- Field đọc trong JSX — `src/pages/auth/LoginPage.tsx`: `errors.email`, `errors.email?.message`, `errors.password`, `errors.password?.message`, `errors.root`, `errors.root.server`, `errors.root.server.message`, `errors.root?.server`, `errors.root?.server?.message`, `locationState.message`, `locationState?.message`, `user.role`.
- Nhóm: auth. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=false, empty=false, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R7. /register

- Page: `src/pages/auth/RegisterPage.tsx`. Layout: CustomerLayout. Quyền: guest, user, staff, storeOwner (admin redirected).
- Service được tham chiếu: `authApi.register`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/auth/types/auth.types.ts` (`AuthRole`, `CustomerStatus`, `UserAddress`, `AuthUser`, `LoginCredentials`, `RegisterInput`, `VerifyOtpInput`, `ForgotPasswordInput`, `ChangePasswordInput`, `TokenPair`, `CurrentUserResponse`); `src/shared/types/index.ts` ().
- Hook/state/router: `useForm`, `useNavigate`.
- Child component: `src/features/auth/components/AuthFormCard.tsx`, `src/features/auth/components/PasswordField.tsx`.
- Tương tác source: `onClick`, `onSubmit`.
- Field đọc trong JSX — `src/pages/auth/RegisterPage.tsx`: `errors.confirmPassword`, `errors.confirmPassword?.message`, `errors.email`, `errors.email?.message`, `errors.fullName`, `errors.fullName?.message`, `errors.password`, `errors.password?.message`, `errors.root`, `errors.root.server`, `errors.root.server.message`, `errors.root?.server`, `errors.root?.server?.message`.
- Nhóm: auth. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=false, empty=false, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R8. /register/verify

- Page: `src/pages/auth/OtpVerificationPage.tsx`. Layout: CustomerLayout. Quyền: guest, user, staff, storeOwner (admin redirected).
- Service được tham chiếu: `authApi.resetPassword`, `authApi.verifyRegistration`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/auth/types/auth.types.ts` (`AuthRole`, `CustomerStatus`, `UserAddress`, `AuthUser`, `LoginCredentials`, `RegisterInput`, `VerifyOtpInput`, `ForgotPasswordInput`, `ChangePasswordInput`, `TokenPair`, `CurrentUserResponse`); `src/shared/types/index.ts` ().
- Hook/state/router: `useForm`, `useLocation`, `useNavigate`, `useSearchParams`.
- Child component: `src/features/auth/components/AuthFormCard.tsx`.
- Tương tác source: `onSubmit`.
- Field đọc trong JSX — `src/pages/auth/OtpVerificationPage.tsx`: `errors.email`, `errors.email?.message`, `errors.otp`, `errors.otp?.message`, `errors.root`, `errors.root.server`, `errors.root.server.message`, `errors.root?.server`, `errors.root?.server?.message`.
- Nhóm: auth. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=false, empty=false, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R9. /forgot-password

- Page: `src/pages/auth/ForgotPasswordPage.tsx`. Layout: CustomerLayout. Quyền: guest, user, staff, storeOwner (admin redirected).
- Service được tham chiếu: `authApi.forgotPassword`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/auth/types/auth.types.ts` (`AuthRole`, `CustomerStatus`, `UserAddress`, `AuthUser`, `LoginCredentials`, `RegisterInput`, `VerifyOtpInput`, `ForgotPasswordInput`, `ChangePasswordInput`, `TokenPair`, `CurrentUserResponse`); `src/shared/types/index.ts` ().
- Hook/state/router: `useForm`, `useNavigate`.
- Child component: `src/features/auth/components/AuthFormCard.tsx`, `src/features/auth/components/PasswordField.tsx`.
- Tương tác source: `onClick`, `onSubmit`.
- Field đọc trong JSX — `src/pages/auth/ForgotPasswordPage.tsx`: `errors.confirmPassword`, `errors.confirmPassword?.message`, `errors.email`, `errors.email?.message`, `errors.newPassword`, `errors.newPassword?.message`, `errors.root`, `errors.root.server`, `errors.root.server.message`, `errors.root?.server`, `errors.root?.server?.message`.
- Nhóm: auth. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=false, empty=false, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R10. /forgot-password/verify

- Page: `src/pages/auth/OtpVerificationPage.tsx`. Layout: CustomerLayout. Quyền: guest, user, staff, storeOwner (admin redirected).
- Service được tham chiếu: `authApi.resetPassword`, `authApi.verifyRegistration`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/auth/types/auth.types.ts` (`AuthRole`, `CustomerStatus`, `UserAddress`, `AuthUser`, `LoginCredentials`, `RegisterInput`, `VerifyOtpInput`, `ForgotPasswordInput`, `ChangePasswordInput`, `TokenPair`, `CurrentUserResponse`); `src/shared/types/index.ts` ().
- Hook/state/router: `useForm`, `useLocation`, `useNavigate`, `useSearchParams`.
- Child component: `src/features/auth/components/AuthFormCard.tsx`.
- Tương tác source: `onSubmit`.
- Field đọc trong JSX — `src/pages/auth/OtpVerificationPage.tsx`: `errors.email`, `errors.email?.message`, `errors.otp`, `errors.otp?.message`, `errors.root`, `errors.root.server`, `errors.root.server.message`, `errors.root?.server`, `errors.root?.server?.message`.
- Nhóm: auth. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=false, empty=false, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R11. /customer

- Redirect source: `<Navigate to={APP_PATHS.HOME} replace />`. Không có UI riêng.

### R12. /cart

- Page: `src/pages/customer/CartPage.tsx`. Layout: CustomerLayout. Quyền: user, staff, storeOwner.
- Service được tham chiếu: `cartApi.addItem`, `cartApi.getPage`, `cartApi.removeItem`, `cartApi.summary`, `cartApi.updateQuantity`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/cart/types/cart.types.ts` (`CartItem`, `CartShop`, `Cart`, `CursorPagedResult`, `CartSummary`, `CartPage`, `CartMutationResponse`); `src/shared/types/index.ts` ().
- Hook/state/router: `useAuthStore.getState`, `useAuthStore.subscribe`, `useCartStore`, `useCartStore.getState`, `useCartStore.getState().setPageActive`, `useCartStore.setState`, `useNavigate`, `useToastStore.getState`, `useToastStore.getState().removeToast`.
- Child component: `src/features/cart/components/CartItemRow.tsx`.
- Tương tác source: `onBlur`, `onChange`, `onClick`, `onKeyDown`, `onQuantity`, `onRemove`.
- Field đọc trong JSX — `src/pages/customer/CartPage.tsx`: `cart.shops`, `cart?.shops`, `cart?.shops.length`, `item.id`, `shop.canCheckout`, `shop.items`, `shop.shopAddress`, `shop.shopId`, `shop.shopLogoUrl`, `shop.shopName`, `shop.totalQuantity`.
- Field đọc trong JSX — `src/features/cart/components/CartItemRow.tsx`: `event.currentTarget`, `event.key`, `event.target`, `item.availableQuantity`, `item.canUpdateQuantity`, `item.flashDealId`, `item.imageUrl`, `item.isAvailable`, `item.originalPrice`, `item.productName`, `item.quantity`, `item.unavailableReason`, `item.unitPrice`, `item.variantName`.
- Nhóm: list/workspace. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R13. /checkout/simulation

- Page: `src/pages/customer/CheckoutSimulationPage.tsx`. Layout: CustomerLayout. Quyền: user, staff, storeOwner.
- Service được tham chiếu: `paymentApi.simulate`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/checkout/types/checkout.types.ts` (`CheckoutMethod`, `DepositRange`, `CheckoutPolicy`, `CheckoutContext`, `PickupEstimate`, `CheckoutRoute`, `CheckoutQuote`, `CheckoutGps`, `CheckoutOrder`, `OrderHistoryPage`); `src/features/payments/types/paymentHistory.types.ts` (`CustomerPayment`, `CustomerPaymentPage`, `CustomerPaymentDetail`); `src/shared/types/index.ts` ().
- Hook/state/router: `useNavigate`, `useSearchParams`.
- Child component: Inline trong page.
- Tương tác source: `onClick`.
- Field đọc trong JSX — `src/pages/customer/CheckoutSimulationPage.tsx`: `order.orderCode`, `order.paymentMethod`, `order.paymentStatus`, `order.shopName`, `order.status`.
- Nhóm: list/workspace. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=false, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R14. /orders/:orderId

- Page: `src/pages/customer/OrderDetailPage.tsx`. Layout: CustomerLayout. Quyền: user, staff, storeOwner.
- Service được tham chiếu: `cartApi.addItem`, `cartApi.getPage`, `cartApi.removeItem`, `cartApi.summary`, `cartApi.updateQuantity`, `feedbackApi.addFeedback`, `feedbackApi.editFeedback`, `feedbackApi.uploadImage`, `orderApi.cancel`, `orderApi.order`, `paymentApi.payment`, `refundApi.confirm`, `refundApi.destination`, `refundApi.forOrder`, `refundApi.request`, `refundApi.upload`, `refundApi.withdraw`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/cart/types/cart.types.ts` (`CartItem`, `CartShop`, `Cart`, `CursorPagedResult`, `CartSummary`, `CartPage`, `CartMutationResponse`); `src/features/checkout/types/checkout.types.ts` (`CheckoutMethod`, `DepositRange`, `CheckoutPolicy`, `CheckoutContext`, `PickupEstimate`, `CheckoutRoute`, `CheckoutQuote`, `CheckoutGps`, `CheckoutOrder`, `OrderHistoryPage`); `src/features/feedbacks/types/feedback.types.ts` (`FeedbackReply`, `FeedbackItem`, `CreateFeedbackInput`, `UpdateFeedbackInput`, `CreateFeedbackReplyInput`, `UpdateFeedbackReplyInput`, `ShopFeedbacksQueryParams`, `FeedbackHistoryResponse`); `src/features/payments/types/paymentHistory.types.ts` (`CustomerPayment`, `CustomerPaymentPage`, `CustomerPaymentDetail`); `src/features/refunds/types/refund.types.ts` (`Refund`, `RefundBank`, `RefundPage`, `RefundImage`, `RefundQr`); `src/shared/types/index.ts` ().
- Hook/state/router: `useAuthStore`, `useAuthStore.getState`, `useAuthStore.subscribe`, `useCartStore.getState`, `useCartStore.getState().refresh`, `useCartStore.setState`, `useParams`, `useRealtimeSubscriptions`, `useToastStore.getState`, `useToastStore.getState().removeToast`.
- Child component: `src/features/checkout/components/CheckoutOrderReceipt.tsx`, `src/features/refunds/components/CustomerOrderRefunds.tsx`, `src/features/refunds/components/RefundCard.tsx`, `src/features/refunds/components/RefundItems.tsx`, `src/features/refunds/components/RefundEvidenceGallery.tsx`, `src/shared/components/modal/BaseModal.tsx`, `src/features/refunds/components/RefundStatusBadge.tsx`, `src/features/refunds/components/RefundDestinationForm.tsx`, `src/features/refunds/components/RefundBankSelect.tsx`, `src/features/refunds/components/RefundFileUpload.tsx`, `src/features/refunds/components/RefundRequestForm.tsx`, `src/features/checkout/components/OrderPickupDeadlines.tsx`, `src/features/checkout/components/OrderPickupCode.tsx`, `src/features/checkout/components/OrderStatusPanel.tsx`, `src/features/checkout/components/CancelOrderAction.tsx`, `src/features/feedbacks/components/RateOrderModal.tsx`, `src/features/feedbacks/components/StarRatingInput.tsx`, `src/features/feedbacks/components/EditReviewModal.tsx`.
- Tương tác source: `onCancel`, `onChange`, `onClick`, `onClose`, `onError`, `onMouseDown`, `onMouseEnter`, `onMouseLeave`, `onSubmit`, `onSubmitted`, `onUpdated`; realtime `CUSTOMER_ORDERS_CHANGED`.
- Field đọc trong JSX — `src/features/checkout/components/CheckoutOrderReceipt.tsx`: `editingFeedbackTarget?.feedbackId`, `editingFeedbackTarget?.imageUrl`, `editingFeedbackTarget?.productName`, `editingFeedbackTarget?.variantName`, `event.currentTarget`, `event.currentTarget.style`, `event.currentTarget.style.display`, `item.canReview`, `item.feedbackId`, `item.flashDealVariantId`, `item.imageUrl`, `item.orderItemId`, `item.productName`, `item.quantity`, `item.rating`, `item.variantName`, `order.id`, `order.items`, `order.orderCode`, `order.paymentMessage`, `order.paymentMethod`, `order.paymentReviewRequired`, `order.paymentStatus`, `order.policy`, `order.policy.appliedRange`, `order.policy.freeOrderAmount`, `order.policy.levelName`, `order.policy.levelRank`, `order.shopAddress`, `order.shopLogoUrl`, `order.shopName`, `order.status`, `order?.createdAt`.
- Field đọc trong JSX — `src/features/refunds/components/CustomerOrderRefunds.tsx`: `refund.id`.
- Field đọc trong JSX — `src/features/refunds/components/RefundCard.tsx`: `entry.actorName`, `entry.actorRole`, `entry.decision`, `entry.id`, `entry.note`, `entry.toStatus`, `new URL(url).protocol`, `refund.accountHolder`, `refund.accountNumber`, `refund.bankName`, `refund.evidenceUrls`, `refund.evidenceUrls.length`, `refund.history`, `refund.isOverdue`, `refund.reason`, `refund.rejectReason`, `refund.reviewDeadlineAt`, `refund.status`, `refund.transferProofUrls`, `refund.transferReference`, `refund.transferredAt`, `refund.type`.
- Field đọc trong JSX — `src/features/refunds/components/RefundItems.tsx`: `item.evidenceUrls`, `item.orderItemId`, `item.productName`, `item.purchasedQuantity`, `item.quantity`, `item.reason`, `item.variantName`, `refund.items`.
- Field đọc trong JSX — `src/features/refunds/components/RefundStatusBadge.tsx`: `colors.Withdrawn`.
- Field đọc trong JSX — `src/features/refunds/components/RefundDestinationForm.tsx`: `bank.accountHolder`, `bank.accountHolder.trim().length`, `bank.accountNumber`, `bank.bankBin`, `bank.bankName`, `bank.qrImageUrl`, `bank.receiverMethod`, `errors.account`, `errors.bank`, `errors.code`, `errors.holder`, `errors.qr`, `event.target`, `event.target.value`, `files.length`, `image.accountHolder`, `image.accountNumber`, `image.bankBin`, `image.url`, `refund.confirmationExpiresAt`, `refund.customerDetailsDeadlineAt`.
- Field đọc trong JSX — `src/features/refunds/components/RefundBankSelect.tsx`: `bank.bin`, `bank.name`, `bank.shortName`, `bank?.bin`, `bank?.name`, `event.target`, `event.target.value`, `item.bin`, `items.length`.
- Field đọc trong JSX — `src/features/refunds/components/RefundFileUpload.tsx`: `event.target`, `event.target.files`, `event.target.value`, `file.name`, `file.size`, `files.length`, `ref.current`, `selected.length`.
- Field đọc trong JSX — `src/features/refunds/components/RefundRequestForm.tsx`: `(reasons[id] \|\| "").trim().length`, `errors.evidence`, `errors.items`, `errors.reason`, `event.target`, `event.target.checked`, `event.target.value`, `evidence[id]?.length`, `evidence[item.orderItemId]?.length`, `fieldErrors.evidence`, `fieldErrors.items`, `fieldErrors.reason`, `image.url`, `item.orderItemId`, `item.productName`, `item.quantity`, `item.variantName`, `order.items`.
- Field đọc trong JSX — `src/features/checkout/components/OrderPickupDeadlines.tsx`: `order.hardDeadlineAt`, `order.softDeadlineAt`.
- Field đọc trong JSX — `src/features/checkout/components/OrderStatusPanel.tsx`: `order.paymentMethod`, `order.paymentReviewRequired`, `order.remainingAtShop`, `order.status`, `payment.color`, `payment.label`, `state.color`, `state.description`, `state.label`.
- Field đọc trong JSX — `src/features/feedbacks/components/RateOrderModal.tsx`: `draft.comment`, `draft.comment.length`, `draft.images`, `draft.images.length`, `draft.rating`, `draft?.enabled`, `e.target`, `e.target.checked`, `e.target.value`, `ev.currentTarget`, `ev.currentTarget.style`, `ev.currentTarget.style.display`, `fileInputRefs.current`, `item.canReview`, `item.flashDealVariantId`, `item.imageUrl`, `item.orderItemId`, `item.productName`, `item.rating`, `item.variantName`, `order.items`, `order.orderCode`, `order.shopName`, `reviewableItems.length`.
- Field đọc trong JSX — `src/features/feedbacks/components/EditReviewModal.tsx`: `comment.length`, `e.target`, `e.target.value`, `ev.currentTarget`, `ev.currentTarget.style`, `ev.currentTarget.style.display`, `feedback.orderCode`, `feedback.reply`, `feedback.reply.content`, `feedback.reply.repliedByName`, `feedback.shopName`, `feedback?.reply`, `feedback?.shopName`, `fileInputRef.current`, `images.length`.
- Nhóm: detail/tabs. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R15. /checkout

- Page: `src/pages/customer/CheckoutPage.tsx`. Layout: CustomerLayout. Quyền: user, staff, storeOwner.
- Service được tham chiếu: `cartApi.addItem`, `cartApi.getPage`, `cartApi.removeItem`, `cartApi.summary`, `cartApi.updateQuantity`, `checkoutApi.context`, `checkoutApi.place`, `checkoutApi.quote`, `feedbackApi.addFeedback`, `feedbackApi.editFeedback`, `feedbackApi.uploadImage`, `orderApi.cancel`, `orderApi.order`, `paymentApi.payment`, `refundApi.confirm`, `refundApi.destination`, `refundApi.forOrder`, `refundApi.request`, `refundApi.upload`, `refundApi.withdraw`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/cart/types/cart.types.ts` (`CartItem`, `CartShop`, `Cart`, `CursorPagedResult`, `CartSummary`, `CartPage`, `CartMutationResponse`); `src/features/checkout/types/checkout.types.ts` (`CheckoutMethod`, `DepositRange`, `CheckoutPolicy`, `CheckoutContext`, `PickupEstimate`, `CheckoutRoute`, `CheckoutQuote`, `CheckoutGps`, `CheckoutOrder`, `OrderHistoryPage`); `src/features/checkout/types/map.types.ts` (`CheckoutMapInstance`, `RouteLayer`, `RouteLayerGroup`, `RouteMarker`, `CheckoutMapSdk`); `src/features/feedbacks/types/feedback.types.ts` (`FeedbackReply`, `FeedbackItem`, `CreateFeedbackInput`, `UpdateFeedbackInput`, `CreateFeedbackReplyInput`, `UpdateFeedbackReplyInput`, `ShopFeedbacksQueryParams`, `FeedbackHistoryResponse`); `src/features/payments/types/paymentHistory.types.ts` (`CustomerPayment`, `CustomerPaymentPage`, `CustomerPaymentDetail`); `src/features/refunds/types/refund.types.ts` (`Refund`, `RefundBank`, `RefundPage`, `RefundImage`, `RefundQr`); `src/shared/types/index.ts` ().
- Hook/state/router: `useAuthStore`, `useAuthStore.getState`, `useAuthStore.subscribe`, `useCartStore.getState`, `useCartStore.getState().refresh`, `useCartStore.setState`, `useNavigate`, `useRealtimeSubscriptions`, `useSearchParams`, `useToastStore.getState`, `useToastStore.getState().removeToast`.
- Child component: `src/features/checkout/components/CheckoutRouteMap.tsx`, `src/features/checkout/components/DepositPolicyPanel.tsx`, `src/features/checkout/components/CheckoutOrderReceipt.tsx`, `src/features/refunds/components/CustomerOrderRefunds.tsx`, `src/features/refunds/components/RefundCard.tsx`, `src/features/refunds/components/RefundItems.tsx`, `src/features/refunds/components/RefundEvidenceGallery.tsx`, `src/shared/components/modal/BaseModal.tsx`, `src/features/refunds/components/RefundStatusBadge.tsx`, `src/features/refunds/components/RefundDestinationForm.tsx`, `src/features/refunds/components/RefundBankSelect.tsx`, `src/features/refunds/components/RefundFileUpload.tsx`, `src/features/refunds/components/RefundRequestForm.tsx`, `src/features/checkout/components/OrderPickupDeadlines.tsx`, `src/features/checkout/components/OrderPickupCode.tsx`, `src/features/checkout/components/OrderStatusPanel.tsx`, `src/features/checkout/components/CancelOrderAction.tsx`, `src/features/feedbacks/components/RateOrderModal.tsx`, `src/features/feedbacks/components/StarRatingInput.tsx`, `src/features/feedbacks/components/EditReviewModal.tsx`, `src/features/cart/components/CartItemRow.tsx`.
- Tương tác source: `onBlur`, `onCancel`, `onChange`, `onClick`, `onClose`, `onError`, `onKeyDown`, `onMouseDown`, `onMouseEnter`, `onMouseLeave`, `onSubmit`, `onSubmitted`, `onUpdated`; realtime `CUSTOMER_ORDERS_CHANGED`.
- Field đọc trong JSX — `src/pages/customer/CheckoutPage.tsx`: `context?.isSimulatedPayment`, `data.onlinePaymentAvailable`, `data.policy`, `data.policy.payAtShopAllowed`, `data.policy.requiredDeposit`, `data.shop`, `data.shop.items`, `data.shop.shopAddress`, `data.shop.shopId`, `data.shop.shopName`, `data.shop.subtotal`, `item.id`, `quote.pickup`, `quote.pickup.bufferMinutes`, `quote.pickup.bufferWasCapped`, `quote.pickup.distanceKm`, `quote.pickup.etaMinutes`, `quote?.route`, `selection.canSubmit`, `selection.dueOnline`, `selection.expired`, `selection.method`.
- Field đọc trong JSX — `src/features/checkout/components/CheckoutRouteMap.tsx`: `shop.shopId`.
- Field đọc trong JSX — `src/features/checkout/components/DepositPolicyPanel.tsx`: `policy.appliedRange`, `policy.appliedRange?.id`, `policy.freeOrderAmount`, `policy.isDefaultLevel`, `policy.levelName`, `policy.levelRank`, `policy.maxDepositOrderAmount`, `policy.payAtShopAllowed`, `policy.ranges`, `policy.ranges.length`, `policy.restrictionCode`, `range.id`, `shop.shopName`.
- Field đọc trong JSX — `src/features/checkout/components/CheckoutOrderReceipt.tsx`: `editingFeedbackTarget?.feedbackId`, `editingFeedbackTarget?.imageUrl`, `editingFeedbackTarget?.productName`, `editingFeedbackTarget?.variantName`, `event.currentTarget`, `event.currentTarget.style`, `event.currentTarget.style.display`, `item.canReview`, `item.feedbackId`, `item.flashDealVariantId`, `item.imageUrl`, `item.orderItemId`, `item.productName`, `item.quantity`, `item.rating`, `item.variantName`, `order.id`, `order.items`, `order.orderCode`, `order.paymentMessage`, `order.paymentMethod`, `order.paymentReviewRequired`, `order.paymentStatus`, `order.policy`, `order.policy.appliedRange`, `order.policy.freeOrderAmount`, `order.policy.levelName`, `order.policy.levelRank`, `order.shopAddress`, `order.shopLogoUrl`, `order.shopName`, `order.status`, `order?.createdAt`.
- Field đọc trong JSX — `src/features/refunds/components/CustomerOrderRefunds.tsx`: `refund.id`.
- Field đọc trong JSX — `src/features/refunds/components/RefundCard.tsx`: `entry.actorName`, `entry.actorRole`, `entry.decision`, `entry.id`, `entry.note`, `entry.toStatus`, `new URL(url).protocol`, `refund.accountHolder`, `refund.accountNumber`, `refund.bankName`, `refund.evidenceUrls`, `refund.evidenceUrls.length`, `refund.history`, `refund.isOverdue`, `refund.reason`, `refund.rejectReason`, `refund.reviewDeadlineAt`, `refund.status`, `refund.transferProofUrls`, `refund.transferReference`, `refund.transferredAt`, `refund.type`.
- Field đọc trong JSX — `src/features/refunds/components/RefundItems.tsx`: `item.evidenceUrls`, `item.orderItemId`, `item.productName`, `item.purchasedQuantity`, `item.quantity`, `item.reason`, `item.variantName`, `refund.items`.
- Field đọc trong JSX — `src/features/refunds/components/RefundStatusBadge.tsx`: `colors.Withdrawn`.
- Field đọc trong JSX — `src/features/refunds/components/RefundDestinationForm.tsx`: `bank.accountHolder`, `bank.accountHolder.trim().length`, `bank.accountNumber`, `bank.bankBin`, `bank.bankName`, `bank.qrImageUrl`, `bank.receiverMethod`, `errors.account`, `errors.bank`, `errors.code`, `errors.holder`, `errors.qr`, `event.target`, `event.target.value`, `files.length`, `image.accountHolder`, `image.accountNumber`, `image.bankBin`, `image.url`, `refund.confirmationExpiresAt`, `refund.customerDetailsDeadlineAt`.
- Field đọc trong JSX — `src/features/refunds/components/RefundBankSelect.tsx`: `bank.bin`, `bank.name`, `bank.shortName`, `bank?.bin`, `bank?.name`, `event.target`, `event.target.value`, `item.bin`, `items.length`.
- Field đọc trong JSX — `src/features/refunds/components/RefundFileUpload.tsx`: `event.target`, `event.target.files`, `event.target.value`, `file.name`, `file.size`, `files.length`, `ref.current`, `selected.length`.
- Field đọc trong JSX — `src/features/refunds/components/RefundRequestForm.tsx`: `(reasons[id] \|\| "").trim().length`, `errors.evidence`, `errors.items`, `errors.reason`, `event.target`, `event.target.checked`, `event.target.value`, `evidence[id]?.length`, `evidence[item.orderItemId]?.length`, `fieldErrors.evidence`, `fieldErrors.items`, `fieldErrors.reason`, `image.url`, `item.orderItemId`, `item.productName`, `item.quantity`, `item.variantName`, `order.items`.
- Field đọc trong JSX — `src/features/checkout/components/OrderPickupDeadlines.tsx`: `order.hardDeadlineAt`, `order.softDeadlineAt`.
- Field đọc trong JSX — `src/features/checkout/components/OrderStatusPanel.tsx`: `order.paymentMethod`, `order.paymentReviewRequired`, `order.remainingAtShop`, `order.status`, `payment.color`, `payment.label`, `state.color`, `state.description`, `state.label`.
- Field đọc trong JSX — `src/features/feedbacks/components/RateOrderModal.tsx`: `draft.comment`, `draft.comment.length`, `draft.images`, `draft.images.length`, `draft.rating`, `draft?.enabled`, `e.target`, `e.target.checked`, `e.target.value`, `ev.currentTarget`, `ev.currentTarget.style`, `ev.currentTarget.style.display`, `fileInputRefs.current`, `item.canReview`, `item.flashDealVariantId`, `item.imageUrl`, `item.orderItemId`, `item.productName`, `item.rating`, `item.variantName`, `order.items`, `order.orderCode`, `order.shopName`, `reviewableItems.length`.
- Field đọc trong JSX — `src/features/feedbacks/components/EditReviewModal.tsx`: `comment.length`, `e.target`, `e.target.value`, `ev.currentTarget`, `ev.currentTarget.style`, `ev.currentTarget.style.display`, `feedback.orderCode`, `feedback.reply`, `feedback.reply.content`, `feedback.reply.repliedByName`, `feedback.shopName`, `feedback?.reply`, `feedback?.shopName`, `fileInputRef.current`, `images.length`.
- Field đọc trong JSX — `src/features/cart/components/CartItemRow.tsx`: `event.currentTarget`, `event.key`, `event.target`, `item.availableQuantity`, `item.canUpdateQuantity`, `item.flashDealId`, `item.imageUrl`, `item.isAvailable`, `item.originalPrice`, `item.productName`, `item.quantity`, `item.unavailableReason`, `item.unitPrice`, `item.variantName`.
- Nhóm: list/workspace. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R16. /account

- Redirect source: `<Navigate to={APP_PATHS.ACCOUNT_PROFILE} replace />`. Không có UI riêng.

### R17. /account/profile

- Page: `src/pages/customer/ProfilePage.tsx`. Layout: CustomerLayout → AccountLayout. Quyền: user, staff, storeOwner.
- Service được tham chiếu: `addressApi.createAddress`, `addressApi.deleteAddress`, `addressApi.getAddresses`, `addressApi.setDefaultAddress`, `addressApi.updateAddress`, `authApi.changePassword`, `profileApi.updateProfile`, `profileApi.uploadAvatar`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/addresses/types/address.types.ts` (`UserAddress`, `CreateUserAddressInput`, `UpdateAddressInput`); `src/features/addresses/types/map.types.ts` (`MapLatLng`, `MapLayer`, `AddressMapInstance`, `AddressMapMarker`, `AddressMapSdk`); `src/features/auth/types/auth.types.ts` (`AuthRole`, `CustomerStatus`, `UserAddress`, `AuthUser`, `LoginCredentials`, `RegisterInput`, `VerifyOtpInput`, `ForgotPasswordInput`, `ChangePasswordInput`, `TokenPair`, `CurrentUserResponse`); `src/features/profiles/types/profile.types.ts` (`UpdateUserProfileInput`); `src/shared/types/index.ts` ().
- Hook/state/router: `useAddressLocation`, `useAuthStore`, `useForm`, `useToastStore.getState`, `useToastStore.getState().removeToast`, `useWatch`.
- Child component: `src/features/addresses/components/AddressSection.tsx`, `src/features/addresses/components/AddressForm.tsx`, `src/features/addresses/components/AddressLocationMap.tsx`, `src/features/profiles/components/UserProfileCard.tsx`, `src/features/profiles/components/EditProfileForm.tsx`, `src/features/profiles/components/ChangePasswordSection.tsx`, `src/features/auth/components/PasswordField.tsx`, `src/features/auth/components/AuthFormCard.tsx`.
- Tương tác source: `onCancel`, `onChange`, `onClick`, `onClose`, `onDelete`, `onEdit`, `onSaved`, `onSelect`, `onSetDefault`, `onSubmit`.
- Field đọc trong JSX — `src/features/addresses/components/AddressSection.tsx`: `address.id`, `address.isDefault`, `address.label`, `addresses.length`.
- Field đọc trong JSX — `src/features/addresses/components/AddressForm.tsx`: `errors.addressLine`, `errors.addressLine?.message`, `errors.city`, `errors.city?.message`, `errors.district`, `errors.district?.message`, `errors.label`, `errors.label?.message`, `errors.latitude`, `errors.latitude?.message`, `errors.longitude`, `errors.longitude?.message`, `errors.root`, `errors.root.server`, `errors.root.server.message`, `errors.root?.server`, `errors.root?.server?.message`, `errors.ward`, `errors.ward?.message`, `location.accuracy`, `location.error`, `location.phase`, `location.selectPoint`.
- Field đọc trong JSX — `src/features/addresses/components/AddressLocationMap.tsx`: `center.lat`, `center.lng`, `mapRef.current`.
- Field đọc trong JSX — `src/features/profiles/components/UserProfileCard.tsx`: `passwordButtonRef.current`, `status.badgeClass`, `status.dotClass`, `status.icon`, `status.label`, `status.textClass`, `toneClasses.container`, `toneClasses.icon`, `toneClasses.label`, `user.customerStatus`, `user.email`, `user.fullName`, `user.phone`.
- Field đọc trong JSX — `src/features/profiles/components/EditProfileForm.tsx`: `errors.fullName`, `errors.fullName.message`, `errors.fullName?.message`, `errors.phone`, `errors.phone.message`, `errors.phone?.message`, `errors.root`, `errors.root.server`, `errors.root.server.message`, `errors.root?.server`, `errors.root?.server?.message`.
- Field đọc trong JSX — `src/features/profiles/components/ChangePasswordSection.tsx`: `errors.confirmPassword`, `errors.confirmPassword?.message`, `errors.currentPassword`, `errors.currentPassword?.message`, `errors.newPassword`, `errors.newPassword?.message`, `errors.root`, `errors.root.server`, `errors.root.server.message`, `errors.root?.server`, `errors.root?.server?.message`.
- Nhóm: form/settings. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R18. /account/orders

- Page: `src/pages/customer/OrdersPage.tsx`. Layout: CustomerLayout → AccountLayout. Quyền: user, staff, storeOwner.
- Service được tham chiếu: `feedbackApi.addFeedback`, `feedbackApi.editFeedback`, `feedbackApi.uploadImage`, `orderApi.orderPage`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/checkout/types/checkout.types.ts` (`CheckoutMethod`, `DepositRange`, `CheckoutPolicy`, `CheckoutContext`, `PickupEstimate`, `CheckoutRoute`, `CheckoutQuote`, `CheckoutGps`, `CheckoutOrder`, `OrderHistoryPage`); `src/features/feedbacks/types/feedback.types.ts` (`FeedbackReply`, `FeedbackItem`, `CreateFeedbackInput`, `UpdateFeedbackInput`, `CreateFeedbackReplyInput`, `UpdateFeedbackReplyInput`, `ShopFeedbacksQueryParams`, `FeedbackHistoryResponse`); `src/shared/types/index.ts` ().
- Hook/state/router: `useAuthStore`, `useRealtimeSubscriptions`, `useSearchParams`, `useToastStore.getState`, `useToastStore.getState().removeToast`.
- Child component: `src/features/checkout/components/OrderPickupCode.tsx`, `src/features/feedbacks/components/RateOrderModal.tsx`, `src/shared/components/modal/BaseModal.tsx`, `src/features/feedbacks/components/StarRatingInput.tsx`, `src/features/feedbacks/components/EditReviewModal.tsx`, `src/features/checkout/components/OrderStatusPanel.tsx`, `src/features/checkout/components/OrderPickupDeadlines.tsx`.
- Tương tác source: `onChange`, `onClick`, `onClose`, `onError`, `onFilter`, `onMouseDown`, `onMouseEnter`, `onMouseLeave`, `onRefresh`, `onSubmit`, `onSubmitted`, `onUpdated`; realtime `CUSTOMER_ORDERS_CHANGED`.
- Field đọc trong JSX — `src/pages/customer/OrdersPage.tsx`: `editingFeedbackTarget?.feedbackId`, `editingFeedbackTarget?.imageUrl`, `editingFeedbackTarget?.productName`, `editingFeedbackTarget?.variantName`, `event.currentTarget`, `event.currentTarget.style`, `event.currentTarget.style.display`, `i.canReview`, `item.feedbackId`, `item.flashDealVariantId`, `item.imageUrl`, `item.productName`, `item.quantity`, `item.rating`, `item.variantName`, `order.createdAt`, `order.id`, `order.items`, `order.items.length`, `order.orderCode`, `order.shopAddress`, `order.shopLogoUrl`, `order.shopName`, `order.status`, `order.totalAmount`, `orders.length`.
- Field đọc trong JSX — `src/features/feedbacks/components/RateOrderModal.tsx`: `draft.comment`, `draft.comment.length`, `draft.images`, `draft.images.length`, `draft.rating`, `draft?.enabled`, `e.target`, `e.target.checked`, `e.target.value`, `ev.currentTarget`, `ev.currentTarget.style`, `ev.currentTarget.style.display`, `fileInputRefs.current`, `item.canReview`, `item.flashDealVariantId`, `item.imageUrl`, `item.orderItemId`, `item.productName`, `item.rating`, `item.variantName`, `order.items`, `order.orderCode`, `order.shopName`, `reviewableItems.length`.
- Field đọc trong JSX — `src/features/feedbacks/components/EditReviewModal.tsx`: `comment.length`, `e.target`, `e.target.value`, `ev.currentTarget`, `ev.currentTarget.style`, `ev.currentTarget.style.display`, `feedback.orderCode`, `feedback.reply`, `feedback.reply.content`, `feedback.reply.repliedByName`, `feedback.shopName`, `feedback?.reply`, `feedback?.shopName`, `fileInputRef.current`, `images.length`.
- Field đọc trong JSX — `src/features/checkout/components/OrderStatusPanel.tsx`: `order.paymentMethod`, `order.paymentReviewRequired`, `order.remainingAtShop`, `order.status`, `payment.color`, `payment.label`, `state.color`, `state.description`, `state.label`.
- Field đọc trong JSX — `src/features/checkout/components/OrderPickupDeadlines.tsx`: `order.hardDeadlineAt`, `order.softDeadlineAt`.
- Nhóm: list/workspace. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R19. /account/payments

- Page: `src/pages/customer/PaymentsPage.tsx`. Layout: CustomerLayout → AccountLayout. Quyền: user, staff, storeOwner.
- Service được tham chiếu: `paymentApi.page`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/checkout/types/checkout.types.ts` (`CheckoutMethod`, `DepositRange`, `CheckoutPolicy`, `CheckoutContext`, `PickupEstimate`, `CheckoutRoute`, `CheckoutQuote`, `CheckoutGps`, `CheckoutOrder`, `OrderHistoryPage`); `src/features/payments/types/paymentHistory.types.ts` (`CustomerPayment`, `CustomerPaymentPage`, `CustomerPaymentDetail`); `src/shared/types/index.ts` ().
- Hook/state/router: `useAuthStore`, `useSearchParams`.
- Child component: `src/features/payments/components/PaymentStatusBadge.tsx`, `src/shared/components/pagination/index.ts`.
- Tương tác source: `onChange`, `onClick`, `onPageChange`.
- Field đọc trong JSX — `src/pages/customer/PaymentsPage.tsx`: `current.error`, `current?.error`, `data.page`, `data.pageSize`, `data.totalItems`, `data.totalPages`, `data?.items`, `data?.items.length`, `e.target`, `e.target.value`, `payment.id`, `payment.orderCode`, `payment.paidAt`, `payment.shopLogoUrl`, `payment.shopName`, `payment.status`.
- Field đọc trong JSX — `src/features/payments/components/PaymentStatusBadge.tsx`: `colors.Cancelled`.
- Nhóm: list/workspace. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R20. /account/refunds

- Page: `src/pages/customer/RefundsPage.tsx`. Layout: CustomerLayout → AccountLayout. Quyền: user, staff, storeOwner.
- Service được tham chiếu: Không có service HTTP được phát hiện. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/checkout/types/checkout.types.ts` (`CheckoutMethod`, `DepositRange`, `CheckoutPolicy`, `CheckoutContext`, `PickupEstimate`, `CheckoutRoute`, `CheckoutQuote`, `CheckoutGps`, `CheckoutOrder`, `OrderHistoryPage`); `src/features/refunds/types/refund.types.ts` (`Refund`, `RefundBank`, `RefundPage`, `RefundImage`, `RefundQr`); `src/shared/types/index.ts` ().
- Hook/state/router: `useRealtimeSubscriptions`, `useSearchParams`.
- Child component: `src/shared/components/pagination/index.ts`, `src/features/refunds/components/RefundStatusBadge.tsx`.
- Tương tác source: `onChange`, `onClick`, `onError`, `onPageChange`; realtime `CUSTOMER_ORDERS_CHANGED`.
- Field đọc trong JSX — `src/pages/customer/RefundsPage.tsx`: `current.data`, `current.data.page`, `current.data.pageSize`, `current.data.totalItems`, `current.data.totalPages`, `current.error`, `current?.data`, `current?.data?.items`, `current?.data?.items.length`, `current?.error`, `e.target`, `refund.id`, `refund.isOverdue`, `refund.orderCode`, `refund.shopLogoUrl`, `refund.shopName`, `refund.status`, `refund.type`.
- Field đọc trong JSX — `src/features/refunds/components/RefundStatusBadge.tsx`: `colors.Withdrawn`.
- Nhóm: list/workspace. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R21. /account/refunds/:refundId

- Page: `src/pages/customer/RefundDetailPage.tsx`. Layout: CustomerLayout → AccountLayout. Quyền: user, staff, storeOwner.
- Service được tham chiếu: `refundApi.confirm`, `refundApi.destination`, `refundApi.upload`, `refundApi.withdraw`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/checkout/types/checkout.types.ts` (`CheckoutMethod`, `DepositRange`, `CheckoutPolicy`, `CheckoutContext`, `PickupEstimate`, `CheckoutRoute`, `CheckoutQuote`, `CheckoutGps`, `CheckoutOrder`, `OrderHistoryPage`); `src/features/refunds/types/refund.types.ts` (`Refund`, `RefundBank`, `RefundPage`, `RefundImage`, `RefundQr`); `src/shared/types/index.ts` ().
- Hook/state/router: `useAuthStore.getState`, `useParams`, `useRealtimeSubscriptions`.
- Child component: `src/features/refunds/components/CustomerRefundDetailContent.tsx`, `src/features/refunds/components/RefundItems.tsx`, `src/features/refunds/components/RefundEvidenceGallery.tsx`, `src/shared/components/modal/BaseModal.tsx`, `src/features/refunds/components/RefundDestinationForm.tsx`, `src/features/refunds/components/RefundBankSelect.tsx`, `src/features/refunds/components/RefundFileUpload.tsx`, `src/features/refunds/components/RefundStatusBadge.tsx`.
- Tương tác source: `onChange`, `onClick`, `onClose`, `onError`, `onMouseDown`, `onSubmit`; realtime `CUSTOMER_ORDERS_CHANGED`.
- Field đọc trong JSX — `src/pages/customer/RefundDetailPage.tsx`: `event.currentTarget`, `event.currentTarget.style`, `event.currentTarget.style.display`, `refund.isOverdue`, `refund.orderCode`, `refund.shopLogoUrl`, `refund.shopName`, `refund.status`.
- Field đọc trong JSX — `src/features/refunds/components/CustomerRefundDetailContent.tsx`: `entry.actorName`, `entry.actorRole`, `entry.decision`, `entry.id`, `entry.note`, `entry.proofUrl`, `item.evidenceUrls`, `item.evidenceUrls?.length`, `refund.accountHolder`, `refund.accountNumber`, `refund.bankName`, `refund.destinationConfirmedAt`, `refund.evidenceUrls`, `refund.history`, `refund.history.length`, `refund.id`, `refund.items`, `refund.reason`, `refund.reviewDeadlineAt`, `refund.shopName`, `refund.status`, `refund.transferProofUrls`, `refund.transferProofUrls.length`, `refund.transferReference`, `refund.type`, `steps.length`.
- Field đọc trong JSX — `src/features/refunds/components/RefundItems.tsx`: `item.evidenceUrls`, `item.orderItemId`, `item.productName`, `item.purchasedQuantity`, `item.quantity`, `item.reason`, `item.variantName`, `refund.items`.
- Field đọc trong JSX — `src/features/refunds/components/RefundDestinationForm.tsx`: `bank.accountHolder`, `bank.accountHolder.trim().length`, `bank.accountNumber`, `bank.bankBin`, `bank.bankName`, `bank.qrImageUrl`, `bank.receiverMethod`, `errors.account`, `errors.bank`, `errors.code`, `errors.holder`, `errors.qr`, `event.target`, `event.target.value`, `files.length`, `image.accountHolder`, `image.accountNumber`, `image.bankBin`, `image.url`, `refund.confirmationExpiresAt`, `refund.customerDetailsDeadlineAt`.
- Field đọc trong JSX — `src/features/refunds/components/RefundBankSelect.tsx`: `bank.bin`, `bank.name`, `bank.shortName`, `bank?.bin`, `bank?.name`, `event.target`, `event.target.value`, `item.bin`, `items.length`.
- Field đọc trong JSX — `src/features/refunds/components/RefundFileUpload.tsx`: `event.target`, `event.target.files`, `event.target.value`, `file.name`, `file.size`, `files.length`, `ref.current`, `selected.length`.
- Field đọc trong JSX — `src/features/refunds/components/RefundStatusBadge.tsx`: `colors.Withdrawn`.
- Nhóm: detail/tabs. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R22. /account/payments/:paymentId

- Page: `src/pages/customer/PaymentDetailPage.tsx`. Layout: CustomerLayout → AccountLayout. Quyền: user, staff, storeOwner.
- Service được tham chiếu: `paymentApi.detail`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/checkout/types/checkout.types.ts` (`CheckoutMethod`, `DepositRange`, `CheckoutPolicy`, `CheckoutContext`, `PickupEstimate`, `CheckoutRoute`, `CheckoutQuote`, `CheckoutGps`, `CheckoutOrder`, `OrderHistoryPage`); `src/features/payments/types/paymentHistory.types.ts` (`CustomerPayment`, `CustomerPaymentPage`, `CustomerPaymentDetail`); `src/shared/types/index.ts` ().
- Hook/state/router: `useAuthStore`, `useParams`.
- Child component: `src/features/payments/components/PaymentStatusBadge.tsx`, `src/features/checkout/components/OrderPickupDeadlines.tsx`, `src/features/checkout/components/OrderPickupCode.tsx`.
- Tương tác source: `onClick`.
- Field đọc trong JSX — `src/pages/customer/PaymentDetailPage.tsx`: `item.flashDealVariantId`, `item.imageUrl`, `item.productName`, `item.quantity`, `item.variantName`, `order.id`, `order.items`, `order.orderCode`, `order.outstandingAmount`, `order.paidAmount`, `order.shopAddress`, `order.shopLogoUrl`, `order.shopName`, `order.status`, `order.totalAmount`, `payment.id`, `payment.paidAt`, `payment.status`.
- Field đọc trong JSX — `src/features/payments/components/PaymentStatusBadge.tsx`: `colors.Cancelled`.
- Field đọc trong JSX — `src/features/checkout/components/OrderPickupDeadlines.tsx`: `order.hardDeadlineAt`, `order.softDeadlineAt`.
- Nhóm: detail/tabs. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=false, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R23. /account/reviews

- Page: `src/pages/customer/ReviewsPage.tsx`. Layout: CustomerLayout → AccountLayout. Quyền: user, staff, storeOwner.
- Service được tham chiếu: `feedbackApi.deleteFeedback`, `feedbackApi.editFeedback`, `feedbackApi.getHistory`, `feedbackApi.uploadImage`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/feedbacks/types/feedback.types.ts` (`FeedbackReply`, `FeedbackItem`, `CreateFeedbackInput`, `UpdateFeedbackInput`, `CreateFeedbackReplyInput`, `UpdateFeedbackReplyInput`, `ShopFeedbacksQueryParams`, `FeedbackHistoryResponse`); `src/shared/types/index.ts` ().
- Hook/state/router: `useSearchParams`, `useToastStore.getState`, `useToastStore.getState().removeToast`.
- Child component: `src/features/feedbacks/components/ReviewsPanel.tsx`, `src/shared/components/modal/BaseModal.tsx`, `src/features/feedbacks/components/ReviewCard.tsx`, `src/features/feedbacks/components/StarRatingInput.tsx`, `src/shared/components/pagination/index.ts`.
- Tương tác source: `onChange`, `onClick`, `onClose`, `onDelete`, `onError`, `onMouseDown`, `onMouseEnter`, `onMouseLeave`, `onPageChange`, `onRequestDelete`, `onUpdate`.
- Field đọc trong JSX — `src/features/feedbacks/components/ReviewsPanel.tsx`: `review.id`, `reviewToDelete?.productName`, `reviews.length`.
- Field đọc trong JSX — `src/features/feedbacks/components/ReviewCard.tsx`: `comment.length`, `e.target`, `ev.currentTarget`, `ev.currentTarget.style`, `ev.currentTarget.style.display`, `fileInputRef.current`, `images.length`, `review.comment`, `review.createdAt`, `review.id`, `review.imageUrl`, `review.images`, `review.images.length`, `review.orderCode`, `review.orderId`, `review.productName`, `review.rating`, `review.reply`, `review.reply.content`, `review.reply.createdAt`, `review.reply.repliedByName`, `review.shopLogoUrl`, `review.shopName`, `review.variantName`.
- Nhóm: list/workspace. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R24. /account/trust-score/:tab?

- Page: `src/pages/customer/TrustScorePage.tsx`. Layout: CustomerLayout → AccountLayout. Quyền: user, staff, storeOwner.
- Service được tham chiếu: `trustApi.getHistory`, `trustApi.getLevelProgress`, `trustApi.getLevels`, `trustApi.getOverview`, `trustApi.getPenalties`, `trustApi.getPenaltyRecovery`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/trust/types/trust.types.ts` (`TrustLevelDepositRange`, `TrustLevelDepositPolicy`, `TrustLevelSummary`, `TrustLevelRequirement`, `TrustStanding`, `TrustScoreOverview`, `ProgressMetricKey`, `ProgressMetric`, `LevelProgress`, `TrustLevelInfo`, `TrustScoreEventType`, `TrustScoreHistoryItem`, `PenaltyStatus`, `PenaltyItem`, `PenaltyRecoveryOrder`, `PenaltyRecovery`, `TrustListParams`, `TrustHistoryParams`, `TrustPenaltyParams`); `src/shared/types/index.ts` ().
- Hook/state/router: `useApiResource`, `useAuthStore`, `useListParams`, `useParams`, `useSearchParams`.
- Child component: `src/features/trust/components/TrustHistoryPanel.tsx`, `src/shared/components/pagination/index.ts`, `src/features/trust/components/ListFilter.tsx`, `src/features/trust/components/PanelState.tsx`, `src/features/trust/components/TrustLevelsPanel.tsx`, `src/features/trust/components/TrustOverviewPanel.tsx`, `src/features/trust/components/ProgressBar.tsx`, `src/features/trust/components/TrustPenaltiesPanel.tsx`, `src/features/trust/components/PenaltyRecoveryModal.tsx`, `src/shared/components/modal/BaseModal.tsx`, `src/features/trust/components/TrustTabs.tsx`.
- Tương tác source: `onChange`, `onClick`, `onClose`, `onMouseDown`, `onPageChange`, `onRetry`.
- Field đọc trong JSX — `src/features/trust/components/TrustHistoryPanel.tsx`: `data.items`, `data.items.length`, `data.page`, `data.total`, `data.totalPages`, `item.eventType`, `item.id`, `item.orderCode`, `item.reason`, `item.scoreAfter`, `item.scoreBefore`, `item.scoreDelta`.
- Field đọc trong JSX — `src/features/trust/components/ListFilter.tsx`: `event.target`, `event.target.value`, `option.label`, `option.value`, `options.find((option) => option.value === event.target.value)?.value`.
- Field đọc trong JSX — `src/features/trust/components/TrustLevelsPanel.tsx`: `level.depositPolicy`, `level.depositPolicy.depositFreeOrderAmount`, `level.depositPolicy.maxDepositOrderAmount`, `level.depositPolicy.ranges`, `level.depositPolicy.requireFullOnlinePayment`, `level.description`, `level.id`, `level.isCurrent`, `level.isUnlocked`, `level.name`, `level.requirement`, `level.requirement.minCompletedOrders`, `level.requirement.minScore`, `range.id`.
- Field đọc trong JSX — `src/features/trust/components/TrustOverviewPanel.tsx`: `data.activePenaltyCount`, `data.completedOrdersCount`, `data.currentScore`, `data.isHighestLevel`, `data.level`, `data.level.description`, `data.level.name`, `metric.key`, `metric.percent`, `overview.error`, `progress.data`, `progress.data.isHighestLevel`, `progress.data.metrics`, `progress.data.nextLevel`, `progress.data.nextLevel.name`, `progress.data.onlineOrderCompletedPoints`, `progress.data.overallPercent`, `progress.data.payAtShopOrderCompletedPoints`, `progress.data.requirementsMet`, `progress.error`, `progress.reload`, `standing.className`, `standing.description`, `standing.label`.
- Field đọc trong JSX — `src/features/trust/components/TrustPenaltiesPanel.tsx`: `data.items`, `data.items.length`, `data.page`, `data.total`, `data.totalPages`, `penalty.completedOnlineOrders`, `penalty.id`, `penalty.occurrenceNumber`, `penalty.orderCode`, `penalty.reason`, `penalty.requiredOnlineOrders`, `penalty.scoreDeducted`, `penalty.status`.
- Field đọc trong JSX — `src/features/trust/components/PenaltyRecoveryModal.tsx`: `data.isRecovered`, `data.metrics`, `data.overallPercent`, `data.penalty`, `data.penalty.occurrenceNumber`, `data.penalty.orderCode`, `data.penalty.permanentOfflineLock`, `data.penalty.reason`, `data.penalty.scoreDeducted`, `data.recoveryOrders`, `data.recoveryOrders.length`, `metric.key`, `metric.percent`, `order.orderCode`, `order.orderId`.
- Field đọc trong JSX — `src/features/trust/components/TrustTabs.tsx`: `tab.id`, `tab.label`.
- Nhóm: detail/tabs. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R25. /account/shop-application

- Page: `src/pages/customer/ShopApplicationPage.tsx`. Layout: CustomerLayout → AccountLayout. Quyền: user, staff, storeOwner.
- Service được tham chiếu: `shopApplicationApi.cancel`, `shopApplicationApi.getMine`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/shop-applications/types/shopApplication.types.ts` (`ShopApplicationStatus`, `ShopDocumentType`, `ShopApplicationDocument`, `ShopApplicationReviewLog`, `ShopApplication`, `ShopApplicationForm`, `ShopDocumentUpload`); `src/shared/types/index.ts` ().
- Hook/state/router: `useNavigate`.
- Child component: `src/features/shop-applications/components/LatestShopApplication.tsx`, `src/features/shop-applications/components/ShopApplicationHistory.tsx`.
- Tương tác source: `onApplicationChanged`, `onClick`.
- Field đọc trong JSX — `src/features/shop-applications/components/LatestShopApplication.tsx`: `application.bankAccountHolder`, `application.bankAccountNumber`, `application.bankName`, `application.businessLicenseNo`, `application.coverImageUrl`, `application.description`, `application.logoUrl`, `application.name`, `application.status`, `status.badge`, `status.dot`, `status.label`, `status.notice`, `status.noticeBody`, `status.noticeTitle`, `step.label`, `step.state`, `steps.length`, `steps[i + 1].state`.
- Field đọc trong JSX — `src/features/shop-applications/components/ShopApplicationHistory.tsx`: `application.closingTime`, `application.coverImageUrl`, `application.description`, `application.hasPayosConfiguration`, `application.id`, `application.logoUrl`, `application.name`, `application.openingTime`, `document.documentType`, `document.fileUrl`, `document.id`, `document.originalFileName`, `documents.length`, `entries.length`, `log.createdAt`, `log.fromStatus`, `log.note`, `log.revisionNumber`, `log.toStatus`, `pageEntries.length`.
- Nhóm: list/workspace. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R26. /account/shop-application/new

- Page: `src/pages/customer/ShopApplicationFormPage.tsx`. Layout: CustomerLayout → AccountLayout. Quyền: user, staff, storeOwner.
- Service được tham chiếu: `shopApplicationApi.create`, `shopApplicationApi.resubmit`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/shop-applications/types/shopApplication.types.ts` (`ShopApplicationStatus`, `ShopDocumentType`, `ShopApplicationDocument`, `ShopApplicationReviewLog`, `ShopApplication`, `ShopApplicationForm`, `ShopDocumentUpload`); `src/shared/types/index.ts` ().
- Hook/state/router: `useNavigate`, `useParams`, `usePreviewUrl`.
- Child component: `src/features/shop-applications/components/ShopApplicationForm.tsx`.
- Tương tác source: `onAdd`, `onBlur`, `onCancel`, `onChange`, `onClick`, `onCoverChange`, `onLogoChange`, `onRemove`, `onSaved`, `onSubmit`.
- Field đọc trong JSX — `src/features/shop-applications/components/ShopApplicationForm.tsx`: `application.documents`, `application?.coverImageUrl`, `application?.documents`, `application?.logoUrl`, `cover.name`, `document.documentType`, `document.file`, `document.file.name`, `document.fileUrl`, `document.id`, `document.isCurrent`, `document.originalFileName`, `document.type`, `documents.length`, `event.currentTarget`, `event.currentTarget.value`, `event.target`, `event.target.files`, `event.target.value`, `fieldErrors.addressLine`, `fieldErrors.bankAccountHolder`, `fieldErrors.bankAccountNumber`, `fieldErrors.bankName`, `fieldErrors.businessLicenseNo`, `fieldErrors.city`, `fieldErrors.closingTime`, `fieldErrors.description`, `fieldErrors.district`, `fieldErrors.documents`, `fieldErrors.latitude`, `fieldErrors.logo`, `fieldErrors.longitude`, `fieldErrors.name`, `fieldErrors.openingTime`, `fieldErrors.payosApiKey`, `fieldErrors.payosChecksumKey`, `fieldErrors.payosClientId`, `fieldErrors.ward`, `form.addressLine`, `form.bankAccountHolder`, `form.bankAccountNumber`, `form.bankName`, `form.businessLicenseNo`, `form.city`, `form.closingTime`, `form.description`, `form.description.length`, `form.district`, `form.latitude`, `form.longitude`, `form.name`, `form.openingTime`, `form.payosApiKey`, `form.payosChecksumKey`, `form.payosClientId`, `form.ward`, `logo.name`, `upload.type`.
- Nhóm: form/settings. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R27. /account/shop-application/edit/:applicationId

- Page: `src/pages/customer/ShopApplicationFormPage.tsx`. Layout: CustomerLayout → AccountLayout. Quyền: user, staff, storeOwner.
- Service được tham chiếu: `shopApplicationApi.create`, `shopApplicationApi.resubmit`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/shop-applications/types/shopApplication.types.ts` (`ShopApplicationStatus`, `ShopDocumentType`, `ShopApplicationDocument`, `ShopApplicationReviewLog`, `ShopApplication`, `ShopApplicationForm`, `ShopDocumentUpload`); `src/shared/types/index.ts` ().
- Hook/state/router: `useNavigate`, `useParams`, `usePreviewUrl`.
- Child component: `src/features/shop-applications/components/ShopApplicationForm.tsx`.
- Tương tác source: `onAdd`, `onBlur`, `onCancel`, `onChange`, `onClick`, `onCoverChange`, `onLogoChange`, `onRemove`, `onSaved`, `onSubmit`.
- Field đọc trong JSX — `src/features/shop-applications/components/ShopApplicationForm.tsx`: `application.documents`, `application?.coverImageUrl`, `application?.documents`, `application?.logoUrl`, `cover.name`, `document.documentType`, `document.file`, `document.file.name`, `document.fileUrl`, `document.id`, `document.isCurrent`, `document.originalFileName`, `document.type`, `documents.length`, `event.currentTarget`, `event.currentTarget.value`, `event.target`, `event.target.files`, `event.target.value`, `fieldErrors.addressLine`, `fieldErrors.bankAccountHolder`, `fieldErrors.bankAccountNumber`, `fieldErrors.bankName`, `fieldErrors.businessLicenseNo`, `fieldErrors.city`, `fieldErrors.closingTime`, `fieldErrors.description`, `fieldErrors.district`, `fieldErrors.documents`, `fieldErrors.latitude`, `fieldErrors.logo`, `fieldErrors.longitude`, `fieldErrors.name`, `fieldErrors.openingTime`, `fieldErrors.payosApiKey`, `fieldErrors.payosChecksumKey`, `fieldErrors.payosClientId`, `fieldErrors.ward`, `form.addressLine`, `form.bankAccountHolder`, `form.bankAccountNumber`, `form.bankName`, `form.businessLicenseNo`, `form.city`, `form.closingTime`, `form.description`, `form.description.length`, `form.district`, `form.latitude`, `form.longitude`, `form.name`, `form.openingTime`, `form.payosApiKey`, `form.payosChecksumKey`, `form.payosClientId`, `form.ward`, `logo.name`, `upload.type`.
- Nhóm: form/settings. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R28. /account/workspace

- Page: `src/pages/customer/WorkspacePage.tsx`. Layout: CustomerLayout → AccountLayout. Quyền: user, staff, storeOwner.
- Service được tham chiếu: `staffApi.getAssociatedShops`, `staffApi.leaveShop`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/staff-shops/types/staff.types.ts` (`ShopStaffStatus`, `AssociatedShop`, `StaffShop`, `StaffInfo`, `StaffShopDetail`); `src/shared/types/index.ts` ().
- Hook/state/router: `useNavigate`, `usePagination`, `useToastStore.getState`, `useToastStore.getState().removeToast`.
- Child component: `src/features/staff-shops/components/AssociatedShopImage.tsx`, `src/shared/components/pagination/index.ts`.
- Tương tác source: `onClick`, `onError`, `onPageChange`.
- Field đọc trong JSX — `src/pages/customer/WorkspacePage.tsx`: `shop.id`, `shop.joinedAt`, `shop.shopId`, `shop.shopName`, `shop.status`, `shopToLeave.shopId`, `shopToLeave.shopName`, `shops.length`.
- Field đọc trong JSX — `src/features/staff-shops/components/AssociatedShopImage.tsx`: `shop.shopName`.
- Nhóm: list/workspace. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R29. /account/staff-invitations

- Page: `src/pages/customer/StaffInvitationsPage.tsx`. Layout: CustomerLayout → AccountLayout. Quyền: user, staff, storeOwner.
- Service được tham chiếu: `staffInvitationApi.acceptInvitation`, `staffInvitationApi.declineInvitation`, `staffInvitationApi.getMyInvitations`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/staff-invitations/types/staffInvitation.types.ts` (`StaffInvitationStatus`, `StaffInvitation`); `src/shared/types/index.ts` ().
- Hook/state/router: `useAuthStore`, `usePagination`, `useToastStore.getState`, `useToastStore.getState().removeToast`.
- Child component: `src/shared/components/pagination/index.ts`.
- Tương tác source: `onClick`, `onError`, `onPageChange`.
- Field đọc trong JSX — `src/pages/customer/StaffInvitationsPage.tsx`: `invitation.id`, `invitation.shopAddress`, `invitation.shopCoverImageUrl`, `invitation.shopId`, `invitation.shopLogoUrl`, `invitation.shopName`, `sortedInvitations.length`.
- Nhóm: list/workspace. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R30. /admin

- Redirect source: `<Navigate to={`${APP_PATHS.ADMIN_ANALYTICS}/overview`} replace />`. Không có UI riêng.

### R31. /admin/analytics/:tab?

- Page: `src/pages/admin/AdminDashboardPage.tsx`. Layout: AdminLayout. Quyền: admin.
- Service được tham chiếu: `platformAnalyticsApi.getDashboard`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/admin-analytics/types/platformAnalytics.types.ts` (`StatusMetricItem`, `DailyCountPoint`, `PlatformFeeStatementSummaryItem`, `PlatformFeeStatistics`, `PlatformUserStatistics`, `PlatformShopStatistics`, `PlatformRevenueAndOrderStatistics`, `PlatformFlashDealStatistics`, `PlatformPaymentAndRefundStatistics`, `PlatformAnalyticsSnapshot`, `PlatformDashboardAnalytics`); `src/features/store-analytics/types/storeAnalytics.types.ts` (`AnalyticsPreset`, `AnalyticsPeriodQuery`, `TrendGranularity`, `AnalyticsPeriodInfo`, `RevenueTrendPoint`, `StoreRevenueOverview`, `OrderStatusCountItem`, `StoreOrderStatistics`, `StoreTopFlashDealItem`, `DealStatusItem`, `StoreFlashDealStatistics`, `PaymentMethodBreakdownItem`, `StorePaymentMethodRevenue`, `StoreAnalyticsSnapshot`, `StoreDashboardAnalytics`); `src/shared/types/index.ts` ().
- Hook/state/router: `useAnalyticsPeriod`, `useLocation`, `useParams`, `usePlatformAnalytics`, `useSearchParams`.
- Child component: `src/features/admin-analytics/components/index.ts`, `src/features/store-analytics/components/index.ts`, `src/shared/components/analytics/index.ts`, `src/shared/components/modal/ErrorModal.tsx`, `src/shared/components/modal/BaseModal.tsx`.
- Tương tác source: `onChange`, `onClick`, `onClose`, `onFromDateChange`, `onMonthChange`, `onMouseDown`, `onRefresh`, `onSelectPreset`, `onToDateChange`, `onYearChange`.
- Field đọc trong JSX — `src/pages/admin/AdminDashboardPage.tsx`: `analytics.fromDate`, `analytics.month`, `analytics.preset`, `analytics.rangeError`, `analytics.refresh`, `analytics.selectMonth`, `analytics.selectPreset`, `analytics.selectYear`, `analytics.setFromDate`, `analytics.setToDate`, `analytics.toDate`, `analytics.year`, `data.overall`, `data.period`, `data.selected`, `snapshot.flashDeals`, `snapshot.paymentsAndRefunds`, `snapshot.platformFees`, `snapshot.revenueAndOrders`, `snapshot.shops`, `snapshot.users`.
- Nhóm: dashboard. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R32. /admin/users

- Page: `src/pages/admin/UserManagementPage.tsx`. Layout: AdminLayout. Quyền: admin.
- Service được tham chiếu: `userApi.getUserDetails`, `userApi.getUsers`, `userApi.updateStatus`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/users/types/user.types.ts` (`AdminUser`, `UserLogResponse`, `UserRoleLogResponse`, `UserDetailsResponse`, `UpdateUserStatusRequest`); `src/shared/types/index.ts` ().
- Hook/state/router: `useAuthStore`, `useSearchParams`, `useUserManagement`.
- Child component: `src/features/users/components/index.ts`, `src/shared/components/pagination/index.ts`, `src/shared/components/modal/ErrorModal.tsx`, `src/shared/components/modal/BaseModal.tsx`, `src/shared/components/modal/SearchInput.tsx`.
- Tương tác source: `onAction`, `onChange`, `onClick`, `onClose`, `onConfirm`, `onMouseDown`, `onPageChange`, `onViewDetails`.
- Field đọc trong JSX — `src/pages/admin/UserManagementPage.tsx`: `currentUser?.id`, `details.data`, `details.isLoading`, `details.isOpen`.
- Field đọc trong JSX — `src/shared/components/modal/SearchInput.tsx`: `e.target`.
- Nhóm: list/workspace. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R33. /admin/categories

- Page: `src/pages/admin/CategoryManagementPage.tsx`. Layout: AdminLayout. Quyền: admin.
- Service được tham chiếu: Không có service HTTP được phát hiện. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/categories/types/adminCategory.types.ts` (`Category`, `CategoryFormData`); `src/shared/types/index.ts` ().
- Hook/state/router: `useCategoryManagement`, `useSearchParams`.
- Child component: `src/features/categories/components/index.ts`, `src/shared/components/pagination/index.ts`, `src/shared/components/modal/ErrorModal.tsx`, `src/shared/components/modal/BaseModal.tsx`, `src/shared/components/modal/SearchInput.tsx`.
- Tương tác source: `onChange`, `onClick`, `onClose`, `onConfirm`, `onDelete`, `onEdit`, `onMouseDown`, `onPageChange`, `onSave`, `onToggleStatus`.
- Field đọc trong JSX — `src/shared/components/modal/SearchInput.tsx`: `e.target`.
- Nhóm: list/workspace. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=false, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R34. /admin/trust/:tab?

- Page: `src/pages/admin/TrustConfigurationPage.tsx`. Layout: AdminLayout. Quyền: admin.
- Service được tham chiếu: `adminTrustApi.createLevel`, `adminTrustApi.createPenaltyRule`, `adminTrustApi.deletePenaltyRule`, `adminTrustApi.getEarningRules`, `adminTrustApi.getLevelHistory`, `adminTrustApi.getLevels`, `adminTrustApi.getPenaltyRules`, `adminTrustApi.getScoreRules`, `adminTrustApi.updateEarningRule`, `adminTrustApi.updateLevelBenefits`, `adminTrustApi.updateLevelInfo`, `adminTrustApi.updateLevelRequirement`, `adminTrustApi.updateLevelStatus`, `adminTrustApi.updatePenaltyRule`, `adminTrustApi.updateScoreRule`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/trust/types/adminTrust.types.ts` (`AdminTrustLevel`, `TrustLevelChangeType`, `TrustLevelDepositPolicySnapshot`, `TrustLevelHistoryItem`, `PenaltyRule`, `TrustScoreRuleKind`, `TrustScoreRule`, `TrustLevelDepositRangeInput`, `UpdateLevelBenefitsPayload`, `UpdateLevelRequirementPayload`, `CreateTrustLevelPayload`, `UpdateLevelInfoPayload`, `UpdateLevelStatusPayload`, `CreatePenaltyRulePayload`, `UpdatePenaltyRulePayload`, `UpdateScoreRulePayload`); `src/features/trust/types/trust.types.ts` (`TrustLevelDepositRange`, `TrustLevelDepositPolicy`, `TrustLevelSummary`, `TrustLevelRequirement`, `TrustStanding`, `TrustScoreOverview`, `ProgressMetricKey`, `ProgressMetric`, `LevelProgress`, `TrustLevelInfo`, `TrustScoreEventType`, `TrustScoreHistoryItem`, `PenaltyStatus`, `PenaltyItem`, `PenaltyRecoveryOrder`, `PenaltyRecovery`, `TrustListParams`, `TrustHistoryParams`, `TrustPenaltyParams`); `src/shared/types/index.ts` ().
- Hook/state/router: `useApiResource`, `useAuthStore`, `useFieldArray`, `useForm`, `useParams`, `useToastStore.getState`, `useToastStore.getState().removeToast`, `useWatch`.
- Child component: `src/features/trust/components/AdminLevelsPanel.tsx`, `src/features/trust/components/CreateLevelModal.tsx`, `src/shared/components/modal/BaseModal.tsx`, `src/features/trust/components/FormField.tsx`, `src/features/trust/components/LevelBenefitsModal.tsx`, `src/features/trust/components/LevelHistoryDrawer.tsx`, `src/features/trust/components/PanelState.tsx`, `src/features/trust/components/LevelInfoModal.tsx`, `src/features/trust/components/LevelRequirementModal.tsx`, `src/features/trust/components/LevelStatusModal.tsx`, `src/features/trust/components/PenaltyRulesPanel.tsx`, `src/features/trust/components/PenaltyRuleModal.tsx`, `src/features/trust/components/RuleStatusBadge.tsx`, `src/features/trust/components/ScoreRulesPanel.tsx`, `src/features/trust/components/ScoreRuleModal.tsx`, `src/features/trust/components/TrustTabs.tsx`.
- Tương tác source: `onCancel`, `onChange`, `onClick`, `onClose`, `onConfirm`, `onCreate`, `onMouseDown`, `onRetry`, `onSave`, `onSubmit`.
- Field đọc trong JSX — `src/features/trust/components/AdminLevelsPanel.tsx`: `level.depositPolicy`, `level.description`, `level.id`, `level.isActive`, `level.name`, `level.rank`, `level.requirement`, `level.requirement?.minCompletedOrders`, `level.requirement?.minScore`, `level.requirement?.minTotalOrderValue`, `level.userCount`, `policy.depositFreeOrderAmount`, `policy.maxDepositOrderAmount`, `policy.ranges`, `policy.ranges.length`, `range.id`.
- Field đọc trong JSX — `src/features/trust/components/CreateLevelModal.tsx`: `e.target`, `errors.depositFreeOrderAmount`, `errors.depositFreeOrderAmount?.message`, `errors.description`, `errors.description?.message`, `errors.maxDepositOrderAmount`, `errors.maxDepositOrderAmount?.message`, `errors.minCompletedOrders`, `errors.minCompletedOrders?.message`, `errors.minScore`, `errors.minScore?.message`, `errors.minTotalOrderValue`, `errors.minTotalOrderValue?.message`, `errors.name`, `errors.name?.message`, `errors.ranges`, `errors.ranges.root`, `errors.ranges.root.message`, `errors.ranges?.root`, `errors.ranges?.root?.message`, `field.id`, `fields.length`, `highestLevel.name`, `highestLevel.requirement`, `highestLevel.requirement?.minCompletedOrders`, `highestLevel.requirement?.minScore`, `highestLevel.requirement?.minTotalOrderValue`, `rangeErr.depositAmount`, `rangeErr.depositAmount.message`, `rangeErr.fromOrderAmount`, `rangeErr.fromOrderAmount.message`, `rangeErr.toOrderAmount`, `rangeErr.toOrderAmount.message`, `rangeErr?.depositAmount`, `rangeErr?.fromOrderAmount`, `rangeErr?.toOrderAmount`.
- Field đọc trong JSX — `src/features/trust/components/LevelBenefitsModal.tsx`: `e.target`, `errors.depositFreeOrderAmount`, `errors.depositFreeOrderAmount?.message`, `errors.description`, `errors.description?.message`, `errors.maxDepositOrderAmount`, `errors.maxDepositOrderAmount?.message`, `errors.ranges`, `errors.ranges.root`, `errors.ranges.root.message`, `errors.ranges?.root`, `errors.ranges?.root?.message`, `field.id`, `fields.length`, `level.name`, `level.rank`, `rangeErr.depositAmount`, `rangeErr.depositAmount.message`, `rangeErr.fromOrderAmount`, `rangeErr.fromOrderAmount.message`, `rangeErr.toOrderAmount`, `rangeErr.toOrderAmount.message`, `rangeErr?.depositAmount`, `rangeErr?.fromOrderAmount`, `rangeErr?.toOrderAmount`.
- Field đọc trong JSX — `src/features/trust/components/LevelHistoryDrawer.tsx`: `badge.label`, `badge.style`, `data.length`, `e.currentTarget`, `e.target`, `item.affectedUserCount`, `item.changeType`, `item.changedByName`, `item.depositPolicy`, `item.depositPolicy.depositFreeOrderAmount`, `item.depositPolicy.maxDepositOrderAmount`, `item.depositPolicy.ranges`, `item.depositPolicy.ranges.length`, `item.depositPolicy.requireFullOnlinePayment`, `item.description`, `item.id`, `item.isActive`, `item.minCompletedOrders`, `item.minScore`, `item.name`, `item.rank`, `item.versionNo`, `level.name`, `level.rank`.
- Field đọc trong JSX — `src/features/trust/components/LevelInfoModal.tsx`: `errors.name`, `errors.name?.message`, `level.name`, `level.rank`, `level?.rank`.
- Field đọc trong JSX — `src/features/trust/components/LevelRequirementModal.tsx`: `errors.minCompletedOrders`, `errors.minCompletedOrders?.message`, `errors.minScore`, `errors.minScore?.message`, `errors.minTotalOrderValue`, `errors.minTotalOrderValue?.message`, `level.name`.
- Field đọc trong JSX — `src/features/trust/components/LevelStatusModal.tsx`: `level.name`, `level.rank`, `level.userCount`.
- Field đọc trong JSX — `src/features/trust/components/PenaltyRulesPanel.tsx`: `data.length`, `deletingRule.effectiveTo`, `deletingRule.minimumOccurrence`, `deletingRule.scoreDeduction`, `event.target`, `rule.effectiveTo`, `rule.id`, `rule.minimumOccurrence`, `rule.permanentOfflineLock`, `rule.requiredOnlineOrders`, `rule.scoreDeduction`.
- Field đọc trong JSX — `src/features/trust/components/PenaltyRuleModal.tsx`: `errors.effectiveFrom`, `errors.effectiveFrom?.message`, `errors.effectiveTo`, `errors.effectiveTo?.message`, `errors.minimumOccurrence`, `errors.minimumOccurrence?.message`, `errors.requiredOnlineOrderValue`, `errors.requiredOnlineOrderValue?.message`, `errors.requiredOnlineOrders`, `errors.requiredOnlineOrders?.message`, `errors.scoreDeduction`, `errors.scoreDeduction?.message`, `item.effectiveTo`, `item.id`, `rule.minimumOccurrence`, `sameOccurrenceRules.length`.
- Field đọc trong JSX — `src/features/trust/components/ScoreRulesPanel.tsx`: `copy.description`, `copy.empty`, `copy.title`, `event.target`, `rule.createdByName`, `rule.effectiveTo`, `rule.id`, `rule.ruleType`, `rule.scoreDelta`.
- Field đọc trong JSX — `src/features/trust/components/ScoreRuleModal.tsx`: `errors.scoreDelta`, `errors.scoreDelta?.message`.
- Field đọc trong JSX — `src/features/trust/components/TrustTabs.tsx`: `tab.id`, `tab.label`.
- Nhóm: detail/tabs. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R35. /admin/products

- Page: `src/pages/admin/ProductManagementPage.tsx`. Layout: AdminLayout. Quyền: admin.
- Service được tham chiếu: Không có service HTTP được phát hiện. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/products/types/product.types.ts` (`Option`, `ProductOptions`, `ProductImage`, `ProductValue`, `ProductAttribute`, `ProductVariant`, `ProductListItem`, `Product`, `Page`, `FlashDeal`, `DraftValue`, `DraftAttribute`, `Combination`, `PriceInput`, `PriceMap`, `ProductListQuery`, `ProductSummary`, `ProductListManagement`, `AttributeValueContent`, `ProductContentManagement`, `RemoveViolatingProduct`, `RevisionContentSnapshot`, `ProductRevisionHistory`); `src/shared/types/index.ts` (); `src/shared/types/moderationAction.types.ts` (`ModerationActionResult`).
- Hook/state/router: .
- Child component: `src/features/products/components/ProductManagementTable.tsx`, `src/shared/components/pagination/index.ts`, `src/shared/components/modal/SearchInput.tsx`.
- Tương tác source: `onChange`, `onClick`, `onPageChange`.
- Field đọc trong JSX — `src/pages/admin/ProductManagementPage.tsx`: `items.length`.
- Field đọc trong JSX — `src/features/products/components/ProductManagementTable.tsx`: `product.categoryName`, `product.id`, `product.latestReview`, `product.latestReview?.status`, `product.name`, `product.shopName`, `product.status`, `review.correctionSubmitted`.
- Field đọc trong JSX — `src/shared/components/modal/SearchInput.tsx`: `e.target`.
- Nhóm: list/workspace. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R36. /admin/products/:productId

- Page: `src/pages/admin/ProductContentManagementPage.tsx`. Layout: AdminLayout. Quyền: admin.
- Service được tham chiếu: `productApi.approveContent`, `productApi.removeForViolation`, `warningApi.issueForProduct`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/products/types/product.types.ts` (`Option`, `ProductOptions`, `ProductImage`, `ProductValue`, `ProductAttribute`, `ProductVariant`, `ProductListItem`, `Product`, `Page`, `FlashDeal`, `DraftValue`, `DraftAttribute`, `Combination`, `PriceInput`, `PriceMap`, `ProductListQuery`, `ProductSummary`, `ProductListManagement`, `AttributeValueContent`, `ProductContentManagement`, `RemoveViolatingProduct`, `RevisionContentSnapshot`, `ProductRevisionHistory`); `src/features/warnings/types/warning.types.ts` (`Warning`, `WarningPage`, `IssueViolationWarning`); `src/shared/types/index.ts` (); `src/shared/types/moderationAction.types.ts` (`ModerationActionResult`).
- Hook/state/router: `useParams`.
- Child component: `src/features/products/components/ProductContentManagement.tsx`, `src/features/products/components/ProductRevisionHistory.tsx`, `src/features/products/components/AttributeValueReview.tsx`, `src/features/products/components/WarningCorrectionReview.tsx`, `src/features/products/components/ProductModerationActions.tsx`.
- Tương tác source: `onChange`, `onClick`, `onRemoved`, `onReviewed`, `onSubmit`.
- Field đọc trong JSX — `src/pages/admin/ProductContentManagementPage.tsx`: `product.id`, `product.latestReview`, `product.name`, `product.warningComparison`, `styles.border`, `styles.icon`, `styles.title`.
- Field đọc trong JSX — `src/features/products/components/ProductContentManagement.tsx`: `activeImage.url`, `activeImage.value`, `attribute.id`, `attribute.name`, `attribute.values`, `attribute.values.length`, `image.id`, `image.url`, `p.attributes`, `p.attributes.length`, `p.categoryName`, `p.description`, `p.id`, `p.images`, `p.images.length`, `p.latestReview`, `p.latestReview.note`, `p.name`, `p.revisionHistory`, `p.shopName`, `p.status`, `p.variants`, `p.variants.length`, `p.warningComparison`, `resolved.attribute`, `resolved.value`, `review.accent`, `review.label`, `review.tone`, `stat.label`, `stat.value`, `value.id`, `value.value`, `variant.dealPrice`, `variant.id`, `variant.isActive`, `variant.originalPrice`, `variant.sku`, `variant.valueIds`, `variant.valueIds.length`.
- Field đọc trong JSX — `src/features/products/components/ProductRevisionHistory.tsx`: `fields.length`, `items.length`, `r.after`, `r.after.attributeValues`, `r.after.attributeValues.length`, `r.after.categoryId`, `r.after.description`, `r.after.name`, `r.before`, `r.before.attributeValues`, `r.before?.categoryId`, `r.before?.description`, `r.before?.name`, `r.id`, `r.isLatest`, `r.requiredCorrection`, `r.reviewNote`, `r.reviewedAt`, `r.reviewedByName`, `r.reviewedByUserId`, `r.revisionNumber`, `r.status`, `r.submittedByName`, `r.submittedByUserId`, `r.warningDueAt`, `r.warningId`, `r.warningReason`, `revisions.length`, `s.categoryName`, `s.description`, `s.images`, `s.images.length`, `s.name`, `v.attributeName`, `v.imageUrl`, `v.value`, `v.valueId`.
- Field đọc trong JSX — `src/features/products/components/AttributeValueReview.tsx`: `item.attributeName`, `item.imageUrl`, `item.value`, `row.after`, `row.before`, `row.changed`, `row.id`, `value.attributeName`, `value.value`.
- Field đọc trong JSX — `src/features/products/components/WarningCorrectionReview.tsx`: `c.after`, `c.after.attributeValues`, `c.before`, `c.before.attributeValues`, `c.dealBlocked`, `c.firstEditedAt`, `c.reason`, `c.requiredCorrection`, `c[other].images`, `c[side].images`, `c[side].images.length`, `e.target`, `field.key`, `field.label`.
- Field đọc trong JSX — `src/features/products/components/ProductModerationActions.tsx`: `current.summary`, `e.target`, `reviewState?.dot`, `reviewState?.label`, `value.length`.
- Nhóm: detail/tabs. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R37. /admin/orders

- Page: `src/pages/commons/ComingSoonPage.tsx`. Layout: AdminLayout. Quyền: admin.
- Service được tham chiếu: Không có service HTTP được phát hiện. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: Không có DTO import trực tiếp; xem generic service/props.
- Hook/state/router: .
- Child component: Inline trong page.
- Tương tác source: Navigation/static content.
- Nhóm: list/workspace. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=false, empty=false, error=false. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R38. /admin/stores

- Page: `src/pages/admin/ShopManagementPage.tsx`. Layout: AdminLayout. Quyền: admin.
- Service được tham chiếu: Không có service HTTP được phát hiện. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/shops/types/ownerShop.types.ts` (`ShopStatus`, `ShopProfileDocument`, `OwnerShop`, `UpdateBusinessInfo`, `ShopPaymentConfig`, `UpdateShopPaymentConfigRequest`); `src/features/shops/types/shop.types.ts` (`NearbyShop`, `ShopProfile`, `StoreReviewReply`, `StoreReview`, `StoreReviewsSummary`, `NearbyShopsPagedResponse`, `NearbyShopsRequest`, `StoreReviewsQueryRequest`); `src/features/shops/types/shopManagement.types.ts` (`ShopOwnerManagement`, `ShopListManagement`, `ShopProfileDocument`, `ShopStatusHistory`, `ShopContentManagement`, `UpdateShopModerationStatus`); `src/shared/types/index.ts` (); `src/shared/types/moderationAction.types.ts` (`ModerationActionResult`).
- Hook/state/router: .
- Child component: `src/features/shops/components/ShopManagementTable.tsx`, `src/features/shops/components/ShopProfileHeader.tsx`, `src/shared/components/pagination/index.ts`, `src/shared/components/modal/SearchInput.tsx`.
- Tương tác source: `onChange`, `onClick`, `onPageChange`.
- Field đọc trong JSX — `src/pages/admin/ShopManagementPage.tsx`: `items.length`.
- Field đọc trong JSX — `src/features/shops/components/ShopManagementTable.tsx`: `shop.city`, `shop.description`, `shop.id`, `shop.name`, `shop.ownerName`, `shop.status`.
- Field đọc trong JSX — `src/features/shops/components/ShopProfileHeader.tsx`: `meta.badge`, `meta.dot`, `meta.label`, `shop.coverImageUrl`, `shop.logoUrl`, `shop.name`.
- Field đọc trong JSX — `src/shared/components/modal/SearchInput.tsx`: `e.target`.
- Nhóm: list/workspace. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R39. /admin/stores/:shopId

- Page: `src/pages/admin/ShopContentManagementPage.tsx`. Layout: AdminLayout. Quyền: admin.
- Service được tham chiếu: `shopApi.updateModerationStatus`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/shops/types/ownerShop.types.ts` (`ShopStatus`, `ShopProfileDocument`, `OwnerShop`, `UpdateBusinessInfo`, `ShopPaymentConfig`, `UpdateShopPaymentConfigRequest`); `src/features/shops/types/shop.types.ts` (`NearbyShop`, `ShopProfile`, `StoreReviewReply`, `StoreReview`, `StoreReviewsSummary`, `NearbyShopsPagedResponse`, `NearbyShopsRequest`, `StoreReviewsQueryRequest`); `src/features/shops/types/shopManagement.types.ts` (`ShopOwnerManagement`, `ShopListManagement`, `ShopProfileDocument`, `ShopStatusHistory`, `ShopContentManagement`, `UpdateShopModerationStatus`); `src/shared/types/index.ts` (); `src/shared/types/moderationAction.types.ts` (`ModerationActionResult`).
- Hook/state/router: `useParams`.
- Child component: `src/features/shops/components/ShopContentManagement.tsx`, `src/features/shops/components/ShopProfileHeader.tsx`, `src/features/shops/components/ShopStatusHistory.tsx`, `src/features/shops/components/ShopModerationActions.tsx`.
- Tương tác source: `onChange`, `onClick`, `onDismiss`, `onSubmit`, `onUpdated`.
- Field đọc trong JSX — `src/pages/admin/ShopContentManagementPage.tsx`: `event.currentTarget`, `event.target`, `notice.message`, `shop.currentStatusReason`, `shop.id`, `shop.name`, `shop.status`, `shop.statusHistory`, `statusStyle.banner`, `statusStyle?.dot`.
- Field đọc trong JSX — `src/features/shops/components/ShopContentManagement.tsx`: `d.documentType`, `d.fileUrl`, `d.id`, `d.originalFileName`, `documents.length`, `owner?.email`, `owner?.fullName`, `owner?.phoneNumber`, `s.businessLicenseNo`, `s.closingTime`, `s.coverImageUrl`, `s.description`, `s.latitude`, `s.logoUrl`, `s.longitude`, `s.name`, `s.openingTime`, `s.status`.
- Field đọc trong JSX — `src/features/shops/components/ShopProfileHeader.tsx`: `meta.badge`, `meta.dot`, `meta.label`, `shop.coverImageUrl`, `shop.logoUrl`, `shop.name`.
- Field đọc trong JSX — `src/features/shops/components/ShopStatusHistory.tsx`: `(styles[status] ?? fallback).badge`, `entries.length`, `entries[0]?.id`, `group.items`, `group.items.length`, `group.label`, `item.createdAt`, `item.fromStatus`, `item.id`, `item.reason`, `item.toStatus`, `style.border`, `style.circle`, `style.icon`.
- Field đọc trong JSX — `src/features/shops/components/ShopModerationActions.tsx`: `availableOptions.length`, `event.target`, `option?.pill`, `reason.length`, `selectedOption.label`, `selectedOption?.submit`.
- Nhóm: detail/tabs. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R40. /admin/shop-applications

- Page: `src/pages/admin/AdminShopApplicationsPage.tsx`. Layout: AdminLayout. Quyền: admin.
- Service được tham chiếu: `adminShopApplicationApi.getAll`, `adminShopApplicationApi.getById`, `adminShopApplicationApi.review`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/shop-applications/types/adminShopApplication.types.ts` (`ShopApplicationReviewDecision`, `ShopApplicationAdminFilter`); `src/shared/types/index.ts` ().
- Hook/state/router: `usePagination`.
- Child component: `src/shared/components/pagination/Pagination.tsx`, `src/shared/components/pagination/usePagination.ts`, `src/features/shop-applications/components/AdminShopApplicationDetail.tsx`, `src/features/shop-applications/components/AdminShopApplicationShared.tsx`, `src/features/shop-applications/components/AdminShopApplicationsList.tsx`.
- Tương tác source: `onBack`, `onChange`, `onClick`, `onNoteChange`, `onPageChange`, `onReview`, `onSortChange`.
- Field đọc trong JSX — `src/shared/components/pagination/Pagination.tsx`: `sizeClasses.btn`, `sizeClasses.gap`, `sizeClasses.iconSize`, `sizeClasses.navBtn`.
- Field đọc trong JSX — `src/features/shop-applications/components/AdminShopApplicationDetail.tsx`: `app.bankAccountHolder`, `app.bankAccountNumber`, `app.bankName`, `app.businessLicenseNo`, `app.closingTime`, `app.coverImageUrl`, `app.description`, `app.hasPayosConfiguration`, `app.logoUrl`, `app.name`, `app.openingTime`, `app.revisionNumber`, `app.status`, `currentDocuments.length`, `doc.documentType`, `doc.fileUrl`, `doc.id`, `doc.originalFileName`, `e.target`, `finalMeta.dot`, `finalMeta.icon`, `finalMeta.label`, `getStatusMeta(log.fromStatus).label`, `log.createdAt`, `log.fromStatus`, `log.note`, `log.revisionNumber`, `log.toStatus`, `logs.length`, `meta.dot`, `meta.icon`, `meta.label`, `note.length`.
- Field đọc trong JSX — `src/features/shop-applications/components/AdminShopApplicationsList.tsx`: `getStatusMeta(filter).label`, `getStatusMeta(status).label`, `item.businessLicenseNo`, `item.id`, `item.logoUrl`, `item.name`, `item.revisionNumber`, `item.status`, `rows.length`.
- Nhóm: list/workspace. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R41. /admin/platform-fees

- Page: `src/pages/admin/PlatformFeeManagementPage.tsx`. Layout: AdminLayout. Quyền: admin.
- Service được tham chiếu: `platformFeeApi.generateMonthly`, `platformFeeApi.getActiveConfig`, `platformFeeApi.getAdminStatements`, `platformFeeApi.getAllConfigs`, `platformFeeApi.processOverdue`, `platformFeeApi.recordExternalPayment`, `platformFeeApi.updateConfig`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/platform-fees/types/platformFee.types.ts` (`PlatformFeeStatementStatus`, `PlatformFeeConfigResponse`, `GetAdminPlatformFeesParams`, `UpdatePlatformFeeConfigRequest`, `PlatformFeeStatementSummary`, `OrderPlatformFeeItem`, `PlatformFeeAdjustmentItem`, `PlatformFeePaymentItem`, `PlatformFeeStatementHistoryItem`, `PlatformFeeStatementDetail`, `PlatformFeePaymentLinkResponse`, `GetShopPlatformFeesParams`, `RecordExternalPaymentRequest`, `PagedResult`); `src/shared/types/index.ts` ().
- Hook/state/router: `useToastStore.getState`, `useToastStore.getState().removeToast`.
- Child component: `src/features/platform-fees/components/StatusBadge.tsx`, `src/features/platform-fees/components/ManualReconciliationModal.tsx`, `src/features/platform-fees/components/AdminStatementDetailModal.tsx`, `src/shared/components/pagination/index.ts`.
- Tương tác source: `onChange`, `onClick`, `onClose`, `onOpenReconcile`, `onPageChange`, `onSubmit`, `onSuccess`.
- Field đọc trong JSX — `src/pages/admin/PlatformFeeManagementPage.tsx`: `config.feePercentage`, `config.feeRate`, `config?.createdAt`, `config?.effectiveFrom`, `e.target`, `e.target.value`, `s.id`, `statements.length`, `stmt.billingMonth`, `stmt.billingYear`, `stmt.dueAt`, `stmt.id`, `stmt.paidAt`, `stmt.shopName`, `stmt.status`, `upcomingConfig.effectiveFrom`, `upcomingConfig.feePercentage`, `upcomingConfig.feeRate`.
- Field đọc trong JSX — `src/features/platform-fees/components/ManualReconciliationModal.tsx`: `e.target`, `selectedStatement.billingMonth`, `selectedStatement.billingYear`, `selectedStatement.id`, `selectedStatement.shopName`, `selectedStatement.status`, `stmt.billingMonth`, `stmt.billingYear`, `stmt.id`, `stmt.shopName`, `unpaidList.length`.
- Field đọc trong JSX — `src/features/platform-fees/components/AdminStatementDetailModal.tsx`: `adj.id`, `adj.reason`, `adj.type`, `h.adjustmentAmount`, `h.changeAmount`, `h.changeType`, `h.id`, `h.reason`, `ord.feeRate`, `ord.id`, `ord.orderId`, `p.createdAt`, `p.gatewayTransactionId`, `p.id`, `p.paidAt`, `p.paymentMethod`, `p.status`, `statement.adjustments`, `statement.adjustments.length`, `statement.adjustments?.length`, `statement.billingMonth`, `statement.billingYear`, `statement.histories`, `statement.histories.length`, `statement.histories?.length`, `statement.orders`, `statement.orders.length`, `statement.orders?.length`, `statement.paidAt`, `statement.payments`, `statement.payments.length`, `statement.payments?.length`, `statement.shopName`, `statement.status`.
- Nhóm: list/workspace. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R42. /admin/feedback

- Page: `src/pages/admin/FeedbackManagementPage.tsx`. Layout: AdminLayout. Quyền: admin.
- Service được tham chiếu: `feedbackApi.remove`, `feedbackApi.restore`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/feedback/types/feedback.types.ts` (`FeedbackStatus`, `Feedback`, `FeedbackPage`); `src/shared/types/index.ts` ().
- Hook/state/router: .
- Child component: `src/features/feedback/components/FeedbackModerationDialog.tsx`, `src/features/feedback/components/FeedbackTable.tsx`, `src/shared/components/pagination/index.ts`, `src/shared/components/modal/SearchInput.tsx`.
- Tương tác source: `onChange`, `onClick`, `onClose`, `onConfirm`, `onKeyDown`, `onModerate`, `onMouseDown`, `onPageChange`.
- Field đọc trong JSX — `src/pages/admin/FeedbackManagementPage.tsx`: `event.target`, `items.length`, `selected.id`.
- Field đọc trong JSX — `src/features/feedback/components/FeedbackModerationDialog.tsx`: `event.ctrlKey`, `event.currentTarget`, `event.key`, `event.metaKey`, `event.target`, `feedback.comment`, `feedback.currentReason`, `feedback.productName`, `feedback.rating`, `feedback.shopName`, `reason.length`, `theme.badge`, `theme.button`, `theme.confirm`, `theme.description`, `theme.glow`, `theme.iconWrap`, `theme.label`, `theme.placeholder`, `theme.ring`, `theme.title`.
- Field đọc trong JSX — `src/features/feedback/components/FeedbackTable.tsx`: `column.className`, `column.label`, `item.comment`, `item.customerEmail`, `item.customerName`, `item.customerPhone`, `item.id`, `item.productName`, `item.rating`, `item.shopName`, `style.badge`, `style.dot`, `style.label`.
- Field đọc trong JSX — `src/shared/components/modal/SearchInput.tsx`: `e.target`.
- Nhóm: list/workspace. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R43. /admin/reviews

- Page: `src/pages/admin/AdminReviewsPage.tsx`. Layout: AdminLayout. Quyền: admin.
- Service được tham chiếu: `feedbackApi.deleteReply`, `feedbackApi.editReply`, `feedbackApi.getAdminFeedbacks`, `feedbackApi.replyFeedback`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/feedbacks/types/feedback.types.ts` (`FeedbackReply`, `FeedbackItem`, `CreateFeedbackInput`, `UpdateFeedbackInput`, `CreateFeedbackReplyInput`, `UpdateFeedbackReplyInput`, `ShopFeedbacksQueryParams`, `FeedbackHistoryResponse`); `src/shared/types/index.ts` ().
- Hook/state/router: `useToastStore.getState`, `useToastStore.getState().removeToast`.
- Child component: `src/features/feedbacks/components/ShopFeedbackManagementPanel.tsx`, `src/shared/components/pagination/index.ts`, `src/shared/components/modal/BaseModal.tsx`.
- Tương tác source: `onChange`, `onClick`, `onClose`, `onDeleteReply`, `onEditReply`, `onMouseDown`, `onPageChange`, `onRatingChange`, `onReply`, `onReplyFilterChange`, `onSubmit`, `onTodayToggle`.
- Field đọc trong JSX — `src/features/feedbacks/components/ShopFeedbackManagementPanel.tsx`: `e.target`, `editReplyContent.length`, `feedbacks.length`, `item.comment`, `item.createdAt`, `item.customerAvatarUrl`, `item.customerName`, `item.id`, `item.imageUrl`, `item.images`, `item.images.length`, `item.orderCode`, `item.productName`, `item.rating`, `item.reply`, `item.reply.content`, `item.reply.createdAt`, `item.reply.repliedByName`, `item.shopName`, `item.variantName`, `replyContent.length`, `replyTarget.comment`, `replyTarget?.comment`, `replyTarget?.customerName`, `replyTarget?.productName`, `replyTarget?.rating`.
- Nhóm: list/workspace. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=false, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R44. /admin/audit-logs

- Page: `src/pages/admin/AdminAuditLogsPage.tsx`. Layout: AdminLayout. Quyền: admin.
- Service được tham chiếu: `auditLogApi.getLogs`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/audit-logs/types/auditLog.types.ts` (`AuditLogItem`, `AuditLogFilterOptions`, `AuditLogQueryParams`, `AuditLogPageResult`); `src/shared/types/index.ts` ().
- Hook/state/router: `useAuditLogs`, `useDebounce`, `useSearchParams`.
- Child component: `src/features/audit-logs/components/index.ts`, `src/shared/components/modal/ErrorModal.tsx`, `src/shared/components/modal/BaseModal.tsx`, `src/shared/components/pagination/index.ts`.
- Tương tác source: `onActionChange`, `onActorTypeChange`, `onClick`, `onClose`, `onFromDateChange`, `onInspect`, `onMouseDown`, `onPageChange`, `onReset`, `onSearchChange`, `onTargetTypeChange`, `onToDateChange`.
- Nhóm: list/workspace. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R45. /staff

- Redirect source: `<Navigate to={APP_PATHS.HOME} replace />`. Không có UI riêng.

### R46. /staff/shops/:shopId

- Redirect source: `<Navigate to="flash-deals" replace />`. Không có UI riêng.

### R47. /staff/shops/:shopId/flash-deals

- Page: `src/pages/staff/StaffFlashDealsPage.tsx`. Layout: StaffLayout. Quyền: staff, storeOwner.
- Service được tham chiếu: `flashDealApi.getShopFlashDeals`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/flash-deals/types/flashDeal.types.ts` (`DealAttributeGroup`, `FlashDealVariant`, `FlashDeal`, `CursorMode`, `FlashDealCursorParams`, `FlashDealCursorResult`, `DealFilterState`, `FlashDealStatusEnum`, `FlashDealVariantStatusEnum`, `ShopFlashDealListItem`, `ShopFlashDealVariantDetail`, `ShopFlashDealDetail`, `ShopEligibleProductVariant`, `ShopEligibleProduct`, `CreateFlashDealVariantPayload`, `CreateFlashDealPayload`, `UpdateFlashDealVariantPayload`, `UpdateFlashDealPayload`, `ShopFlashDealQueryParams`); `src/shared/types/index.ts` ().
- Hook/state/router: `useNavigate`, `useParams`, `useToastStore.getState`, `useToastStore.getState().removeToast`.
- Child component: Inline trong page.
- Tương tác source: `onChange`, `onClick`.
- Field đọc trong JSX — `src/pages/staff/StaffFlashDealsPage.tsx`: `FlashDealStatusEnum.Hidden`, `deal.availableQuantity`, `deal.categoryName`, `deal.id`, `deal.maxDiscountPercent`, `deal.minDealPrice`, `deal.productImageUrl`, `deal.productName`, `deal.reservedQuantity`, `deal.soldQuantity`, `deal.status`, `deal.totalQuantity`, `deals.length`, `e.target`, `filteredDeals.length`, `stats.ended`, `stats.hidden`, `stats.pending`, `stats.remaining`, `stats.running`, `stats.sold`, `stats.total`, `stats.totalDeals`, `tabCounts.all`, `tabCounts.ended`, `tabCounts.hidden`, `tabCounts.pending`, `tabCounts.running`.
- Nhóm: detail/tabs. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R48. /staff/shops/:shopId/flash-deals/:id

- Page: `src/pages/staff/StaffFlashDealDetailPage.tsx`. Layout: StaffLayout. Quyền: staff, storeOwner.
- Service được tham chiếu: `flashDealApi.getShopFlashDealDetail`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/flash-deals/types/flashDeal.types.ts` (`DealAttributeGroup`, `FlashDealVariant`, `FlashDeal`, `CursorMode`, `FlashDealCursorParams`, `FlashDealCursorResult`, `DealFilterState`, `FlashDealStatusEnum`, `FlashDealVariantStatusEnum`, `ShopFlashDealListItem`, `ShopFlashDealVariantDetail`, `ShopFlashDealDetail`, `ShopEligibleProductVariant`, `ShopEligibleProduct`, `CreateFlashDealVariantPayload`, `CreateFlashDealPayload`, `UpdateFlashDealVariantPayload`, `UpdateFlashDealPayload`, `ShopFlashDealQueryParams`); `src/shared/types/index.ts` ().
- Hook/state/router: `useNavigate`, `useParams`, `useToastStore.getState`, `useToastStore.getState().removeToast`.
- Child component: Inline trong page.
- Tương tác source: `onClick`.
- Field đọc trong JSX — `src/pages/staff/StaffFlashDealDetailPage.tsx`: `FlashDealVariantStatusEnum.Hide`, `deal.categoryName`, `deal.description`, `deal.primaryImageUrl`, `deal.productName`, `deal.variants`, `summary.percent`, `summary.remainingQty`, `summary.reservedQty`, `summary.revenue`, `summary.soldQty`, `summary.totalQty`, `v.availableQuantity`, `v.dealPrice`, `v.discountPercent`, `v.id`, `v.originalPrice`, `v.reservedQuantity`, `v.sku`, `v.soldQuantity`, `v.status`, `v.totalQuantity`, `v.variantName`.
- Nhóm: detail/tabs. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=false, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R49. /staff/shops/:shopId/reviews

- Page: `src/pages/store-owner/StoreOwnerReviewsPage.tsx`. Layout: StaffLayout. Quyền: staff, storeOwner.
- Service được tham chiếu: `feedbackApi.deleteReply`, `feedbackApi.editReply`, `feedbackApi.getShopFeedbacks`, `feedbackApi.replyFeedback`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/feedbacks/types/feedback.types.ts` (`FeedbackReply`, `FeedbackItem`, `CreateFeedbackInput`, `UpdateFeedbackInput`, `CreateFeedbackReplyInput`, `UpdateFeedbackReplyInput`, `ShopFeedbacksQueryParams`, `FeedbackHistoryResponse`); `src/shared/types/index.ts` ().
- Hook/state/router: `useToastStore.getState`, `useToastStore.getState().removeToast`.
- Child component: `src/features/feedbacks/components/ShopFeedbackManagementPanel.tsx`, `src/shared/components/pagination/index.ts`, `src/shared/components/modal/BaseModal.tsx`.
- Tương tác source: `onChange`, `onClick`, `onClose`, `onDeleteReply`, `onEditReply`, `onMouseDown`, `onPageChange`, `onRatingChange`, `onReply`, `onReplyFilterChange`, `onSubmit`, `onTodayToggle`.
- Field đọc trong JSX — `src/features/feedbacks/components/ShopFeedbackManagementPanel.tsx`: `e.target`, `editReplyContent.length`, `feedbacks.length`, `item.comment`, `item.createdAt`, `item.customerAvatarUrl`, `item.customerName`, `item.id`, `item.imageUrl`, `item.images`, `item.images.length`, `item.orderCode`, `item.productName`, `item.rating`, `item.reply`, `item.reply.content`, `item.reply.createdAt`, `item.reply.repliedByName`, `item.shopName`, `item.variantName`, `replyContent.length`, `replyTarget.comment`, `replyTarget?.comment`, `replyTarget?.customerName`, `replyTarget?.productName`, `replyTarget?.rating`.
- Nhóm: detail/tabs. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=false, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R50. /staff/shops/:shopId/orders

- Page: `src/pages/shop/ShopOrdersPage.tsx`. Layout: StaffLayout. Quyền: staff, storeOwner.
- Service được tham chiếu: `shopOrderApi.page`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/checkout/types/checkout.types.ts` (`CheckoutMethod`, `DepositRange`, `CheckoutPolicy`, `CheckoutContext`, `PickupEstimate`, `CheckoutRoute`, `CheckoutQuote`, `CheckoutGps`, `CheckoutOrder`, `OrderHistoryPage`); `src/features/shop-orders/types/shopOrder.types.ts` (`OrderRefund`, `ShopOrder`, `ShopOrderPage`); `src/shared/types/index.ts` ().
- Hook/state/router: `useAuthStore`, `useParams`, `useShopOrderEventsStore`.
- Child component: `src/shared/components/pagination/index.ts`, `src/features/shop-orders/components/ConfirmationCountdown.tsx`, `src/features/checkout/components/OrderStatusPanel.tsx`, `src/features/checkout/components/OrderPickupDeadlines.tsx`.
- Tương tác source: `onChange`, `onClick`, `onPageChange`, `onSubmit`.
- Field đọc trong JSX — `src/pages/shop/ShopOrdersPage.tsx`: `Intl.DateTimeFormat`, `data.page`, `data.page.page`, `data.page.pageSize`, `data.page.totalItems`, `data.page.totalPages`, `data?.page`, `data?.page.items`, `data?.page.items.length`, `data?.statusCounts`, `e.target`, `e.target.value`, `generation.current`, `item.flashDealVariantId`, `item.imageUrl`, `item.productName`, `item.quantity`, `item.variantName`, `row.confirmationDeadlineAt`, `row.customerName`, `row.customerPhone`, `row.order`, `row.order.id`, `row.order.items`, `row.order.items.length`, `row.order.orderCode`, `row.order.outstandingAmount`, `row.order.paidAmount`, `row.order.totalAmount`.
- Field đọc trong JSX — `src/features/shop-orders/components/ConfirmationCountdown.tsx`: `remaining.label`, `remaining.urgent`.
- Field đọc trong JSX — `src/features/checkout/components/OrderStatusPanel.tsx`: `order.paymentMethod`, `order.paymentReviewRequired`, `order.remainingAtShop`, `order.status`, `payment.color`, `payment.label`, `state.color`, `state.description`, `state.label`.
- Field đọc trong JSX — `src/features/checkout/components/OrderPickupDeadlines.tsx`: `order.hardDeadlineAt`, `order.softDeadlineAt`.
- Nhóm: detail/tabs. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R51. /staff/shops/:shopId/orders/:orderId

- Page: `src/pages/shop/ShopOrderDetailPage.tsx`. Layout: StaffLayout. Quyền: staff, storeOwner.
- Service được tham chiếu: `refundApi.qr`, `refundApi.review`, `refundApi.transfer`, `refundApi.upload`, `shopOrderApi.action`, `shopOrderApi.detail`, `shopOrderApi.simulateRemaining`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/checkout/types/checkout.types.ts` (`CheckoutMethod`, `DepositRange`, `CheckoutPolicy`, `CheckoutContext`, `PickupEstimate`, `CheckoutRoute`, `CheckoutQuote`, `CheckoutGps`, `CheckoutOrder`, `OrderHistoryPage`); `src/features/refunds/types/refund.types.ts` (`Refund`, `RefundBank`, `RefundPage`, `RefundImage`, `RefundQr`); `src/features/shop-orders/types/shopOrder.types.ts` (`OrderRefund`, `ShopOrder`, `ShopOrderPage`); `src/shared/types/index.ts` ().
- Hook/state/router: `useAuthStore`, `useAuthStore.getState`, `useParams`, `useShopOrderEventsStore`.
- Child component: `src/features/refunds/components/ShopOrderRefunds.tsx`, `src/features/refunds/components/ShopRefundCard.tsx`, `src/features/refunds/components/RefundItems.tsx`, `src/features/refunds/components/RefundEvidenceGallery.tsx`, `src/shared/components/modal/BaseModal.tsx`, `src/features/refunds/components/RefundCustomerInfo.tsx`, `src/features/refunds/components/RefundFileUpload.tsx`, `src/features/refunds/components/RefundStatusBadge.tsx`, `src/features/shop-orders/components/ConfirmationCountdown.tsx`, `src/features/shop-orders/components/HistoryActor.tsx`, `src/features/checkout/components/OrderStatusPanel.tsx`, `src/features/checkout/components/OrderPickupDeadlines.tsx`, `src/features/shop-orders/components/PaymentQr.tsx`.
- Tương tác source: `onChange`, `onChanged`, `onClick`, `onClose`, `onError`, `onMouseDown`, `onSubmit`.
- Field đọc trong JSX — `src/pages/shop/ShopOrderDetailPage.tsx`: `data.confirmationDeadlineAt`, `data.customerName`, `data.customerPhone`, `data.history`, `data.isOwner`, `data.payments`, `e.target`, `event.actorId`, `event.actorName`, `event.actorRole`, `event.note`, `event.toStatus`, `item.flashDealVariantId`, `item.imageUrl`, `item.productName`, `item.quantity`, `item.variantName`, `o.items`, `o.orderCode`, `o.outstandingAmount`, `o.paidAmount`, `o.paymentMethod`, `o.paymentReviewRequired`, `o.remainingAtShop`, `o.status`, `o.totalAmount`, `p.id`, `p.paidAt`, `p.status`, `p.type`, `remaining.checkoutUrl`, `remaining.id`, `remaining.qrCode`, `remaining?.checkoutUrl`, `remaining?.collectedAmount`, `remaining?.status`.
- Field đọc trong JSX — `src/features/refunds/components/ShopOrderRefunds.tsx`: `changed.id`, `item.id`, `refund.amount`, `refund.id`, `refund.status`.
- Field đọc trong JSX — `src/features/refunds/components/ShopRefundCard.tsx`: `currentQr.imageUrl`, `currentQr.transferContent`, `entry.actorName`, `entry.actorRole`, `entry.decision`, `entry.id`, `entry.note`, `event.target`, `item.evidenceUrls`, `item.evidenceUrls?.length`, `proof.length`, `refund.accountHolder`, `refund.accountNumber`, `refund.amount`, `refund.bankName`, `refund.customerDetailsDeadlineAt`, `refund.destinationConfirmedAt`, `refund.evidenceUrls`, `refund.history`, `refund.history.length`, `refund.items`, `refund.reason`, `refund.rejectReason`, `refund.reviewDeadlineAt`, `refund.status`, `refund.transferProofUrls`, `refund.transferProofUrls.length`, `refund.transferReference`, `refund.transferredAmount`.
- Field đọc trong JSX — `src/features/refunds/components/RefundItems.tsx`: `item.evidenceUrls`, `item.orderItemId`, `item.productName`, `item.purchasedQuantity`, `item.quantity`, `item.reason`, `item.variantName`, `refund.items`.
- Field đọc trong JSX — `src/features/refunds/components/RefundCustomerInfo.tsx`: `refund.customerAvatarUrl`, `refund.customerLevelName`, `refund.customerLevelRank`, `refund.customerName`, `refund.customerPhone`.
- Field đọc trong JSX — `src/features/refunds/components/RefundFileUpload.tsx`: `event.target`, `event.target.files`, `event.target.value`, `file.name`, `file.size`, `files.length`, `ref.current`, `selected.length`.
- Field đọc trong JSX — `src/features/refunds/components/RefundStatusBadge.tsx`: `colors.Withdrawn`.
- Field đọc trong JSX — `src/features/shop-orders/components/ConfirmationCountdown.tsx`: `remaining.label`, `remaining.urgent`.
- Field đọc trong JSX — `src/features/shop-orders/components/HistoryActor.tsx`: `actor.name`, `actor.role`.
- Field đọc trong JSX — `src/features/checkout/components/OrderStatusPanel.tsx`: `order.paymentMethod`, `order.paymentReviewRequired`, `order.remainingAtShop`, `order.status`, `payment.color`, `payment.label`, `state.color`, `state.description`, `state.label`.
- Field đọc trong JSX — `src/features/checkout/components/OrderPickupDeadlines.tsx`: `order.hardDeadlineAt`, `order.softDeadlineAt`.
- Nhóm: detail/tabs. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R52. /staff/shops/:shopId/info

- Page: `src/pages/staff/StaffShopDetailPage.tsx`. Layout: StaffLayout. Quyền: staff, storeOwner.
- Service được tham chiếu: `staffApi.getShopDetail`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/staff-shops/types/staff.types.ts` (`ShopStaffStatus`, `AssociatedShop`, `StaffShop`, `StaffInfo`, `StaffShopDetail`); `src/shared/types/index.ts` ().
- Hook/state/router: `useNavigate`, `useParams`.
- Child component: Inline trong page.
- Tương tác source: `onClick`.
- Field đọc trong JSX — `src/pages/staff/StaffShopDetailPage.tsx`: `shop.coverImageUrl`, `shop.description`, `shop.logoUrl`, `shop.name`, `staff.displayName`, `staff.staffNickname`, `staff.userEmail`.
- Nhóm: detail/tabs. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=false, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R53. /store-owner

- Page: `src/pages/store-owner/StoreOwnerDashboardPage.tsx`. Layout: StoreOwnerLayout. Quyền: storeOwner.
- Service được tham chiếu: `storeAnalyticsApi.getDashboard`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/store-analytics/types/storeAnalytics.types.ts` (`AnalyticsPreset`, `AnalyticsPeriodQuery`, `TrendGranularity`, `AnalyticsPeriodInfo`, `RevenueTrendPoint`, `StoreRevenueOverview`, `OrderStatusCountItem`, `StoreOrderStatistics`, `StoreTopFlashDealItem`, `DealStatusItem`, `StoreFlashDealStatistics`, `PaymentMethodBreakdownItem`, `StorePaymentMethodRevenue`, `StoreAnalyticsSnapshot`, `StoreDashboardAnalytics`); `src/shared/types/index.ts` ().
- Hook/state/router: `useAnalyticsPeriod`, `useSearchParams`, `useStoreAnalytics`.
- Child component: `src/features/store-analytics/components/index.ts`, `src/shared/components/analytics/index.ts`, `src/shared/components/modal/ErrorModal.tsx`, `src/shared/components/modal/BaseModal.tsx`.
- Tương tác source: `onChange`, `onClick`, `onClose`, `onFromDateChange`, `onMonthChange`, `onMouseDown`, `onRefresh`, `onSelectPreset`, `onToDateChange`, `onYearChange`.
- Field đọc trong JSX — `src/pages/store-owner/StoreOwnerDashboardPage.tsx`: `analytics.fromDate`, `analytics.month`, `analytics.preset`, `analytics.rangeError`, `analytics.refresh`, `analytics.selectMonth`, `analytics.selectPreset`, `analytics.selectYear`, `analytics.setFromDate`, `analytics.setToDate`, `analytics.toDate`, `analytics.year`, `data.overall`, `data.period`, `data.selected`, `data?.shopName`, `snapshot.flashDealStatistics`, `snapshot.orderStatistics`, `snapshot.revenueByPaymentMethod`, `snapshot.revenueOverview`.
- Nhóm: dashboard. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R54. /store-owner/products

- Page: `src/features/products/pages/ProductListPage.tsx`. Layout: StoreOwnerLayout → ProductsWorkspace. Quyền: storeOwner.
- Service được tham chiếu: `productApi.list`, `productApi.options`, `productApi.summary`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/products/types/product.types.ts` (`Option`, `ProductOptions`, `ProductImage`, `ProductValue`, `ProductAttribute`, `ProductVariant`, `ProductListItem`, `Product`, `Page`, `FlashDeal`, `DraftValue`, `DraftAttribute`, `Combination`, `PriceInput`, `PriceMap`, `ProductListQuery`, `ProductSummary`, `ProductListManagement`, `AttributeValueContent`, `ProductContentManagement`, `RemoveViolatingProduct`, `RevisionContentSnapshot`, `ProductRevisionHistory`); `src/shared/types/index.ts` (); `src/shared/types/moderationAction.types.ts` (`ModerationActionResult`).
- Hook/state/router: `useLocation`, `useOutletContext`, `useProductResource`, `useProductWorkspace`, `useReducer`.
- Child component: `src/shared/components/pagination/index.ts`, `src/features/products/components/ProductUi.tsx`, `src/features/products/components/ProductListSkeleton.tsx`.
- Tương tác source: `onCancel`, `onChange`, `onClick`, `onError`, `onPageChange`, `onSubmit`.
- Field đọc trong JSX — `src/features/products/pages/ProductListPage.tsx`: `c.id`, `c.name`, `data.items`, `data.page`, `data.pageSize`, `data.totalItems`, `data.totalPages`, `data?.items`, `data?.items.length`, `e.target`, `e.target.value`, `product.categoryName`, `product.id`, `product.images`, `product.images[0]?.url`, `product.maxDealPrice`, `product.minDealPrice`, `product.name`, `product.status`, `product.variantCount`, `query.categoryId`, `query.status`, `status.label`, `status.value`, `summary?.activeProducts`, `summary?.hiddenProducts`, `summary?.totalProducts`, `summary?.totalVariants`.
- Field đọc trong JSX — `src/features/products/components/ProductUi.tsx`: `e.currentTarget`, `e.currentTarget.style`, `e.currentTarget.style.display`, `file.name`.
- Field đọc trong JSX — `src/features/products/pages/ProductsWorkspace.tsx`: `shop.id`.
- Nhóm: list/workspace. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R55. /store-owner/products/new

- Page: `src/features/products/pages/CreateProductPage.tsx`. Layout: StoreOwnerLayout → ProductsWorkspace. Quyền: storeOwner.
- Service được tham chiếu: `productApi.create`, `productApi.options`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/products/types/product.types.ts` (`Option`, `ProductOptions`, `ProductImage`, `ProductValue`, `ProductAttribute`, `ProductVariant`, `ProductListItem`, `Product`, `Page`, `FlashDeal`, `DraftValue`, `DraftAttribute`, `Combination`, `PriceInput`, `PriceMap`, `ProductListQuery`, `ProductSummary`, `ProductListManagement`, `AttributeValueContent`, `ProductContentManagement`, `RemoveViolatingProduct`, `RevisionContentSnapshot`, `ProductRevisionHistory`); `src/shared/types/index.ts` (); `src/shared/types/moderationAction.types.ts` (`ModerationActionResult`).
- Hook/state/router: `useLocation`, `useNavigate`, `useOutletContext`, `useProductResource`, `useProductWorkspace`, `useReducer`.
- Child component: `src/features/products/components/ProductUi.tsx`, `src/features/products/components/GalleryEditor.tsx`, `src/features/products/components/AttributesEditor.tsx`, `src/features/products/components/VariantPrices.tsx`, `src/features/products/components/ProductListSkeleton.tsx`.
- Tương tác source: `onCancel`, `onChange`, `onClick`, `onError`, `onFilesChange`, `onSubmit`.
- Field đọc trong JSX — `src/features/products/pages/CreateProductPage.tsx`: `attributes.length`, `c.id`, `c.name`, `categories.length`, `description.length`, `e.target`, `files.length`, `variants.count`, `variants.rows`.
- Field đọc trong JSX — `src/features/products/components/ProductUi.tsx`: `e.currentTarget`, `e.currentTarget.style`, `e.currentTarget.style.display`, `file.name`.
- Field đọc trong JSX — `src/features/products/components/GalleryEditor.tsx`: `LIMITS.images`, `event.target`, `event.target.files`, `event.target.value`, `existing.length`, `file.name`, `files.length`, `image.id`, `image.url`, `incoming.length`, `x.id`.
- Field đọc trong JSX — `src/features/products/components/AttributesEditor.tsx`: `LIMITS.attributes`, `LIMITS.values`, `a.id`, `attribute.id`, `attribute.name`, `attribute.values`, `attribute.values.length`, `attributes.length`, `e.target`, `e.target.files`, `e.target.value`, `existing.find((a) => a.id === attribute.id)?.values`, `saved.length`, `v.id`, `v.value`, `value.id`, `value.image`, `value.value`.
- Field đọc trong JSX — `src/features/products/components/VariantPrices.tsx`: `bulk.dealPrice`, `bulk.originalPrice`, `e.target`, `e.target.value`, `prices[row.key]?.dealPrice`, `prices[row.key]?.originalPrice`, `r.key`, `row.key`, `row.label`, `rows.length`, `rows[0].key`.
- Field đọc trong JSX — `src/features/products/pages/ProductsWorkspace.tsx`: `shop.id`.
- Nhóm: form/settings. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R56. /store-owner/products/:productId

- Page: `src/features/products/pages/ProductDetailPage.tsx`. Layout: StoreOwnerLayout → ProductsWorkspace. Quyền: storeOwner.
- Service được tham chiếu: `productApi.addValues`, `productApi.deals`, `productApi.get`, `productApi.images`, `productApi.options`, `productApi.remove`, `productApi.update`, `productApi.updateStatus`, `productApi.updateVariant`, `productApi.valueImage`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/products/types/product.types.ts` (`Option`, `ProductOptions`, `ProductImage`, `ProductValue`, `ProductAttribute`, `ProductVariant`, `ProductListItem`, `Product`, `Page`, `FlashDeal`, `DraftValue`, `DraftAttribute`, `Combination`, `PriceInput`, `PriceMap`, `ProductListQuery`, `ProductSummary`, `ProductListManagement`, `AttributeValueContent`, `ProductContentManagement`, `RemoveViolatingProduct`, `RevisionContentSnapshot`, `ProductRevisionHistory`); `src/shared/types/index.ts` (); `src/shared/types/moderationAction.types.ts` (`ModerationActionResult`).
- Hook/state/router: `useLocation`, `useNavigate`, `useOutletContext`, `useParams`, `useProductResource`, `useProductWorkspace`, `useReducer`.
- Child component: `src/features/products/components/ProductUi.tsx`, `src/features/products/components/ProductInfoEditor.tsx`, `src/features/products/components/ProductImagesPanel.tsx`, `src/features/products/components/GalleryEditor.tsx`, `src/features/products/components/ProductVariantsPanel.tsx`, `src/features/products/components/AttributesEditor.tsx`, `src/features/products/components/VariantPrices.tsx`, `src/features/products/components/ProductDealsPanel.tsx`, `src/shared/components/pagination/Pagination.tsx`, `src/features/products/components/ProductListSkeleton.tsx`.
- Tương tác source: `onCancel`, `onChange`, `onClick`, `onClose`, `onError`, `onExistingChange`, `onFilesChange`, `onKeyDown`, `onPageChange`, `onSaved`, `onSubmit`.
- Field đọc trong JSX — `src/features/products/pages/ProductDetailPage.tsx`: `e.key`, `item.icon`, `item.id`, `item.label`, `product.activeWarning`, `product.activeWarning.dealBlocked`, `product.activeWarning.dueAt`, `product.activeWarning.firstEditedAt`, `product.activeWarning.requiredCorrection`, `product.categoryName`, `product.id`, `product.images`, `product.images[0]?.url`, `product.latestReview`, `product.latestReview.note`, `product.latestReview.status`, `product.name`, `product.status`, `product.variants`, `product.variants.length`, `tabs.length`, `tabs[next].id`, `workspace.shop`, `workspace.shop.id`.
- Field đọc trong JSX — `src/features/products/components/ProductUi.tsx`: `e.currentTarget`, `e.currentTarget.style`, `e.currentTarget.style.display`, `file.name`.
- Field đọc trong JSX — `src/features/products/components/ProductInfoEditor.tsx`: `c.id`, `c.name`, `categories.length`, `e.target`, `product.categoryId`, `product.categoryName`.
- Field đọc trong JSX — `src/features/products/components/ProductImagesPanel.tsx`: `attribute.id`, `attribute.name`, `attribute.values`, `e.target`, `e.target.files`, `e.target.value`, `existing.length`, `files.length`, `i.source`, `image.attributeName`, `image.id`, `image.source`, `image.url`, `image.value`, `product.attributes`, `product.id`, `product.images`, `product.images.length`, `product.name`, `saved.images`, `value.id`, `value.imageUrl`, `value.value`.
- Field đọc trong JSX — `src/features/products/components/GalleryEditor.tsx`: `LIMITS.images`, `event.target`, `event.target.files`, `event.target.value`, `existing.length`, `file.name`, `files.length`, `image.id`, `image.url`, `incoming.length`, `x.id`.
- Field đọc trong JSX — `src/features/products/components/ProductVariantsPanel.tsx`: `a.id`, `a.name`, `a.values`, `e.target`, `generated.count`, `product.attributes`, `product.attributes.length`, `product.variants`, `product.variants.length`, `rows.length`, `v.id`, `v.image`, `v.isActive`, `v.sku`, `v.value`, `v.valueIds`, `variant.sku`.
- Field đọc trong JSX — `src/features/products/components/AttributesEditor.tsx`: `LIMITS.attributes`, `LIMITS.values`, `a.id`, `attribute.id`, `attribute.name`, `attribute.values`, `attribute.values.length`, `attributes.length`, `e.target`, `e.target.files`, `e.target.value`, `existing.find((a) => a.id === attribute.id)?.values`, `saved.length`, `v.id`, `v.value`, `value.id`, `value.image`, `value.value`.
- Field đọc trong JSX — `src/features/products/components/VariantPrices.tsx`: `bulk.dealPrice`, `bulk.originalPrice`, `e.target`, `e.target.value`, `prices[row.key]?.dealPrice`, `prices[row.key]?.originalPrice`, `r.key`, `row.key`, `row.label`, `rows.length`, `rows[0].key`.
- Field đọc trong JSX — `src/features/products/components/ProductDealsPanel.tsx`: `data.items`, `data.items.length`, `data.page`, `data.pageSize`, `data.totalItems`, `data.totalPages`, `deal.id`, `deal.status`, `deal.variants`, `e.target`, `v.availableQuantity`, `v.discountPercent`, `v.id`, `v.reservedQuantity`, `v.soldQuantity`, `v.status`, `v.totalQuantity`, `v.values`, `value.value`.
- Field đọc trong JSX — `src/shared/components/pagination/Pagination.tsx`: `sizeClasses.btn`, `sizeClasses.gap`, `sizeClasses.iconSize`, `sizeClasses.navBtn`.
- Field đọc trong JSX — `src/features/products/pages/ProductsWorkspace.tsx`: `shop.id`.
- Nhóm: detail/tabs. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R57. /store-owner/products/:productId/flash-deals

- Page: `src/features/products/pages/ProductHistoryPage.tsx`. Layout: StoreOwnerLayout → ProductsWorkspace. Quyền: storeOwner.
- Service được tham chiếu: `productApi.deals`, `productApi.options`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/products/types/product.types.ts` (`Option`, `ProductOptions`, `ProductImage`, `ProductValue`, `ProductAttribute`, `ProductVariant`, `ProductListItem`, `Product`, `Page`, `FlashDeal`, `DraftValue`, `DraftAttribute`, `Combination`, `PriceInput`, `PriceMap`, `ProductListQuery`, `ProductSummary`, `ProductListManagement`, `AttributeValueContent`, `ProductContentManagement`, `RemoveViolatingProduct`, `RevisionContentSnapshot`, `ProductRevisionHistory`); `src/shared/types/index.ts` (); `src/shared/types/moderationAction.types.ts` (`ModerationActionResult`).
- Hook/state/router: `useLocation`, `useOutletContext`, `useParams`, `useProductResource`, `useProductWorkspace`, `useReducer`.
- Child component: `src/features/products/components/ProductDealsPanel.tsx`, `src/shared/components/pagination/Pagination.tsx`, `src/features/products/components/ProductUi.tsx`, `src/features/products/components/ProductListSkeleton.tsx`.
- Tương tác source: `onCancel`, `onChange`, `onClick`, `onError`, `onPageChange`.
- Field đọc trong JSX — `src/features/products/pages/ProductHistoryPage.tsx`: `location.state`, `location.state.message`, `location.state.productName`, `location.state?.message`, `location.state?.productName`.
- Field đọc trong JSX — `src/features/products/components/ProductDealsPanel.tsx`: `data.items`, `data.items.length`, `data.page`, `data.pageSize`, `data.totalItems`, `data.totalPages`, `deal.id`, `deal.status`, `deal.variants`, `e.target`, `v.availableQuantity`, `v.discountPercent`, `v.id`, `v.reservedQuantity`, `v.soldQuantity`, `v.status`, `v.totalQuantity`, `v.values`, `value.value`.
- Field đọc trong JSX — `src/shared/components/pagination/Pagination.tsx`: `sizeClasses.btn`, `sizeClasses.gap`, `sizeClasses.iconSize`, `sizeClasses.navBtn`.
- Field đọc trong JSX — `src/features/products/components/ProductUi.tsx`: `e.currentTarget`, `e.currentTarget.style`, `e.currentTarget.style.display`, `file.name`.
- Field đọc trong JSX — `src/features/products/pages/ProductsWorkspace.tsx`: `shop.id`.
- Nhóm: detail/tabs. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R58. /store-owner/orders

- Page: `src/pages/shop/ShopOrdersPage.tsx`. Layout: StoreOwnerLayout. Quyền: storeOwner.
- Service được tham chiếu: `shopOrderApi.page`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/checkout/types/checkout.types.ts` (`CheckoutMethod`, `DepositRange`, `CheckoutPolicy`, `CheckoutContext`, `PickupEstimate`, `CheckoutRoute`, `CheckoutQuote`, `CheckoutGps`, `CheckoutOrder`, `OrderHistoryPage`); `src/features/shop-orders/types/shopOrder.types.ts` (`OrderRefund`, `ShopOrder`, `ShopOrderPage`); `src/shared/types/index.ts` ().
- Hook/state/router: `useAuthStore`, `useParams`, `useShopOrderEventsStore`.
- Child component: `src/shared/components/pagination/index.ts`, `src/features/shop-orders/components/ConfirmationCountdown.tsx`, `src/features/checkout/components/OrderStatusPanel.tsx`, `src/features/checkout/components/OrderPickupDeadlines.tsx`.
- Tương tác source: `onChange`, `onClick`, `onPageChange`, `onSubmit`.
- Field đọc trong JSX — `src/pages/shop/ShopOrdersPage.tsx`: `Intl.DateTimeFormat`, `data.page`, `data.page.page`, `data.page.pageSize`, `data.page.totalItems`, `data.page.totalPages`, `data?.page`, `data?.page.items`, `data?.page.items.length`, `data?.statusCounts`, `e.target`, `e.target.value`, `generation.current`, `item.flashDealVariantId`, `item.imageUrl`, `item.productName`, `item.quantity`, `item.variantName`, `row.confirmationDeadlineAt`, `row.customerName`, `row.customerPhone`, `row.order`, `row.order.id`, `row.order.items`, `row.order.items.length`, `row.order.orderCode`, `row.order.outstandingAmount`, `row.order.paidAmount`, `row.order.totalAmount`.
- Field đọc trong JSX — `src/features/shop-orders/components/ConfirmationCountdown.tsx`: `remaining.label`, `remaining.urgent`.
- Field đọc trong JSX — `src/features/checkout/components/OrderStatusPanel.tsx`: `order.paymentMethod`, `order.paymentReviewRequired`, `order.remainingAtShop`, `order.status`, `payment.color`, `payment.label`, `state.color`, `state.description`, `state.label`.
- Field đọc trong JSX — `src/features/checkout/components/OrderPickupDeadlines.tsx`: `order.hardDeadlineAt`, `order.softDeadlineAt`.
- Nhóm: list/workspace. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R59. /store-owner/orders/:orderId

- Page: `src/pages/shop/ShopOrderDetailPage.tsx`. Layout: StoreOwnerLayout. Quyền: storeOwner.
- Service được tham chiếu: `refundApi.qr`, `refundApi.review`, `refundApi.transfer`, `refundApi.upload`, `shopOrderApi.action`, `shopOrderApi.detail`, `shopOrderApi.simulateRemaining`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/checkout/types/checkout.types.ts` (`CheckoutMethod`, `DepositRange`, `CheckoutPolicy`, `CheckoutContext`, `PickupEstimate`, `CheckoutRoute`, `CheckoutQuote`, `CheckoutGps`, `CheckoutOrder`, `OrderHistoryPage`); `src/features/refunds/types/refund.types.ts` (`Refund`, `RefundBank`, `RefundPage`, `RefundImage`, `RefundQr`); `src/features/shop-orders/types/shopOrder.types.ts` (`OrderRefund`, `ShopOrder`, `ShopOrderPage`); `src/shared/types/index.ts` ().
- Hook/state/router: `useAuthStore`, `useAuthStore.getState`, `useParams`, `useShopOrderEventsStore`.
- Child component: `src/features/refunds/components/ShopOrderRefunds.tsx`, `src/features/refunds/components/ShopRefundCard.tsx`, `src/features/refunds/components/RefundItems.tsx`, `src/features/refunds/components/RefundEvidenceGallery.tsx`, `src/shared/components/modal/BaseModal.tsx`, `src/features/refunds/components/RefundCustomerInfo.tsx`, `src/features/refunds/components/RefundFileUpload.tsx`, `src/features/refunds/components/RefundStatusBadge.tsx`, `src/features/shop-orders/components/ConfirmationCountdown.tsx`, `src/features/shop-orders/components/HistoryActor.tsx`, `src/features/checkout/components/OrderStatusPanel.tsx`, `src/features/checkout/components/OrderPickupDeadlines.tsx`, `src/features/shop-orders/components/PaymentQr.tsx`.
- Tương tác source: `onChange`, `onChanged`, `onClick`, `onClose`, `onError`, `onMouseDown`, `onSubmit`.
- Field đọc trong JSX — `src/pages/shop/ShopOrderDetailPage.tsx`: `data.confirmationDeadlineAt`, `data.customerName`, `data.customerPhone`, `data.history`, `data.isOwner`, `data.payments`, `e.target`, `event.actorId`, `event.actorName`, `event.actorRole`, `event.note`, `event.toStatus`, `item.flashDealVariantId`, `item.imageUrl`, `item.productName`, `item.quantity`, `item.variantName`, `o.items`, `o.orderCode`, `o.outstandingAmount`, `o.paidAmount`, `o.paymentMethod`, `o.paymentReviewRequired`, `o.remainingAtShop`, `o.status`, `o.totalAmount`, `p.id`, `p.paidAt`, `p.status`, `p.type`, `remaining.checkoutUrl`, `remaining.id`, `remaining.qrCode`, `remaining?.checkoutUrl`, `remaining?.collectedAmount`, `remaining?.status`.
- Field đọc trong JSX — `src/features/refunds/components/ShopOrderRefunds.tsx`: `changed.id`, `item.id`, `refund.amount`, `refund.id`, `refund.status`.
- Field đọc trong JSX — `src/features/refunds/components/ShopRefundCard.tsx`: `currentQr.imageUrl`, `currentQr.transferContent`, `entry.actorName`, `entry.actorRole`, `entry.decision`, `entry.id`, `entry.note`, `event.target`, `item.evidenceUrls`, `item.evidenceUrls?.length`, `proof.length`, `refund.accountHolder`, `refund.accountNumber`, `refund.amount`, `refund.bankName`, `refund.customerDetailsDeadlineAt`, `refund.destinationConfirmedAt`, `refund.evidenceUrls`, `refund.history`, `refund.history.length`, `refund.items`, `refund.reason`, `refund.rejectReason`, `refund.reviewDeadlineAt`, `refund.status`, `refund.transferProofUrls`, `refund.transferProofUrls.length`, `refund.transferReference`, `refund.transferredAmount`.
- Field đọc trong JSX — `src/features/refunds/components/RefundItems.tsx`: `item.evidenceUrls`, `item.orderItemId`, `item.productName`, `item.purchasedQuantity`, `item.quantity`, `item.reason`, `item.variantName`, `refund.items`.
- Field đọc trong JSX — `src/features/refunds/components/RefundCustomerInfo.tsx`: `refund.customerAvatarUrl`, `refund.customerLevelName`, `refund.customerLevelRank`, `refund.customerName`, `refund.customerPhone`.
- Field đọc trong JSX — `src/features/refunds/components/RefundFileUpload.tsx`: `event.target`, `event.target.files`, `event.target.value`, `file.name`, `file.size`, `files.length`, `ref.current`, `selected.length`.
- Field đọc trong JSX — `src/features/refunds/components/RefundStatusBadge.tsx`: `colors.Withdrawn`.
- Field đọc trong JSX — `src/features/shop-orders/components/ConfirmationCountdown.tsx`: `remaining.label`, `remaining.urgent`.
- Field đọc trong JSX — `src/features/shop-orders/components/HistoryActor.tsx`: `actor.name`, `actor.role`.
- Field đọc trong JSX — `src/features/checkout/components/OrderStatusPanel.tsx`: `order.paymentMethod`, `order.paymentReviewRequired`, `order.remainingAtShop`, `order.status`, `payment.color`, `payment.label`, `state.color`, `state.description`, `state.label`.
- Field đọc trong JSX — `src/features/checkout/components/OrderPickupDeadlines.tsx`: `order.hardDeadlineAt`, `order.softDeadlineAt`.
- Nhóm: detail/tabs. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R60. /store-owner/refunds

- Page: `src/pages/store-owner/RefundsPage.tsx`. Layout: StoreOwnerLayout. Quyền: storeOwner.
- Service được tham chiếu: Không có service HTTP được phát hiện. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/checkout/types/checkout.types.ts` (`CheckoutMethod`, `DepositRange`, `CheckoutPolicy`, `CheckoutContext`, `PickupEstimate`, `CheckoutRoute`, `CheckoutQuote`, `CheckoutGps`, `CheckoutOrder`, `OrderHistoryPage`); `src/features/refunds/types/refund.types.ts` (`Refund`, `RefundBank`, `RefundPage`, `RefundImage`, `RefundQr`); `src/shared/types/index.ts` ().
- Hook/state/router: `useRealtimeSubscriptions`, `useSearchParams`.
- Child component: `src/features/refunds/components/RefundCustomerInfo.tsx`, `src/features/refunds/components/RefundStatusBadge.tsx`, `src/shared/components/pagination/index.ts`.
- Tương tác source: `onChange`, `onClick`, `onError`, `onPageChange`; realtime `SHOP_ORDERS_CHANGED`.
- Field đọc trong JSX — `src/pages/store-owner/RefundsPage.tsx`: `data.page`, `data.pageSize`, `data.totalItems`, `data.totalPages`, `data?.items`, `data?.items.length`, `event.target`, `event.target.value`, `refund.id`, `refund.isOverdue`, `refund.orderCode`, `refund.reason`, `refund.requiresShopAction`, `refund.reviewDeadlineAt`, `refund.shopAction`, `refund.status`, `refund.type`.
- Field đọc trong JSX — `src/features/refunds/components/RefundCustomerInfo.tsx`: `refund.customerAvatarUrl`, `refund.customerLevelName`, `refund.customerLevelRank`, `refund.customerName`, `refund.customerPhone`.
- Field đọc trong JSX — `src/features/refunds/components/RefundStatusBadge.tsx`: `colors.Withdrawn`.
- Nhóm: list/workspace. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R61. /store-owner/refunds/:refundId

- Page: `src/pages/store-owner/RefundDetailPage.tsx`. Layout: StoreOwnerLayout. Quyền: storeOwner.
- Service được tham chiếu: `refundApi.qr`, `refundApi.review`, `refundApi.transfer`, `refundApi.upload`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/checkout/types/checkout.types.ts` (`CheckoutMethod`, `DepositRange`, `CheckoutPolicy`, `CheckoutContext`, `PickupEstimate`, `CheckoutRoute`, `CheckoutQuote`, `CheckoutGps`, `CheckoutOrder`, `OrderHistoryPage`); `src/features/refunds/types/refund.types.ts` (`Refund`, `RefundBank`, `RefundPage`, `RefundImage`, `RefundQr`); `src/shared/types/index.ts` ().
- Hook/state/router: `useAuthStore.getState`, `useParams`, `useRealtimeSubscriptions`.
- Child component: `src/features/refunds/components/ShopRefundCard.tsx`, `src/features/refunds/components/RefundItems.tsx`, `src/features/refunds/components/RefundEvidenceGallery.tsx`, `src/shared/components/modal/BaseModal.tsx`, `src/features/refunds/components/RefundCustomerInfo.tsx`, `src/features/refunds/components/RefundFileUpload.tsx`, `src/features/refunds/components/RefundStatusBadge.tsx`.
- Tương tác source: `onChange`, `onChanged`, `onClick`, `onClose`, `onError`, `onMouseDown`, `onSubmit`; realtime `SHOP_ORDERS_CHANGED`.
- Field đọc trong JSX — `src/pages/store-owner/RefundDetailPage.tsx`: `refund.amount`, `refund.customerName`, `refund.id`, `refund.orderCode`, `refund.orderId`, `refund.status`.
- Field đọc trong JSX — `src/features/refunds/components/ShopRefundCard.tsx`: `currentQr.imageUrl`, `currentQr.transferContent`, `entry.actorName`, `entry.actorRole`, `entry.decision`, `entry.id`, `entry.note`, `event.target`, `item.evidenceUrls`, `item.evidenceUrls?.length`, `proof.length`, `refund.accountHolder`, `refund.accountNumber`, `refund.amount`, `refund.bankName`, `refund.customerDetailsDeadlineAt`, `refund.destinationConfirmedAt`, `refund.evidenceUrls`, `refund.history`, `refund.history.length`, `refund.items`, `refund.reason`, `refund.rejectReason`, `refund.reviewDeadlineAt`, `refund.status`, `refund.transferProofUrls`, `refund.transferProofUrls.length`, `refund.transferReference`, `refund.transferredAmount`.
- Field đọc trong JSX — `src/features/refunds/components/RefundItems.tsx`: `item.evidenceUrls`, `item.orderItemId`, `item.productName`, `item.purchasedQuantity`, `item.quantity`, `item.reason`, `item.variantName`, `refund.items`.
- Field đọc trong JSX — `src/features/refunds/components/RefundCustomerInfo.tsx`: `refund.customerAvatarUrl`, `refund.customerLevelName`, `refund.customerLevelRank`, `refund.customerName`, `refund.customerPhone`.
- Field đọc trong JSX — `src/features/refunds/components/RefundFileUpload.tsx`: `event.target`, `event.target.files`, `event.target.value`, `file.name`, `file.size`, `files.length`, `ref.current`, `selected.length`.
- Field đọc trong JSX — `src/features/refunds/components/RefundStatusBadge.tsx`: `colors.Withdrawn`.
- Nhóm: detail/tabs. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R62. /store-owner/flash-deals

- Page: `src/pages/store-owner/StoreOwnerFlashDealsPage.tsx`. Layout: StoreOwnerLayout. Quyền: storeOwner.
- Service được tham chiếu: `flashDealApi.cancelFlashDeal`, `flashDealApi.getShopFlashDeals`, `flashDealApi.hideFlashDeal`, `flashDealApi.unhideFlashDeal`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/flash-deals/types/flashDeal.types.ts` (`DealAttributeGroup`, `FlashDealVariant`, `FlashDeal`, `CursorMode`, `FlashDealCursorParams`, `FlashDealCursorResult`, `DealFilterState`, `FlashDealStatusEnum`, `FlashDealVariantStatusEnum`, `ShopFlashDealListItem`, `ShopFlashDealVariantDetail`, `ShopFlashDealDetail`, `ShopEligibleProductVariant`, `ShopEligibleProduct`, `CreateFlashDealVariantPayload`, `CreateFlashDealPayload`, `UpdateFlashDealVariantPayload`, `UpdateFlashDealPayload`, `ShopFlashDealQueryParams`); `src/shared/types/index.ts` ().
- Hook/state/router: `useNavigate`, `useToastStore.getState`, `useToastStore.getState().removeToast`.
- Child component: `src/features/flash-deals/components/CancelFlashDealModal.tsx`, `src/features/flash-deals/components/ToggleVisibilityModal.tsx`.
- Tương tác source: `onChange`, `onClick`, `onClose`, `onConfirm`.
- Field đọc trong JSX — `src/pages/store-owner/StoreOwnerFlashDealsPage.tsx`: `FlashDealStatusEnum.Hidden`, `deal.availableQuantity`, `deal.categoryName`, `deal.id`, `deal.maxDiscountPercent`, `deal.minDealPrice`, `deal.productImageUrl`, `deal.productName`, `deal.reservedQuantity`, `deal.soldQuantity`, `deal.status`, `deal.totalQuantity`, `deals.length`, `e.target`, `filteredDeals.length`, `stats.ended`, `stats.hidden`, `stats.pending`, `stats.remaining`, `stats.running`, `stats.sold`, `stats.total`, `stats.totalDeals`, `tabCounts.all`, `tabCounts.ended`, `tabCounts.hidden`, `tabCounts.pending`, `tabCounts.running`.
- Field đọc trong JSX — `src/features/flash-deals/components/CancelFlashDealModal.tsx`: `deal.categoryName`, `deal.minDealPrice`, `deal.productName`.
- Field đọc trong JSX — `src/features/flash-deals/components/ToggleVisibilityModal.tsx`: `deal.categoryName`, `deal.minDealPrice`, `deal.productName`.
- Nhóm: list/workspace. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R63. /store-owner/flash-deals/create

- Page: `src/pages/store-owner/StoreOwnerFlashDealFormPage.tsx`. Layout: StoreOwnerLayout. Quyền: storeOwner.
- Service được tham chiếu: `flashDealApi.createFlashDeal`, `flashDealApi.getEligibleProducts`, `flashDealApi.getShopFlashDealDetail`, `flashDealApi.updateFlashDeal`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/flash-deals/types/flashDeal.types.ts` (`DealAttributeGroup`, `FlashDealVariant`, `FlashDeal`, `CursorMode`, `FlashDealCursorParams`, `FlashDealCursorResult`, `DealFilterState`, `FlashDealStatusEnum`, `FlashDealVariantStatusEnum`, `ShopFlashDealListItem`, `ShopFlashDealVariantDetail`, `ShopFlashDealDetail`, `ShopEligibleProductVariant`, `ShopEligibleProduct`, `CreateFlashDealVariantPayload`, `CreateFlashDealPayload`, `UpdateFlashDealVariantPayload`, `UpdateFlashDealPayload`, `ShopFlashDealQueryParams`); `src/shared/types/index.ts` ().
- Hook/state/router: `useNavigate`, `useParams`, `useToastStore.getState`, `useToastStore.getState().removeToast`.
- Child component: Inline trong page.
- Tương tác source: `onChange`, `onClick`, `onSubmit`.
- Field đọc trong JSX — `src/pages/store-owner/StoreOwnerFlashDealFormPage.tsx`: `FlashDealVariantStatusEnum.Active`, `FlashDealVariantStatusEnum.Hide`, `currentProduct.categoryName`, `currentProduct.description`, `currentProduct.imageUrl`, `currentProduct.name`, `currentProduct.variants`, `currentProduct.variants.length`, `currentProduct?.name`, `e.target`, `eligibleProducts.length`, `p.categoryName`, `p.id`, `p.imageUrl`, `p.name`, `p.variants`, `p.variants.length`, `scheduleSummary.durationMins`, `scheduleSummary.hours`, `scheduleSummary.isCurrentlyActive`, `scheduleSummary.isNextDay`, `scheduleSummary.isUpcoming`, `scheduleSummary.minutes`, `scheduleSummary?.errorMessage`, `scheduleSummary?.isNextDay`, `scheduleSummary?.isValid`, `v.dealPrice`, `v.isSelected`, `v.originalPrice`, `v.reservedQuantity`, `v.sku`, `v.soldQuantity`, `v.status`, `v.totalQuantity`, `v.variantId`, `v.variantName`, `variants.length`.
- Nhóm: form/settings. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R64. /store-owner/flash-deals/:id

- Page: `src/pages/store-owner/StoreOwnerFlashDealDetailPage.tsx`. Layout: StoreOwnerLayout. Quyền: storeOwner.
- Service được tham chiếu: `flashDealApi.cancelFlashDeal`, `flashDealApi.getShopFlashDealDetail`, `flashDealApi.hideFlashDeal`, `flashDealApi.unhideFlashDeal`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/flash-deals/types/flashDeal.types.ts` (`DealAttributeGroup`, `FlashDealVariant`, `FlashDeal`, `CursorMode`, `FlashDealCursorParams`, `FlashDealCursorResult`, `DealFilterState`, `FlashDealStatusEnum`, `FlashDealVariantStatusEnum`, `ShopFlashDealListItem`, `ShopFlashDealVariantDetail`, `ShopFlashDealDetail`, `ShopEligibleProductVariant`, `ShopEligibleProduct`, `CreateFlashDealVariantPayload`, `CreateFlashDealPayload`, `UpdateFlashDealVariantPayload`, `UpdateFlashDealPayload`, `ShopFlashDealQueryParams`); `src/shared/types/index.ts` ().
- Hook/state/router: `useNavigate`, `useParams`, `useToastStore.getState`, `useToastStore.getState().removeToast`.
- Child component: `src/features/flash-deals/components/CancelFlashDealModal.tsx`, `src/features/flash-deals/components/ToggleVisibilityModal.tsx`.
- Tương tác source: `onClick`, `onClose`, `onConfirm`.
- Field đọc trong JSX — `src/pages/store-owner/StoreOwnerFlashDealDetailPage.tsx`: `FlashDealVariantStatusEnum.Hide`, `deal.categoryName`, `deal.description`, `deal.id`, `deal.primaryImageUrl`, `deal.productName`, `deal.status`, `deal.variants`, `deal.variants.length`, `summary.percent`, `summary.remainingQty`, `summary.reservedQty`, `summary.revenue`, `summary.soldQty`, `summary.totalQty`, `v.availableQuantity`, `v.dealPrice`, `v.discountPercent`, `v.id`, `v.originalPrice`, `v.reservedQuantity`, `v.sku`, `v.soldQuantity`, `v.status`, `v.totalQuantity`, `v.variantName`.
- Field đọc trong JSX — `src/features/flash-deals/components/CancelFlashDealModal.tsx`: `deal.categoryName`, `deal.minDealPrice`, `deal.productName`.
- Field đọc trong JSX — `src/features/flash-deals/components/ToggleVisibilityModal.tsx`: `deal.categoryName`, `deal.minDealPrice`, `deal.productName`.
- Nhóm: detail/tabs. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=false, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R65. /store-owner/flash-deals/:id/edit

- Page: `src/pages/store-owner/StoreOwnerFlashDealFormPage.tsx`. Layout: StoreOwnerLayout. Quyền: storeOwner.
- Service được tham chiếu: `flashDealApi.createFlashDeal`, `flashDealApi.getEligibleProducts`, `flashDealApi.getShopFlashDealDetail`, `flashDealApi.updateFlashDeal`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/flash-deals/types/flashDeal.types.ts` (`DealAttributeGroup`, `FlashDealVariant`, `FlashDeal`, `CursorMode`, `FlashDealCursorParams`, `FlashDealCursorResult`, `DealFilterState`, `FlashDealStatusEnum`, `FlashDealVariantStatusEnum`, `ShopFlashDealListItem`, `ShopFlashDealVariantDetail`, `ShopFlashDealDetail`, `ShopEligibleProductVariant`, `ShopEligibleProduct`, `CreateFlashDealVariantPayload`, `CreateFlashDealPayload`, `UpdateFlashDealVariantPayload`, `UpdateFlashDealPayload`, `ShopFlashDealQueryParams`); `src/shared/types/index.ts` ().
- Hook/state/router: `useNavigate`, `useParams`, `useToastStore.getState`, `useToastStore.getState().removeToast`.
- Child component: Inline trong page.
- Tương tác source: `onChange`, `onClick`, `onSubmit`.
- Field đọc trong JSX — `src/pages/store-owner/StoreOwnerFlashDealFormPage.tsx`: `FlashDealVariantStatusEnum.Active`, `FlashDealVariantStatusEnum.Hide`, `currentProduct.categoryName`, `currentProduct.description`, `currentProduct.imageUrl`, `currentProduct.name`, `currentProduct.variants`, `currentProduct.variants.length`, `currentProduct?.name`, `e.target`, `eligibleProducts.length`, `p.categoryName`, `p.id`, `p.imageUrl`, `p.name`, `p.variants`, `p.variants.length`, `scheduleSummary.durationMins`, `scheduleSummary.hours`, `scheduleSummary.isCurrentlyActive`, `scheduleSummary.isNextDay`, `scheduleSummary.isUpcoming`, `scheduleSummary.minutes`, `scheduleSummary?.errorMessage`, `scheduleSummary?.isNextDay`, `scheduleSummary?.isValid`, `v.dealPrice`, `v.isSelected`, `v.originalPrice`, `v.reservedQuantity`, `v.sku`, `v.soldQuantity`, `v.status`, `v.totalQuantity`, `v.variantId`, `v.variantName`, `variants.length`.
- Nhóm: form/settings. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R66. /store-owner/staff

- Page: `src/pages/store-owner/StaffManagementPage.tsx`. Layout: StoreOwnerLayout. Quyền: storeOwner.
- Service được tham chiếu: `shopStaffApi.getStaffList`, `shopStaffApi.inviteStaff`, `shopStaffApi.removeStaff`, `shopStaffApi.searchStaffCandidates`, `shopStaffApi.updateStaff`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/shop-staffs/types/shopStaff.types.ts` (`ShopStaffStatus`, `ShopStaff`, `StaffActivityLog`, `UpdateStaffInfoInput`, `InviteStaffInput`, `StaffCandidate`, `StaffInvitation`); `src/shared/types/index.ts` ().
- Hook/state/router: `useNavigate`, `usePagination`, `useToastStore.getState`, `useToastStore.getState().removeToast`.
- Child component: `src/features/shop-staffs/components/InviteStaffModal.tsx`, `src/features/shop-staffs/components/StaffTable.tsx`, `src/features/shop-staffs/components/StaffStatusBadge.tsx`, `src/features/shop-staffs/components/StaffStatCard.tsx`, `src/features/shop-staffs/components/EditStaffModal.tsx`, `src/features/shop-staffs/components/RemoveStaffModal.tsx`, `src/shared/components/pagination/index.ts`.
- Tương tác source: `onChange`, `onClick`, `onClose`, `onConfirm`, `onEdit`, `onPageChange`, `onRemove`, `onSubmit`, `onView`.
- Field đọc trong JSX — `src/pages/store-owner/StaffManagementPage.tsx`: `event.target`, `staff.id`, `staffs.length`.
- Field đọc trong JSX — `src/features/shop-staffs/components/InviteStaffModal.tsx`: `candidates.length`, `event.target`, `selectedUser?.userId`, `user.email`, `user.fullName`, `user.userId`.
- Field đọc trong JSX — `src/features/shop-staffs/components/StaffTable.tsx`: `staff.displayName`, `staff.id`, `staff.staffNickname`, `staff.status`, `staff.userEmail`.
- Field đọc trong JSX — `src/features/shop-staffs/components/EditStaffModal.tsx`: `event.target`, `event.target.value`, `staff.displayName`, `staff.userEmail`.
- Field đọc trong JSX — `src/features/shop-staffs/components/RemoveStaffModal.tsx`: `staff.displayName`, `staff.staffNickname`, `staff.userEmail`.
- Nhóm: list/workspace. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R67. /store-owner/staff/invitations

- Page: `src/pages/store-owner/StaffInvitationsPage.tsx`. Layout: StoreOwnerLayout. Quyền: storeOwner.
- Service được tham chiếu: `shopStaffApi.getInvitations`, `shopStaffApi.revokeInvitation`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/shop-staffs/types/shopStaff.types.ts` (`ShopStaffStatus`, `ShopStaff`, `StaffActivityLog`, `UpdateStaffInfoInput`, `InviteStaffInput`, `StaffCandidate`, `StaffInvitation`); `src/shared/types/index.ts` ().
- Hook/state/router: `useNavigate`, `usePagination`, `useToastStore.getState`, `useToastStore.getState().removeToast`.
- Child component: `src/shared/components/pagination/index.ts`.
- Tương tác source: `onClick`, `onClose`, `onConfirm`, `onPageChange`.
- Field đọc trong JSX — `src/pages/store-owner/StaffInvitationsPage.tsx`: `invitation.id`, `invitation.invitedUserEmail`, `invitation.invitedUserName`, `invitation.status`, `invitations.length`.
- Nhóm: list/workspace. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R68. /store-owner/staff/:staffId

- Page: `src/pages/store-owner/StaffDetailPage.tsx`. Layout: StoreOwnerLayout. Quyền: storeOwner.
- Service được tham chiếu: `shopStaffApi.getStaffActivityLogs`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/shop-staffs/types/shopStaff.types.ts` (`ShopStaffStatus`, `ShopStaff`, `StaffActivityLog`, `UpdateStaffInfoInput`, `InviteStaffInput`, `StaffCandidate`, `StaffInvitation`); `src/shared/types/index.ts` ().
- Hook/state/router: `useLocation`, `useNavigate`, `usePagination`, `useParams`, `useToastStore.getState`, `useToastStore.getState().removeToast`.
- Child component: `src/features/shop-staffs/components/StaffStatusBadge.tsx`, `src/features/shop-staffs/components/StaffActivityLog.tsx`, `src/shared/components/pagination/index.ts`.
- Tương tác source: `onClick`, `onPageChange`.
- Field đọc trong JSX — `src/pages/store-owner/StaffDetailPage.tsx`: `staff.displayName`, `staff.joinedAt`, `staff.staffNickname`, `staff.status`, `staff.userEmail`.
- Field đọc trong JSX — `src/features/shop-staffs/components/StaffActivityLog.tsx`: `log.action`, `log.actorName`, `log.actorRole`, `log.description`, `log.id`, `log.orderId`, `logs.length`.
- Nhóm: detail/tabs. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R69. /store-owner/payment-settings

- Page: `src/pages/store-owner/PaymentSettingsPage.tsx`. Layout: StoreOwnerLayout. Quyền: storeOwner.
- Service được tham chiếu: `ownerShopApi.getMyPaymentConfig`, `ownerShopApi.updateMyPaymentConfig`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/shops/types/ownerShop.types.ts` (`ShopStatus`, `ShopProfileDocument`, `OwnerShop`, `UpdateBusinessInfo`, `ShopPaymentConfig`, `UpdateShopPaymentConfigRequest`); `src/shared/types/index.ts` ().
- Hook/state/router: `useToastStore.getState`, `useToastStore.getState().removeToast`.
- Child component: `src/features/shops/components/BankAccountSection.tsx`, `src/features/shops/components/PayOSSection.tsx`.
- Tương tác source: `onBlur`, `onChange`, `onClick`, `onSave`, `onToggleVisibility`.
- Field đọc trong JSX — `src/pages/store-owner/PaymentSettingsPage.tsx`: `config.bankAccountHolder`, `config.bankAccountNumber`, `config.bankName`, `config.hasPayosApiKey`, `config.hasPayosChecksumKey`, `config.payosClientId`.
- Field đọc trong JSX — `src/features/shops/components/BankAccountSection.tsx`: `errors.bankAccountHolder`, `errors.bankAccountNumber`, `errors.bankName`, `event.target`, `values.bankAccountHolder`, `values.bankAccountNumber`, `values.bankName`.
- Field đọc trong JSX — `src/features/shops/components/PayOSSection.tsx`: `current.apiKey`, `current.checksumKey`, `errors.payosApiKey`, `errors.payosChecksumKey`, `errors.payosClientId`, `event.target`, `initial.hasPayosApiKey`, `initial.hasPayosChecksumKey`, `initial.payosClientId`, `values.payosApiKey`, `values.payosChecksumKey`, `values.payosClientId`, `visibleSecrets.apiKey`, `visibleSecrets.checksumKey`.
- Nhóm: form/settings. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R70. /store-owner/platform-fees

- Page: `src/pages/store-owner/PlatformFeesPage.tsx`. Layout: StoreOwnerLayout. Quyền: storeOwner.
- Service được tham chiếu: `ownerShopApi.getMyShopProfile`, `platformFeeApi.createPaymentLink`, `platformFeeApi.getShopStatements`, `platformFeeApi.simulateWebhook`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/platform-fees/types/platformFee.types.ts` (`PlatformFeeStatementStatus`, `PlatformFeeConfigResponse`, `GetAdminPlatformFeesParams`, `UpdatePlatformFeeConfigRequest`, `PlatformFeeStatementSummary`, `OrderPlatformFeeItem`, `PlatformFeeAdjustmentItem`, `PlatformFeePaymentItem`, `PlatformFeeStatementHistoryItem`, `PlatformFeeStatementDetail`, `PlatformFeePaymentLinkResponse`, `GetShopPlatformFeesParams`, `RecordExternalPaymentRequest`, `PagedResult`); `src/features/shops/types/ownerShop.types.ts` (`ShopStatus`, `ShopProfileDocument`, `OwnerShop`, `UpdateBusinessInfo`, `ShopPaymentConfig`, `UpdateShopPaymentConfigRequest`); `src/shared/types/index.ts` ().
- Hook/state/router: `useToastStore.getState`, `useToastStore.getState().removeToast`.
- Child component: `src/features/platform-fees/components/StatusBadge.tsx`, `src/features/platform-fees/components/PlatformFeePaymentModal.tsx`, `src/shared/components/pagination/index.ts`.
- Tương tác source: `onChange`, `onClick`, `onClose`, `onPageChange`, `onPaymentSuccess`.
- Field đọc trong JSX — `src/pages/store-owner/PlatformFeesPage.tsx`: `e.target`, `e.target.value`, `payingStatement.billingMonth`, `payingStatement.billingYear`, `shop?.status`, `statements.length`, `stmt.billingMonth`, `stmt.billingYear`, `stmt.dueAt`, `stmt.id`, `stmt.paidAt`, `stmt.status`.
- Field đọc trong JSX — `src/features/platform-fees/components/PlatformFeePaymentModal.tsx`: `paymentData.checkoutUrl`, `paymentData.orderCode`.
- Nhóm: list/workspace. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R71. /store-owner/platform-fees/:statementId

- Page: `src/pages/store-owner/PlatformFeeDetailPage.tsx`. Layout: StoreOwnerLayout. Quyền: storeOwner.
- Service được tham chiếu: `ownerShopApi.getMyShopProfile`, `platformFeeApi.createPaymentLink`, `platformFeeApi.getStatementDetail`, `platformFeeApi.simulateWebhook`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/platform-fees/types/platformFee.types.ts` (`PlatformFeeStatementStatus`, `PlatformFeeConfigResponse`, `GetAdminPlatformFeesParams`, `UpdatePlatformFeeConfigRequest`, `PlatformFeeStatementSummary`, `OrderPlatformFeeItem`, `PlatformFeeAdjustmentItem`, `PlatformFeePaymentItem`, `PlatformFeeStatementHistoryItem`, `PlatformFeeStatementDetail`, `PlatformFeePaymentLinkResponse`, `GetShopPlatformFeesParams`, `RecordExternalPaymentRequest`, `PagedResult`); `src/features/shops/types/ownerShop.types.ts` (`ShopStatus`, `ShopProfileDocument`, `OwnerShop`, `UpdateBusinessInfo`, `ShopPaymentConfig`, `UpdateShopPaymentConfigRequest`); `src/shared/types/index.ts` ().
- Hook/state/router: `useParams`, `useToastStore.getState`, `useToastStore.getState().removeToast`.
- Child component: `src/features/platform-fees/components/StatusBadge.tsx`, `src/features/platform-fees/components/PlatformFeePaymentModal.tsx`.
- Tương tác source: `onClick`, `onClose`, `onPaymentSuccess`.
- Field đọc trong JSX — `src/pages/store-owner/PlatformFeeDetailPage.tsx`: `adj.amount`, `adj.createdAt`, `adj.id`, `adj.reason`, `adj.type`, `h.adjustmentAmount`, `h.changeAmount`, `h.changeType`, `h.createdAt`, `h.id`, `h.reason`, `ord.calculatedAt`, `ord.feeRate`, `ord.id`, `ord.orderId`, `pmt.createdAt`, `pmt.gatewayTransactionId`, `pmt.id`, `pmt.paidAt`, `pmt.paymentMethod`, `pmt.status`, `statement.adjustments`, `statement.adjustments.length`, `statement.adjustments?.length`, `statement.billingMonth`, `statement.billingYear`, `statement.dueAt`, `statement.histories`, `statement.histories.length`, `statement.histories?.length`, `statement.id`, `statement.issuedAt`, `statement.orders`, `statement.orders.length`, `statement.orders?.length`, `statement.paidAt`, `statement.payments`, `statement.payments.length`, `statement.payments?.length`, `statement.status`.
- Field đọc trong JSX — `src/features/platform-fees/components/PlatformFeePaymentModal.tsx`: `paymentData.checkoutUrl`, `paymentData.orderCode`.
- Nhóm: detail/tabs. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R72. /store-owner/profile

- Page: `src/pages/store-owner/ShopProfilePage.tsx`. Layout: StoreOwnerLayout. Quyền: storeOwner.
- Service được tham chiếu: `ownerShopApi.getMyShopProfile`, `ownerShopApi.updateBusinessInfo`, `ownerShopApi.updateImages`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/shops/types/ownerShop.types.ts` (`ShopStatus`, `ShopProfileDocument`, `OwnerShop`, `UpdateBusinessInfo`, `ShopPaymentConfig`, `UpdateShopPaymentConfigRequest`); `src/shared/types/index.ts` ().
- Hook/state/router: `useAddressLocation`, `useForm`, `useToastStore.getState`, `useToastStore.getState().removeToast`.
- Child component: `src/features/shops/components/ShopProfileHeader.tsx`, `src/features/shops/components/ShopProfileDetails.tsx`, `src/features/shops/components/ShopBusinessInfoForm.tsx`.
- Tương tác source: `onCancel`, `onChange`, `onClick`, `onEdit`, `onError`, `onFileChange`, `onSubmit`, `onUndo`.
- Field đọc trong JSX — `src/features/shops/components/ShopProfileHeader.tsx`: `meta.badge`, `meta.dot`, `meta.label`, `shop.coverImageUrl`, `shop.logoUrl`, `shop.name`.
- Field đọc trong JSX — `src/features/shops/components/ShopProfileDetails.tsx`: `document.documentType`, `document.fileUrl`, `document.id`, `document.originalFileName`, `documents.length`, `meta.band`, `meta.description`, `meta.tile`, `segment.from`, `segment.to`, `shop.businessLicenseNo`, `shop.description`, `shop.latitude`, `shop.longitude`, `shop.name`, `shop.status`.
- Field đọc trong JSX — `src/features/shops/components/ShopBusinessInfoForm.tsx`: `errors.addressLine`, `errors.addressLine?.message`, `errors.city`, `errors.city?.message`, `errors.closingTime`, `errors.closingTime?.message`, `errors.description`, `errors.description?.message`, `errors.district`, `errors.district?.message`, `errors.latitude`, `errors.latitude?.message`, `errors.longitude`, `errors.longitude?.message`, `errors.name`, `errors.name?.message`, `errors.openingTime`, `errors.openingTime?.message`, `errors.root`, `errors.root.server`, `errors.root.server.message`, `errors.root?.server`, `errors.root?.server?.message`, `errors.ward`, `errors.ward?.message`, `file.name`, `imageErrors.coverImage`, `imageErrors.logo`, `location.accuracy`, `location.error`, `location.phase`, `shop.coverImageUrl`, `shop.logoUrl`.
- Nhóm: form/settings. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R73. /store-owner/warnings

- Page: `src/features/warnings/pages/WarningHistoryPage.tsx`. Layout: StoreOwnerLayout. Quyền: storeOwner.
- Service được tham chiếu: `productApi.get`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/products/types/product.types.ts` (`Option`, `ProductOptions`, `ProductImage`, `ProductValue`, `ProductAttribute`, `ProductVariant`, `ProductListItem`, `Product`, `Page`, `FlashDeal`, `DraftValue`, `DraftAttribute`, `Combination`, `PriceInput`, `PriceMap`, `ProductListQuery`, `ProductSummary`, `ProductListManagement`, `AttributeValueContent`, `ProductContentManagement`, `RemoveViolatingProduct`, `RevisionContentSnapshot`, `ProductRevisionHistory`); `src/features/warnings/types/warning.types.ts` (`Warning`, `WarningPage`, `IssueViolationWarning`); `src/shared/types/index.ts` (); `src/shared/types/moderationAction.types.ts` (`ModerationActionResult`).
- Hook/state/router: `useNavigate`.
- Child component: `src/shared/components/pagination/index.ts`, `src/features/warnings/components/WarningItems.tsx`.
- Tương tác source: `onClick`, `onPageChange`.
- Field đọc trong JSX — `src/features/warnings/pages/WarningHistoryPage.tsx`: `items.length`.
- Field đọc trong JSX — `src/features/warnings/components/WarningItems.tsx`: `meta.border`, `meta.color`, `meta.description`, `meta.label`, `w.dealBlocked`, `w.dueAt`, `w.firstEditedAt`, `w.id`, `w.productName`, `w.reason`, `w.requiredCorrection`, `w.resolvedAt`.
- Nhóm: list/workspace. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=true, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R74. /store-owner/reviews

- Page: `src/pages/store-owner/StoreOwnerReviewsPage.tsx`. Layout: StoreOwnerLayout. Quyền: storeOwner.
- Service được tham chiếu: `feedbackApi.deleteReply`, `feedbackApi.editReply`, `feedbackApi.getShopFeedbacks`, `feedbackApi.replyFeedback`. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: `src/features/feedbacks/types/feedback.types.ts` (`FeedbackReply`, `FeedbackItem`, `CreateFeedbackInput`, `UpdateFeedbackInput`, `CreateFeedbackReplyInput`, `UpdateFeedbackReplyInput`, `ShopFeedbacksQueryParams`, `FeedbackHistoryResponse`); `src/shared/types/index.ts` ().
- Hook/state/router: `useToastStore.getState`, `useToastStore.getState().removeToast`.
- Child component: `src/features/feedbacks/components/ShopFeedbackManagementPanel.tsx`, `src/shared/components/pagination/index.ts`, `src/shared/components/modal/BaseModal.tsx`.
- Tương tác source: `onChange`, `onClick`, `onClose`, `onDeleteReply`, `onEditReply`, `onMouseDown`, `onPageChange`, `onRatingChange`, `onReply`, `onReplyFilterChange`, `onSubmit`, `onTodayToggle`.
- Field đọc trong JSX — `src/features/feedbacks/components/ShopFeedbackManagementPanel.tsx`: `e.target`, `editReplyContent.length`, `feedbacks.length`, `item.comment`, `item.createdAt`, `item.customerAvatarUrl`, `item.customerName`, `item.id`, `item.imageUrl`, `item.images`, `item.images.length`, `item.orderCode`, `item.productName`, `item.rating`, `item.reply`, `item.reply.content`, `item.reply.createdAt`, `item.reply.repliedByName`, `item.shopName`, `item.variantName`, `replyContent.length`, `replyTarget.comment`, `replyTarget?.comment`, `replyTarget?.customerName`, `replyTarget?.productName`, `replyTarget?.rating`.
- Nhóm: list/workspace. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=true, empty=false, error=true. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

### R75. *

- Page: `src/pages/commons/NotFoundPage.tsx`. Layout: —. Quyền: public.
- Service được tham chiếu: Không có service HTTP được phát hiện. Xem bảng endpoint để đối chiếu method, URL và response generic.
- DTO/type nguồn: Không có DTO import trực tiếp; xem generic service/props.
- Hook/state/router: `useNavigate`.
- Child component: Inline trong page.
- Tương tác source: `onClick`.
- Nhóm: misc. Dấu hiệu trạng thái trong source (chưa xác minh bằng UI): loading=false, empty=true, error=false. Cần kiểm tra lại từng nhánh và skeleton khi redesign.

## Bảng endpoint/service được route tham chiếu

Các method có thể có nhiều HTTP request/nhánh. “void / inferred” nghĩa là không có generic tại callsite, không khẳng định response rỗng. Helper URL cục bộ như path/base/url phải đọc cùng file service.

| File | Function/service method | HTTP | URL expression | Response generic |
|---|---|---|---|---|
| `src/features/addresses/api/addressApi.ts` | `createAddress` | POST | `"/user-addresses"` | `ApiResponse<UserAddress>` |
| `src/features/addresses/api/addressApi.ts` | `deleteAddress` | DELETE | `(addressId: string) => `/user-addresses/${addressId}`(addressId)` | `void / inferred` |
| `src/features/addresses/api/addressApi.ts` | `getAddresses` | GET | `"/user-addresses"` | `ApiResponse<UserAddress[]>` |
| `src/features/addresses/api/addressApi.ts` | `setDefaultAddress` | PUT | `(addressId: string) => `/user-addresses/${addressId}/default`(addressId)` | `ApiResponse<UserAddress>` |
| `src/features/addresses/api/addressApi.ts` | `updateAddress` | PUT | `(addressId: string) => `/user-addresses/${addressId}`(addressId)` | `ApiResponse<UserAddress>` |
| `src/features/admin-analytics/api/platformAnalyticsApi.ts` | `getDashboard` | GET | `"/admin/analytics"` | `ApiResponse<PlatformDashboardAnalytics>` |
| `src/features/audit-logs/api/auditLogApi.ts` | `getLogs` | GET | `"/admin/audit-logs"` | `ApiResponse<BackendAuditLogPageResponse>` |
| `src/features/auth/api/authApi.ts` | `changePassword` | PUT | `"/auth/me/password"` | `ApiMessageResponse` |
| `src/features/auth/api/authApi.ts` | `forgotPassword` | POST | `"/auth/password-resets"` | `ApiMessageResponse` |
| `src/features/auth/api/authApi.ts` | `getMe` | GET | `"/auth/me"` | `ApiResponse<CurrentUserResponse>` |
| `src/features/auth/api/authApi.ts` | `login` | POST | `"/auth/sessions"` | `ApiResponse<TokenPair>` |
| `src/features/auth/api/authApi.ts` | `register` | POST | `"/auth/registrations"` | `ApiMessageResponse` |
| `src/features/auth/api/authApi.ts` | `resetPassword` | POST | `"/auth/password-reset-verifications"` | `ApiMessageResponse` |
| `src/features/auth/api/authApi.ts` | `verifyRegistration` | POST | `"/auth/registration-verifications"` | `ApiMessageResponse` |
| `src/features/cart/api/cartApi.ts` | `addItem` | POST | `"/cart/items"` | `ApiResponse<Cart>` |
| `src/features/cart/api/cartApi.ts` | `getPage` | GET | `"/cart/page"` | `ApiResponse<CartPage>` |
| `src/features/cart/api/cartApi.ts` | `removeItem` | DELETE | `(itemId: string) => `/cart/items/${itemId}`(itemId)` | `ApiResponse<CartMutationResponse>` |
| `src/features/cart/api/cartApi.ts` | `summary` | GET | `"/cart/summary"` | `ApiResponse<CartSummary>` |
| `src/features/cart/api/cartApi.ts` | `updateQuantity` | PUT | `(itemId: string) => `/cart/items/${itemId}`(itemId)` | `ApiResponse<CartMutationResponse>` |
| `src/features/checkout/api/checkoutApi.ts` | `context` | GET | `"/checkout/context"` | `ApiResponse<CheckoutContext>` |
| `src/features/checkout/api/checkoutApi.ts` | `place` | POST | `"/checkout/orders"` | `ApiResponse<CheckoutOrder>` |
| `src/features/checkout/api/checkoutApi.ts` | `quote` | POST | `"/checkout/quotes"` | `ApiResponse<CheckoutQuote>` |
| `src/features/feedback/api/feedbackApi.ts` | `remove` | DELETE | `(feedbackId: string) => `/feedback/${encodeURIComponent(feedbackId)}`(id)` | `void / inferred` |
| `src/features/feedback/api/feedbackApi.ts` | `restore` | POST | `(feedbackId: string) => `/feedback/${encodeURIComponent(feedbackId)}/restore`(id)` | `void / inferred` |
| `src/features/feedbacks/api/feedbackApi.ts` | `addFeedback` | POST | `"/feedbacks"` | `ApiResponse<FeedbackItem>` |
| `src/features/feedbacks/api/feedbackApi.ts` | `deleteFeedback` | DELETE | `(id: string) => `/feedbacks/${id}`(feedbackId)` | `void / inferred` |
| `src/features/feedbacks/api/feedbackApi.ts` | `deleteReply` | DELETE | `(id: string) => `/feedbacks/${id}/reply`(feedbackId)` | `void / inferred` |
| `src/features/feedbacks/api/feedbackApi.ts` | `editFeedback` | PUT | `(id: string) => `/feedbacks/${id}`(feedbackId)` | `ApiResponse<FeedbackItem>` |
| `src/features/feedbacks/api/feedbackApi.ts` | `editReply` | PUT | `(id: string) => `/feedbacks/${id}/reply`(feedbackId)` | `ApiResponse<FeedbackReply>` |
| `src/features/feedbacks/api/feedbackApi.ts` | `getAdminFeedbacks` | GET | `"/feedbacks/admin"` | `ApiResponse<FeedbackHistoryResponse>` |
| `src/features/feedbacks/api/feedbackApi.ts` | `getHistory` | GET | `"/feedbacks/history"` | `ApiResponse<FeedbackHistoryResponse>` |
| `src/features/feedbacks/api/feedbackApi.ts` | `getShopFeedbacks` | GET | `"/feedbacks/shop"` | `ApiResponse<FeedbackHistoryResponse>` |
| `src/features/feedbacks/api/feedbackApi.ts` | `replyFeedback` | POST | `(id: string) => `/feedbacks/${id}/reply`(feedbackId)` | `ApiResponse<FeedbackReply>` |
| `src/features/feedbacks/api/feedbackApi.ts` | `uploadImage` | POST | `"/feedbacks/images"` | `ApiResponse<string>` |
| `src/features/flash-deals/api/flashDealApi.ts` | `cancelFlashDeal` | POST | `(dealId: string) => `/flash-deals/${dealId}/cancellations`(id)` | `void / inferred` |
| `src/features/flash-deals/api/flashDealApi.ts` | `createFlashDeal` | POST | `"/flash-deals"` | `ApiResponse<import("@/features/flash-deals/types/flashDeal.types").ShopFlashDealDetail>` |
| `src/features/flash-deals/api/flashDealApi.ts` | `getEligibleProducts` | GET | `"/flash-deals/eligible-products"` | `ApiResponse<import("@/features/flash-deals/types/flashDeal.types").ShopEligibleProduct[]>` |
| `src/features/flash-deals/api/flashDealApi.ts` | `getNearbyDeals` | GET | `"/flash-deals"` | `ApiResponse<FlashDealCursorResult>` |
| `src/features/flash-deals/api/flashDealApi.ts` | `getShopFlashDealDetail` | GET | `(dealId: string) => `/flash-deals/shop/${dealId}`(id)` | `ApiResponse<import("@/features/flash-deals/types/flashDeal.types").ShopFlashDealDetail>` |
| `src/features/flash-deals/api/flashDealApi.ts` | `getShopFlashDeals` | GET | `"/flash-deals/shop"` | `ApiResponse<{ items: import("@/features/flash-deals/types/flashDeal.types").ShopFlashDealListItem[]; totalItems: number; page: number; pageSize: number; }>` |
| `src/features/flash-deals/api/flashDealApi.ts` | `hideFlashDeal` | POST | `(dealId: string) => `/flash-deals/${dealId}/hides`(id)` | `void / inferred` |
| `src/features/flash-deals/api/flashDealApi.ts` | `unhideFlashDeal` | DELETE | `(dealId: string) => `/flash-deals/${dealId}/hides`(id)` | `void / inferred` |
| `src/features/flash-deals/api/flashDealApi.ts` | `updateFlashDeal` | PUT | `(dealId: string) => `/flash-deals/${dealId}`(id)` | `ApiResponse<import("@/features/flash-deals/types/flashDeal.types").ShopFlashDealDetail>` |
| `src/features/orders/api/orderApi.ts` | `cancel` | POST | `(id: string) => `/orders/${encodeURIComponent(id)}/cancellations`(orderId)` | `ApiResponse<CheckoutOrder>` |
| `src/features/orders/api/orderApi.ts` | `order` | GET | `(id: string) => `/orders/${encodeURIComponent(id)}`(orderId)` | `ApiResponse<CheckoutOrder>` |
| `src/features/orders/api/orderApi.ts` | `orderPage` | GET | `"/orders/page"` | `ApiResponse<OrderHistoryPage>` |
| `src/features/payments/api/paymentApi.ts` | `detail` | GET | ``/payments/${encodeURIComponent(paymentId)}`` | `ApiResponse<CustomerPaymentDetail>` |
| `src/features/payments/api/paymentApi.ts` | `page` | GET | `"/payments"` | `ApiResponse<CustomerPaymentPage>` |
| `src/features/payments/api/paymentApi.ts` | `payment` | POST | `(id: string) => `/payments/orders/${encodeURIComponent(id)}/payment-link`(orderId)` | `ApiResponse<CheckoutOrder>` |
| `src/features/payments/api/paymentApi.ts` | `simulate` | POST | ``/payments/simulation/${encodeURIComponent(orderId)}`` | `ApiResponse<CheckoutOrder>` |
| `src/features/platform-fees/api/platformFeeApi.ts` | `createPaymentLink` | POST | `(shopId: string, statementId: string) => `/shops/${shopId}/platform-fees/${statementId}/payment-link`(shopId, statementId)` | `ApiResponse<PlatformFeePaymentLinkResponse>` |
| `src/features/platform-fees/api/platformFeeApi.ts` | `generateMonthly` | POST | `"/admin/platform-fee-statements/generate-monthly"` | `ApiResponse<number>` |
| `src/features/platform-fees/api/platformFeeApi.ts` | `getActiveConfig` | GET | `"/admin/platform-fee-config"` | `ApiResponse<PlatformFeeConfigResponse>` |
| `src/features/platform-fees/api/platformFeeApi.ts` | `getAdminStatements` | GET | `"/admin/platform-fee-statements"` | `ApiResponse<PagedResult<PlatformFeeStatementSummary>>` |
| `src/features/platform-fees/api/platformFeeApi.ts` | `getAllConfigs` | GET | `"/admin/platform-fee-configs"` | `ApiResponse<PlatformFeeConfigResponse[]>` |
| `src/features/platform-fees/api/platformFeeApi.ts` | `getShopStatements` | GET | `(shopId: string) => `/shops/${shopId}/platform-fees`(shopId)` | `ApiResponse<PagedResult<PlatformFeeStatementSummary>>` |
| `src/features/platform-fees/api/platformFeeApi.ts` | `getStatementDetail` | GET | `(shopId: string, statementId: string) => `/shops/${shopId}/platform-fees/${statementId}`(shopId, statementId)` | `ApiResponse<PlatformFeeStatementDetail>` |
| `src/features/platform-fees/api/platformFeeApi.ts` | `processOverdue` | POST | `"/admin/platform-fee-statements/process-overdue"` | `ApiResponse<number>` |
| `src/features/platform-fees/api/platformFeeApi.ts` | `recordExternalPayment` | POST | `(statementId: string) => `/admin/platform-fee-statements/${statementId}/reconciliation`(statementId)` | `ApiResponse<string>` |
| `src/features/platform-fees/api/platformFeeApi.ts` | `simulateWebhook` | POST | `"/platform-fees/payos/webhook"` | `void / inferred` |
| `src/features/platform-fees/api/platformFeeApi.ts` | `updateConfig` | PUT | `"/admin/platform-fee-config"` | `ApiResponse<PlatformFeeConfigResponse>` |
| `src/features/products/api/productApi.ts` | `addValues` | POST | `(productId: string) => `/products/${productId}/attribute-values`(id)` | `ApiResponse<Product>` |
| `src/features/products/api/productApi.ts` | `approveContent` | POST | `(productId: string, revisionId: string) => `/products/admin/${encodeURIComponent(productId)}/content-revisions/${encodeURIComponent(revisionId)}/approve`(productId, revisionId)` | `ApiResponse<ModerationActionResult>` |
| `src/features/products/api/productApi.ts` | `create` | POST | `"/products"` | `ApiResponse<Product>` |
| `src/features/products/api/productApi.ts` | `deals` | GET | `(productId: string) => `/products/${productId}/flash-deals`(id)` | `ApiResponse<Page<FlashDeal>>` |
| `src/features/products/api/productApi.ts` | `get` | GET | `(productId: string) => `/products/${productId}`(id)` | `ApiResponse<Product>` |
| `src/features/products/api/productApi.ts` | `images` | PUT | `(productId: string) => `/products/${productId}/images`(id)` | `ApiResponse<Product>` |
| `src/features/products/api/productApi.ts` | `list` | GET | `"/products"` | `ApiResponse<Page<ProductListItem>>` |
| `src/features/products/api/productApi.ts` | `options` | GET | `"/products/options"` | `ApiResponse<ProductOptions>` |
| `src/features/products/api/productApi.ts` | `remove` | DELETE | `(productId: string) => `/products/${productId}`(id)` | `void / inferred` |
| `src/features/products/api/productApi.ts` | `removeForViolation` | DELETE | `(productId: string) => `/products/admin/${encodeURIComponent(productId)}/violation`(productId)` | `ApiResponse<ModerationActionResult>` |
| `src/features/products/api/productApi.ts` | `summary` | GET | `"/products/summary"` | `ApiResponse<ProductSummary>` |
| `src/features/products/api/productApi.ts` | `update` | PUT | `(productId: string) => `/products/${productId}`(id)` | `ApiResponse<Product>` |
| `src/features/products/api/productApi.ts` | `updateStatus` | PATCH | `(productId: string) => `/products/${productId}/status`(productId)` | `ApiResponse<Product>` |
| `src/features/products/api/productApi.ts` | `updateVariant` | PUT | `(productId: string, variantId: string) => `/products/${productId}/variants/${variantId}`(productId, variantId)` | `ApiResponse<Product>` |
| `src/features/products/api/productApi.ts` | `valueImage` | PUT | `(productId: string, valueId: string) => `/products/${productId}/values/${valueId}/image`(id, valueId)` | `ApiResponse<Product>` |
| `src/features/profiles/api/profileApi.ts` | `updateProfile` | PUT | `"/users/me"` | `ApiResponse<CurrentUserResponse>` |
| `src/features/profiles/api/profileApi.ts` | `uploadAvatar` | PUT | `"/users/me/avatar"` | `ApiResponse<CurrentUserResponse>` |
| `src/features/refunds/api/refundApi.ts` | `confirm` | POST | ``${path(id)}/destination/confirm`` | `ApiResponse<Refund>` |
| `src/features/refunds/api/refundApi.ts` | `destination` | POST | ``${path(id)}/destination`` | `ApiResponse<Refund>` |
| `src/features/refunds/api/refundApi.ts` | `forOrder` | GET | ``/orders/${encodeURIComponent(orderId)}/refunds`` | `ApiResponse<Refund[]>` |
| `src/features/refunds/api/refundApi.ts` | `qr` | POST | ``/refunds/shop/${encodeURIComponent(id)}/vietqr`` | `ApiResponse<RefundQr>` |
| `src/features/refunds/api/refundApi.ts` | `request` | POST | ``/orders/${encodeURIComponent(orderId)}/refunds/requests`` | `ApiResponse<Refund>` |
| `src/features/refunds/api/refundApi.ts` | `review` | POST | ``/refunds/shop/${encodeURIComponent(id)}/${approve ? "approve" : "reject"}`` | `ApiResponse<Refund>` |
| `src/features/refunds/api/refundApi.ts` | `transfer` | POST | ``/refunds/shop/${encodeURIComponent(id)}/transfer`` | `ApiResponse<Refund>` |
| `src/features/refunds/api/refundApi.ts` | `upload` | POST | `url` | `ApiResponse<RefundImage>` |
| `src/features/refunds/api/refundApi.ts` | `withdraw` | POST | ``${path(id)}/withdraw`` | `ApiResponse<Refund>` |
| `src/features/shop-applications/api/adminShopApplicationApi.ts` | `getAll` | GET | `"/shop-applications"` | `ApiResponse<ShopApplication[]>` |
| `src/features/shop-applications/api/adminShopApplicationApi.ts` | `getById` | GET | `(applicationId: string) => `/shop-applications/${applicationId}`(id)` | `ApiResponse<ShopApplication>` |
| `src/features/shop-applications/api/adminShopApplicationApi.ts` | `review` | POST | `(applicationId: string) => `/shop-applications/${applicationId}/reviews`(id)` | `ApiResponse<ShopApplication>` |
| `src/features/shop-applications/api/shopApplicationApi.ts` | `cancel` | POST | `(applicationId: string) => `/shop-applications/${applicationId}/cancellations`(applicationId)` | `void / inferred` |
| `src/features/shop-applications/api/shopApplicationApi.ts` | `create` | POST | `"/shop-applications"` | `ApiResponse<ShopApplication>` |
| `src/features/shop-applications/api/shopApplicationApi.ts` | `getMine` | GET | `"/shop-applications/me/latest"` | `ApiResponse<ShopApplication>` |
| `src/features/shop-applications/api/shopApplicationApi.ts` | `resubmit` | POST | `(applicationId: string) => `/shop-applications/${applicationId}/revisions`(applicationId)` | `ApiResponse<ShopApplication>` |
| `src/features/shop-orders/api/shopOrderApi.ts` | `action` | POST | ``${base(shopId)}/${encodeURIComponent(orderId)}/${action}`` | `ApiResponse<ShopOrder>` |
| `src/features/shop-orders/api/shopOrderApi.ts` | `detail` | GET | ``${base(shopId)}/${encodeURIComponent(orderId)}`` | `ApiResponse<ShopOrder>` |
| `src/features/shop-orders/api/shopOrderApi.ts` | `page` | GET | `base(shopId)` | `ApiResponse<ShopOrderPage>` |
| `src/features/shop-orders/api/shopOrderApi.ts` | `simulateRemaining` | POST | ``/payments/simulation/shops/${encodeURIComponent(shopId)}/${encodeURIComponent(orderId)}/remaining`` | `void / inferred` |
| `src/features/shop-staffs/api/shopStaffApi.ts` | `getInvitations` | GET | `"/staff-invitations/shop"` | `ApiResponse<PaginatedResponse<StaffInvitation>>` |
| `src/features/shop-staffs/api/shopStaffApi.ts` | `getStaffActivityLogs` | GET | `(staffId: string) => `/shop-staff/${staffId}/activity-logs`(staffId)` | `ApiResponse<PaginatedResponse<StaffActivityLog>>` |
| `src/features/shop-staffs/api/shopStaffApi.ts` | `getStaffList` | GET | `"/shop-staff"` | `ApiResponse<PaginatedResponse<ShopStaff>>` |
| `src/features/shop-staffs/api/shopStaffApi.ts` | `inviteStaff` | POST | `"/staff-invitations"` | `ApiResponse<StaffInvitation>` |
| `src/features/shop-staffs/api/shopStaffApi.ts` | `removeStaff` | DELETE | `(staffId: string) => `/shop-staff/${staffId}`(staffId)` | `ApiResponse<null>` |
| `src/features/shop-staffs/api/shopStaffApi.ts` | `revokeInvitation` | DELETE | `(invitationId: string) => `/staff-invitations/${invitationId}`(invitationId)` | `ApiResponse<null>` |
| `src/features/shop-staffs/api/shopStaffApi.ts` | `searchStaffCandidates` | GET | `"/staff-invitations/candidates"` | `ApiResponse<PaginatedResponse<StaffCandidate>>` |
| `src/features/shop-staffs/api/shopStaffApi.ts` | `updateStaff` | PUT | `(staffId: string) => `/shop-staff/${staffId}`(staffId)` | `ApiResponse<null>` |
| `src/features/shops/api/ownerShopApi.ts` | `getMyPaymentConfig` | GET | `"/shops/me/payment-config"` | `ApiResponse<ShopPaymentConfig>` |
| `src/features/shops/api/ownerShopApi.ts` | `getMyShopProfile` | GET | `"/shops/me"` | `ApiResponse<OwnerShop>` |
| `src/features/shops/api/ownerShopApi.ts` | `updateBusinessInfo` | PUT | `"/shops/me/business-info"` | `ApiResponse<null>` |
| `src/features/shops/api/ownerShopApi.ts` | `updateImages` | PUT | `"/shops/me/images"` | `ApiResponse<null>` |
| `src/features/shops/api/ownerShopApi.ts` | `updateMyPaymentConfig` | PUT | `"/shops/me/payment-config"` | `ApiResponse<ShopPaymentConfig>` |
| `src/features/shops/api/shopApi.ts` | `getNearbyShops` | GET | `"/shops"` | `ApiResponse<NearbyShopsPagedResponse>` |
| `src/features/shops/api/shopApi.ts` | `updateModerationStatus` | PATCH | `(shopId: string) => `/shops/admin/${encodeURIComponent(shopId)}/status`(shopId)` | `ApiResponse<ModerationActionResult>` |
| `src/features/staff-invitations/api/staffInvitationApi.ts` | `acceptInvitation` | POST | `(invitationId: string) => `/staff-invitations/${invitationId}/acceptances`(invitationId)` | `void / inferred` |
| `src/features/staff-invitations/api/staffInvitationApi.ts` | `declineInvitation` | POST | `(invitationId: string) => `/staff-invitations/${invitationId}/rejections`(invitationId)` | `void / inferred` |
| `src/features/staff-invitations/api/staffInvitationApi.ts` | `getMyInvitations` | GET | `"/staff-invitations/me"` | `ApiResponse<StaffInvitation[]>` |
| `src/features/staff-shops/api/staffApi.ts` | `getAssociatedShops` | GET | `"/shop-staff/me/shops"` | `ApiResponse<AssociatedShop[]>` |
| `src/features/staff-shops/api/staffApi.ts` | `getShopDetail` | GET | `(shopId: string) => `/shop-staff/me/shops/${shopId}`(shopId)` | `ApiResponse<StaffShopDetail>` |
| `src/features/staff-shops/api/staffApi.ts` | `leaveShop` | DELETE | `(shopId: string) => `/shop-staff/me/shops/${shopId}`(shopId)` | `void / inferred` |
| `src/features/store-analytics/api/storeAnalyticsApi.ts` | `getDashboard` | GET | `"/shops/me/analytics"` | `ApiResponse<StoreDashboardAnalytics>` |
| `src/features/trust/api/adminTrustApi.ts` | `createLevel` | POST | `"/admin/trust/levels"` | `ApiResponse<AdminTrustLevel>` |
| `src/features/trust/api/adminTrustApi.ts` | `createPenaltyRule` | POST | `"/admin/trust/penalty-rules"` | `ApiResponse<PenaltyRule>` |
| `src/features/trust/api/adminTrustApi.ts` | `deletePenaltyRule` | DELETE | `(ruleId: string) => `/admin/trust/penalty-rules/${ruleId}`(ruleId)` | `void / inferred` |
| `src/features/trust/api/adminTrustApi.ts` | `getEarningRules` | GET | `"/admin/trust/earning-rules"` | `ApiResponse<TrustScoreRule[]>` |
| `src/features/trust/api/adminTrustApi.ts` | `getLevelHistory` | GET | `(levelId: string) => `/admin/trust/levels/${levelId}/history`(levelId)` | `ApiResponse<TrustLevelHistoryItem[]>` |
| `src/features/trust/api/adminTrustApi.ts` | `getLevels` | GET | `"/admin/trust/levels"` | `ApiResponse<AdminTrustLevel[]>` |
| `src/features/trust/api/adminTrustApi.ts` | `getPenaltyRules` | GET | `"/admin/trust/penalty-rules"` | `ApiResponse<PenaltyRule[]>` |
| `src/features/trust/api/adminTrustApi.ts` | `getScoreRules` | GET | `"/admin/trust/score-rules"` | `ApiResponse<TrustScoreRule[]>` |
| `src/features/trust/api/adminTrustApi.ts` | `updateEarningRule` | PUT | `(ruleId: string) => `/admin/trust/earning-rules/${ruleId}`(ruleId)` | `ApiResponse<TrustScoreRule>` |
| `src/features/trust/api/adminTrustApi.ts` | `updateLevelBenefits` | PUT | `(levelId: string) => `/admin/trust/levels/${levelId}/benefits`(levelId)` | `ApiResponse<AdminTrustLevel>` |
| `src/features/trust/api/adminTrustApi.ts` | `updateLevelInfo` | PUT | `(levelId: string) => `/admin/trust/levels/${levelId}/info`(levelId)` | `ApiResponse<AdminTrustLevel>` |
| `src/features/trust/api/adminTrustApi.ts` | `updateLevelRequirement` | PUT | `(levelId: string) => `/admin/trust/levels/${levelId}/requirements`(levelId)` | `ApiResponse<AdminTrustLevel>` |
| `src/features/trust/api/adminTrustApi.ts` | `updateLevelStatus` | PUT | `(levelId: string) => `/admin/trust/levels/${levelId}/status`(levelId)` | `ApiResponse<AdminTrustLevel>` |
| `src/features/trust/api/adminTrustApi.ts` | `updatePenaltyRule` | PUT | `(ruleId: string) => `/admin/trust/penalty-rules/${ruleId}`(ruleId)` | `ApiResponse<PenaltyRule>` |
| `src/features/trust/api/adminTrustApi.ts` | `updateScoreRule` | PUT | `(ruleId: string) => `/admin/trust/score-rules/${ruleId}`(ruleId)` | `ApiResponse<TrustScoreRule>` |
| `src/features/trust/api/trustApi.ts` | `getHistory` | GET | `"/trust-score/me/history"` | `ApiResponse<BackendPaged<TrustScoreHistoryItem>>` |
| `src/features/trust/api/trustApi.ts` | `getLevelProgress` | GET | `"/trust-score/me/level-progress"` | `ApiResponse<LevelProgress>` |
| `src/features/trust/api/trustApi.ts` | `getLevels` | GET | `"/trust-score/levels"` | `ApiResponse<TrustLevelInfo[]>` |
| `src/features/trust/api/trustApi.ts` | `getOverview` | GET | `"/trust-score/me"` | `ApiResponse<TrustScoreOverview>` |
| `src/features/trust/api/trustApi.ts` | `getPenalties` | GET | `"/trust-score/me/penalties"` | `ApiResponse<BackendPaged<PenaltyItem>>` |
| `src/features/trust/api/trustApi.ts` | `getPenaltyRecovery` | GET | `(penaltyId: string) => `/trust-score/me/penalties/${penaltyId}/recovery`(penaltyId)` | `ApiResponse<PenaltyRecovery>` |
| `src/features/users/api/userApi.ts` | `getUserDetails` | GET | `(userId: string) => `/users/${userId}`(id)` | `ApiResponse<UserDetailsResponse>` |
| `src/features/users/api/userApi.ts` | `getUsers` | GET | `"/users"` | `ApiResponse<BackendPagedUser>` |
| `src/features/users/api/userApi.ts` | `updateStatus` | PATCH | `(userId: string) => `/users/${userId}/status`(id)` | `ApiResponse<string>` |
| `src/features/warnings/api/warningApi.ts` | `issueForProduct` | POST | `(productId: string) => `/warnings/admin/products/${encodeURIComponent(productId)}`(productId)` | `ApiResponse<ModerationActionResult>` |

## Shared components tìm thấy

Ưu tiên tận dụng hệ thống hiện có, không thêm UI library. Các component/hook bên dưới được ít nhất hai route tham chiếu (bao gồm alias); chỉ số là phụ thuộc tĩnh, không phải số render runtime.

- `src/features/addresses/hooks/useAddressLocation.ts`: 2 route.
- `src/features/auth/components/AuthFormCard.tsx`: 6 route.
- `src/features/auth/components/PasswordField.tsx`: 4 route.
- `src/features/cart/components/CartItemRow.tsx`: 2 route.
- `src/features/checkout/components/CancelOrderAction.tsx`: 2 route.
- `src/features/checkout/components/CheckoutOrderReceipt.tsx`: 2 route.
- `src/features/checkout/components/OrderPickupCode.tsx`: 4 route.
- `src/features/checkout/components/OrderPickupDeadlines.tsx`: 8 route.
- `src/features/checkout/components/OrderStatusPanel.tsx`: 7 route.
- `src/features/feedbacks/components/EditReviewModal.tsx`: 3 route.
- `src/features/feedbacks/components/RateOrderModal.tsx`: 3 route.
- `src/features/feedbacks/components/ShopFeedbackManagementPanel.tsx`: 3 route.
- `src/features/feedbacks/components/StarRatingInput.tsx`: 4 route.
- `src/features/flash-deals/components/CancelFlashDealModal.tsx`: 2 route.
- `src/features/flash-deals/components/ShopDealCard.tsx`: 2 route.
- `src/features/flash-deals/components/ToggleVisibilityModal.tsx`: 2 route.
- `src/features/payments/components/PaymentStatusBadge.tsx`: 2 route.
- `src/features/platform-fees/components/PlatformFeePaymentModal.tsx`: 2 route.
- `src/features/platform-fees/components/StatusBadge.tsx`: 3 route.
- `src/features/products/components/AttributesEditor.tsx`: 2 route.
- `src/features/products/components/GalleryEditor.tsx`: 2 route.
- `src/features/products/components/ProductDealsPanel.tsx`: 2 route.
- `src/features/products/components/ProductListSkeleton.tsx`: 4 route.
- `src/features/products/components/ProductUi.tsx`: 4 route.
- `src/features/products/components/VariantPrices.tsx`: 2 route.
- `src/features/products/hooks/useProductResource.ts`: 4 route.
- `src/features/products/hooks/useProductWorkspace.ts`: 4 route.
- `src/features/refunds/components/CustomerOrderRefunds.tsx`: 2 route.
- `src/features/refunds/components/RefundBankSelect.tsx`: 3 route.
- `src/features/refunds/components/RefundCard.tsx`: 2 route.
- `src/features/refunds/components/RefundCustomerInfo.tsx`: 4 route.
- `src/features/refunds/components/RefundDestinationForm.tsx`: 3 route.
- `src/features/refunds/components/RefundEvidenceGallery.tsx`: 6 route.
- `src/features/refunds/components/RefundFileUpload.tsx`: 6 route.
- `src/features/refunds/components/RefundItems.tsx`: 6 route.
- `src/features/refunds/components/RefundRequestForm.tsx`: 2 route.
- `src/features/refunds/components/RefundStatusBadge.tsx`: 8 route.
- `src/features/refunds/components/ShopOrderRefunds.tsx`: 2 route.
- `src/features/refunds/components/ShopRefundCard.tsx`: 3 route.
- `src/features/shop-applications/components/ShopApplicationForm.tsx`: 2 route.
- `src/features/shop-orders/components/ConfirmationCountdown.tsx`: 4 route.
- `src/features/shop-orders/components/HistoryActor.tsx`: 2 route.
- `src/features/shop-orders/components/PaymentQr.tsx`: 2 route.
- `src/features/shop-staffs/components/StaffStatusBadge.tsx`: 2 route.
- `src/features/shops/components/ShopProfileHeader.tsx`: 3 route.
- `src/features/shops/components/StoreReviewList.tsx`: 2 route.
- `src/features/store-analytics/components/index.ts`: 2 route.
- `src/features/store-analytics/hooks/useAnalyticsPeriod.ts`: 2 route.
- `src/features/trust/components/PanelState.tsx`: 2 route.
- `src/features/trust/components/TrustTabs.tsx`: 2 route.
- `src/features/trust/hooks/useApiResource.ts`: 2 route.
- `src/shared/components/analytics/index.ts`: 2 route.
- `src/shared/components/modal/BaseModal.tsx`: 18 route.
- `src/shared/components/modal/ErrorModal.tsx`: 5 route.
- `src/shared/components/modal/SearchInput.tsx`: 5 route.
- `src/shared/components/pagination/Pagination.tsx`: 3 route.
- `src/shared/components/pagination/index.ts`: 27 route.
- `src/shared/hooks/useDebounce.ts`: 2 route.

## Các trường hợp cần chú ý

- /shops/:id và /shop-profile cùng CustomerShopPage; /flash-deals/:id và /deals/:id cùng CustomerDealDetailPage. Giữ cả alias; /shop-profile không có :id, phải giữ cách page lấy shop context hiện tại.
- /checkout nhận context/query (bao gồm orderId cho receipt); /checkout/simulation dùng orderId/paymentId và mutation payment mô phỏng vào backend thật nếu bấm. Audit chỉ đọc, chưa chạy.
- TrustScorePage có overview/levels/history/penalties; TrustConfigurationPage có levels/penalty-rules/score-rules/earning-rules. Tab thiếu/sai điều hướng về tab mặc định. Dashboard admin dùng :tab? theo config analytics.
- /store-owner/products là parent workspace có index page; child new/:productId/:productId/flash-deals phải có ProductOptions.shop/categories trong context.
- ShopOrdersPage, ShopOrderDetailPage và StoreOwnerReviewsPage dùng chung owner/staff nhưng context shopId và quyền khác nhau. Mock phải có cả hai role.
- /admin/orders hiện chỉ ComingSoonPage, không tạo giả API orders admin. 404 là * ngoài guard/layout.
- src/pages/staff/StaffShopsPage.tsx không được import trong routes.tsx; /staff hiện redirect về /. Không xem file tồn tại là một route đang hoạt động. Entry main.tsx import src/App.tsx; cần theo re-export sang src/app/App.tsx khi thay shell. StaffDetailPage lấy staff từ location.state, reload trực tiếp có thể báo Staff member not found dù activity-log endpoint vẫn được gọi; fixture phải kiểm tra cả navigation state và direct URL.
- Loading/empty/error trong tài liệu là phát hiện code, chưa có visual/a11y/responsive review. Không khẳng định tất cả màn hình đã đủ ba trạng thái.

## Proposed order

1. Shell/layout: CustomerLayout, AccountLayout, WorkspaceLayout, role wrappers, headers/sidebar; session checking, toast, notifications/pending badges.
2. Tokens + primitive dùng chung: giữ green scale hiện tại; hợp nhất button/input/card/table/badge/pagination/modal/empty/skeleton/page header dựa trên thành phần sẵn có.
3. Mock: session/role, background header requests, HTTP response/error/pagination, upload, realtime, location/maps/banks; trạng thái normal/loading/empty/error. Không đổi public contract.
4. Auth (login/register/verify/reset) → public discovery/shop/deal → profile/address/workspace/invitations/shop applications.
5. Customer cart/checkout/order/payment/refund/reviews/trust; bao phủ query receipt và simulation.
6. Owner products (workspace/list/create/detail/history) → flash deals → shop settings/payment → staff/invitations.
7. Owner/staff orders/refunds/reviews; cập nhật cùng component rồi kiểm tra từng route/role riêng.
8. Owner/admin dashboards → admin lists (categories/users/shops/products/applications/fees/feedback/reviews/audit) → moderation detail và trust settings.
9. ComingSoon/404/redirect regression → QA toàn bộ route, tab, alias, responsive và a11y.

Không bắt đầu tokens, mock hoặc redesign trong lượt audit. ui-ux-pro-max chưa có trong danh sách skill hiện tại; chỉ cần xử lý trước giai đoạn ui-redesign-page.

## Tương tác và dữ liệu điều hướng cần giữ

| Nhóm | Tương tác / điều kiện cần kiểm tra ở bước redesign |
|---|---|
| Auth | Email/password, validation Zod và lỗi field server; return URL qua location.state; OTP có hai purpose registration/password-reset, giữ resend và flow tương ứng. |
| Discovery | Cho phép GPS hoặc địa chỉ lưu; radius, category/distance/price, keyword, shop; load older/apply new deals; chọn shop/map marker. |
| Profile | Edit profile/avatar, change password; CRUD/default address, chọn tọa độ và reverse geocode. |
| Checkout | Context/quote/place; selected cart items, địa chỉ, payment method; receipt theo query orderId; simulation theo orderId/paymentId, paid/cancelled/expired. |
| Customer orders/payments/refunds | Pagination và filter status/type tùy page; order cancellation, QR/link/deadline, refund evidence upload và item quantities, destination bank/confirmation/withdraw; rating/review modal. |
| Shop applications | Latest/history, create/resubmit/cancel; edit applicationId; admin list/detail/review. |
| Workspace/staff | Associated shops và leave confirmation; invitation accept/decline/revoke, search candidates, edit/remove staff; staff detail cần location.state.staff. |
| Products | Keyword/status/category tùy page, pagination; options context, create/edit, gallery/image upload, attribute values/variant prices, status/delete và flash-deal history; content revision/moderation actions. |
| Flash deals | List/filter, create/edit form, eligible products, detail, cancel/hide/unhide confirmation; staff xem theo shop context. |
| Shop orders/refunds | Status/search/orderDate/page, pending badge, order history, countdown/pickup/payment/action theo trạng thái; shop refund approve/reject/QR/transfer/proof. |
| Trust | URL :tab?; customer overview/levels/history/penalties và recovery; admin level/rule dialogs, history drawer, status/info/requirements/benefits; list filters. |
| Analytics | URL tab và period/date range, range validation, refresh; overview/charts/comparison/breakdowns từ dashboard DTO. |
| Platform fees | Config/scheduled configs, statements/filter, generate/process overdue, reconciliation/detail/payment modal và payment link. |
| Moderation/admin lists | Search/filter/pagination tùy module; category forms/status/delete; user status/detail/history; shop/product content moderation, warnings; review replies và feedback remove/restore; audit-log detail modal. |

Các mục trong bảng là tổng hợp theo nhóm; endpoint và field đọc của từng route nằm ở R1–R75. Đây là audit tĩnh, không thay đổi validation, auth, API hoặc business logic.
