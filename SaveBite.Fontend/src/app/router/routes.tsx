import { AdminPlatformFeeDetailPage } from "@/pages/admin/AdminPlatformFeeDetailPage";
import { CommissionFeeSchedulesPage } from "@/pages/admin/CommissionFeeSchedulesPage";
import { RestockPage } from "@/pages/shop/RestockPage";
import { FollowedShopsPage } from "@/pages/customer/FollowedShopsPage";
import { CustomerRefundDetailPage } from "@/pages/customer/RefundDetailPage";
import { ShopRefundDetailPage } from "@/pages/store-owner/RefundDetailPage";
import { ShopRefundsPage } from "@/pages/store-owner/RefundsPage";
import { RefundsPage } from "@/pages/customer/RefundsPage";
import { PaymentsPage } from "@/pages/customer/PaymentsPage";
import { PaymentDetailPage } from "@/pages/customer/PaymentDetailPage";
import { Navigate, type RouteObject } from "react-router-dom";

import { APP_PATHS } from "@/app/router/paths";
import { ProtectedRoute } from "@/app/router/ProtectedRoute";
import { CustomerRoute } from "@/app/router/CustomerRoute";
import {
  ADMIN_ROLES,
  CUSTOMER_ROLES,
  STAFF_ROLES,
  STORE_OWNER_ROLES,
} from "@/app/router/routeAccess";

import { AdminLayout } from "@/shared/layouts/AdminLayout";
import { CustomerLayout } from "@/shared/layouts/CustomerLayout";
import { StaffLayout } from "@/shared/layouts/StaffLayout";
import { StoreOwnerLayout } from "@/shared/layouts/StoreOwnerLayout";

import { ForgotPasswordPage } from "@/pages/auth/ForgotPasswordPage";
import { LoginPage } from "@/pages/auth/LoginPage";
import { OtpVerificationPage } from "@/pages/auth/OtpVerificationPage";
import { RegisterPage } from "@/pages/auth/RegisterPage";

import { CartPage } from "@/pages/customer/CartPage";
import { CheckoutSimulationPage } from "@/pages/customer/CheckoutSimulationPage";
import { CheckoutPage } from "@/pages/customer/CheckoutPage";
import { CustomerHomePage } from "@/pages/customer/CustomerHomePage";
import { AccountLayout } from "@/shared/layouts/AccountLayout";
import { CustomerStaffInvitationsPage } from "@/pages/customer/StaffInvitationsPage";
import { StoreOwnerStaffInvitationsPage } from "@/pages/store-owner/StaffInvitationsPage";

import { WorkspacePage } from "@/pages/customer/WorkspacePage";
import { ShopApplicationFormPage } from "@/pages/customer/ShopApplicationFormPage";
import ShopApplicationPage from "@/pages/customer/ShopApplicationPage";
import { TrustScorePage } from "@/pages/customer/TrustScorePage";
import { ReviewsPage } from "@/pages/customer/ReviewsPage";
import { OrderRefundRequestPage } from "@/pages/customer/OrderRefundRequestPage";
import { OrderDetailPage } from "@/pages/customer/OrderDetailPage";
import { OrdersPage } from "@/pages/customer/OrdersPage";
import { ProfilePage } from "@/pages/customer/ProfilePage";

import { AdminDashboardPage } from "@/pages/admin/AdminDashboardPage";
import { AdminAuditLogsPage } from "@/pages/admin/AdminAuditLogsPage";
import { CategoryManagementPage } from "@/pages/admin/CategoryManagementPage";
import { UserManagementPage } from "@/pages/admin/UserManagementPage";
import { TrustConfigurationPage } from "@/pages/admin/TrustConfigurationPage";
import { AdminShopApplicationsPage } from "@/pages/admin/AdminShopApplicationsPage";

import { StoreOwnerDashboardPage } from "@/pages/store-owner/StoreOwnerDashboardPage";
import { StaffManagementPage } from "@/pages/store-owner/StaffManagementPage";
import { StaffDetailPage } from "@/pages/store-owner/StaffDetailPage";
import { PaymentSettingsPage } from "@/pages/store-owner/PaymentSettingsPage";
import { ShopProfilePage } from "@/pages/store-owner/ShopProfilePage";
import { StoreOwnerFlashDealsPage } from "@/pages/store-owner/StoreOwnerFlashDealsPage";
import { StoreOwnerFlashDealDetailPage } from "@/pages/store-owner/StoreOwnerFlashDealDetailPage";
import { StoreOwnerFlashDealFormPage } from "@/pages/store-owner/StoreOwnerFlashDealFormPage";

import { StaffShopDetailPage } from "@/pages/staff/StaffShopDetailPage";
import { StaffFlashDealsPage } from "@/pages/staff/StaffFlashDealsPage";
import { StaffFlashDealDetailPage } from "@/pages/staff/StaffFlashDealDetailPage";

import { CustomerShopPage } from "@/pages/customer/CustomerShopPage";
import { CustomerDealDetailPage } from "@/pages/customer/CustomerDealDetailPage";
import { ComingSoonPage } from "@/pages/commons/ComingSoonPage";
import { NotFoundPage } from "@/pages/commons/NotFoundPage";
import { StoreOwnerReviewsPage } from "@/pages/store-owner/StoreOwnerReviewsPage";
import { AdminReviewsPage } from "@/pages/admin/AdminReviewsPage";

import { ShopOrdersPage } from "@/pages/shop/ShopOrdersPage";
import { ShopOrderDetailPage } from "@/pages/shop/ShopOrderDetailPage";
import { PlatformFeeManagementPage } from "@/pages/admin/PlatformFeeManagementPage";
import { AdminPlatformPayOsConfigPage } from "@/pages/admin/AdminPlatformPayOsConfigPage";
import { PlatformFeesPage } from "@/pages/store-owner/PlatformFeesPage";
import { PlatformFeeDetailPage } from "@/pages/store-owner/PlatformFeeDetailPage";
import { ShopManagementPage } from "@/pages/admin/ShopManagementPage";
import { ShopContentManagementPage } from "@/pages/admin/ShopContentManagementPage";
import { ProductManagementPage } from "@/pages/admin/ProductManagementPage";
import { ProductContentManagementPage } from "@/pages/admin/ProductContentManagementPage";
import { WarningHistoryPage } from "@/features/warnings/pages/WarningHistoryPage";
import { FeedbackManagementPage } from "@/pages/admin/FeedbackManagementPage";
import { ProductsWorkspace } from "@/features/products/pages/ProductsWorkspace";
import { ProductListPage } from "@/features/products/pages/ProductListPage";
import { CreateProductPage } from "@/features/products/pages/CreateProductPage";
import { ProductDetailPage } from "@/features/products/pages/ProductDetailPage";
import { ProductHistoryPage } from "@/features/products/pages/ProductHistoryPage";
import { UiShowcasePage } from "@/pages/dev/UiShowcasePage";

export const routes: RouteObject[] = [
  ...(import.meta.env.DEV && import.meta.env.VITE_ENABLE_UI_SHOWCASE === "true"
    ? [
        {
          path: APP_PATHS.UI_SHOWCASE,
          element: <UiShowcasePage />,
        },
      ]
    : []),

  // Customer & public
  {
    element: <CustomerRoute />,
    children: [
      {
        element: <CustomerLayout />,
        children: [
          {
            path: APP_PATHS.HOME,
            element: <CustomerHomePage />,
          },
          {
            path: APP_PATHS.SHOP_DETAIL,
            element: <CustomerShopPage />,
          },
          {
            path: APP_PATHS.SHOP_PROFILE,
            element: <CustomerShopPage />,
          },
          {
            path: APP_PATHS.FLASH_DEAL_DETAIL,
            element: <CustomerDealDetailPage />,
          },
          {
            path: APP_PATHS.DEAL_DETAIL_LEGACY,
            element: <CustomerDealDetailPage />,
          },

          // Authentication
          {
            path: APP_PATHS.LOGIN,
            element: <LoginPage />,
          },
          {
            path: APP_PATHS.REGISTER,
            element: <RegisterPage />,
          },
          {
            path: APP_PATHS.REGISTER_VERIFY,
            element: <OtpVerificationPage purpose="registration" />,
          },
          {
            path: APP_PATHS.FORGOT_PASSWORD,
            element: <ForgotPasswordPage />,
          },
          {
            path: APP_PATHS.RESET_PASSWORD_VERIFY,
            element: <OtpVerificationPage purpose="password-reset" />,
          },

          // Customer Role
          {
            element: <ProtectedRoute allowedRoles={CUSTOMER_ROLES} />,
            children: [
              {
                path: APP_PATHS.CUSTOMER_LEGACY,
                element: <Navigate to={APP_PATHS.HOME} replace />,
              },
              {
                path: APP_PATHS.CART,
                element: <CartPage />,
              },
              {
                path: APP_PATHS.CHECKOUT_SIMULATION,
                element: <CheckoutSimulationPage />,
              },
              {
                path: APP_PATHS.ORDER_REFUND_REQUEST,
                element: <OrderRefundRequestPage />,
              },
              {
                path: APP_PATHS.ORDER_DETAIL,
                element: <OrderDetailPage />,
              },
              {
                path: APP_PATHS.CHECKOUT,
                element: <CheckoutPage />,
              },
              {
                path: APP_PATHS.ACCOUNT,
                element: <AccountLayout />,
                children: [
                  {
                    index: true,
                    element: <Navigate to={APP_PATHS.ACCOUNT_PROFILE} replace />,
                  },
                  {
                    path: APP_PATHS.ACCOUNT_PROFILE,
                    element: <ProfilePage />,
                  },
                  {
                    path: APP_PATHS.ACCOUNT_ORDERS,
                    element: <OrdersPage />,
                  },
                  {
                    path: APP_PATHS.ACCOUNT_PAYMENTS,
                    element: <PaymentsPage />,
                  },
                  {
                    path: APP_PATHS.ACCOUNT_REFUNDS,
                    element: <RefundsPage />,
                  },
                  {
                    path: APP_PATHS.REFUND_DETAIL,
                    element: <CustomerRefundDetailPage />,
                  },
                  {
                    path: APP_PATHS.ACCOUNT_PAYMENT_DETAIL,
                    element: <PaymentDetailPage />,
                  },
                  {
                    path: APP_PATHS.ACCOUNT_FOLLOWED_SHOPS,
                    element: <FollowedShopsPage />,
                  },
                  {
                    path: APP_PATHS.ACCOUNT_REVIEWS,
                    element: <ReviewsPage />,
                  },
                  {
                    path: APP_PATHS.ACCOUNT_TRUST_SCORE_TAB,
                    element: <TrustScorePage />,
                  },
                  {
                    path: APP_PATHS.ACCOUNT_SHOP_APPLICATION,
                    element: <ShopApplicationPage />,
                  },
                  {
                    path: APP_PATHS.ACCOUNT_SHOP_APPLICATION_NEW,
                    element: <ShopApplicationFormPage />,
                  },
                  {
                    path: APP_PATHS.ACCOUNT_SHOP_APPLICATION_EDIT,
                    element: <ShopApplicationFormPage />,
                  },
                  {
                    path: APP_PATHS.ACCOUNT_WORKSPACE,
                    element: <WorkspacePage />,
                  },
                  {
                    path: APP_PATHS.ACCOUNT_STAFF_INVITATIONS,
                    element: <CustomerStaffInvitationsPage />,
                  },
                ],
              },
            ],
          },
        ],
      },
    ],
  },

  // Admin
  {
    element: <ProtectedRoute allowedRoles={ADMIN_ROLES} />,
    children: [
      {
        path: APP_PATHS.ADMIN,
        element: <AdminLayout />,
        children: [
          {
            index: true,
            element: <Navigate to={APP_PATHS.ADMIN_ANALYTICS_OVERVIEW} replace />,
          },
          {
            path: APP_PATHS.ADMIN_ANALYTICS_TAB,
            element: <AdminDashboardPage />,
          },
          {
            path: APP_PATHS.ADMIN_USERS,
            element: <UserManagementPage />,
          },
          {
            path: APP_PATHS.ADMIN_CATEGORIES,
            element: <CategoryManagementPage />,
          },
          {
            path: APP_PATHS.ADMIN_TRUST_TAB,
            element: <TrustConfigurationPage />,
          },
          {
            path: APP_PATHS.ADMIN_PRODUCTS,
            element: <ProductManagementPage />,
          },
          {
            path: APP_PATHS.ADMIN_PRODUCT_CONTENT,
            element: <ProductContentManagementPage />,
          },
          {
            path: APP_PATHS.ADMIN_ORDERS,
            element: (
              <ComingSoonPage
                title="Orders"
                description="The order management module is ready to connect to the API."
                backTo={APP_PATHS.ADMIN}
              />
            ),
          },
          {
            path: APP_PATHS.ADMIN_STORES,
            element: <ShopManagementPage />,
          },
          {
            path: APP_PATHS.ADMIN_SHOP_CONTENT,
            element: <ShopContentManagementPage />,
          },
          {
            path: APP_PATHS.ADMIN_SHOP_APPLICATIONS,
            element: <AdminShopApplicationsPage />,
          },
          {
            path: APP_PATHS.ADMIN_COMMISSION_FEE_SCHEDULES,
            element: <CommissionFeeSchedulesPage />,
          },
          {
            path: APP_PATHS.ADMIN_PLATFORM_FEE_DETAIL,
            element: <AdminPlatformFeeDetailPage />,
          },
          {
            path: APP_PATHS.ADMIN_PLATFORM_FEE,
            element: <PlatformFeeManagementPage />,
          },
          {
            path: APP_PATHS.ADMIN_PLATFORM_PAYOS_CONFIG,
            element: <AdminPlatformPayOsConfigPage />,
          },
          {
            path: APP_PATHS.ADMIN_FEEDBACK,
            element: <FeedbackManagementPage />,
          },
          {
            path: APP_PATHS.ADMIN_REVIEWS,
            element: <AdminReviewsPage />,
          },
          {
            path: APP_PATHS.ADMIN_AUDIT_LOGS,
            element: <AdminAuditLogsPage />,
          },
        ],
      },
    ],
  },

  // Staff
  {
    element: <ProtectedRoute allowedRoles={STAFF_ROLES} />,
    children: [
      {
        path: APP_PATHS.STAFF,
        element: <Navigate to={APP_PATHS.HOME} replace />,
      },
      {
        path: APP_PATHS.STAFF_SHOP,
        element: <StaffLayout />,
        children: [
          {
            index: true,
            element: <Navigate to={APP_PATHS.STAFF_SHOP_INDEX_REDIRECT} replace />,
          },
          {
            path: APP_PATHS.STAFF_FLASH_DEALS,
            element: <StaffFlashDealsPage />,
          },
          {
            path: APP_PATHS.STAFF_FLASH_DEAL_DETAIL,
            element: <StaffFlashDealDetailPage />,
          },
          {
            path: APP_PATHS.STAFF_REVIEWS,
            element: <StoreOwnerReviewsPage />,
          },
          {
            path: APP_PATHS.STAFF_RESTOCK,
            element: <RestockPage />,
          },
          {
            path: APP_PATHS.STAFF_ORDERS,
            element: <ShopOrdersPage />,
          },
          {
            path: APP_PATHS.STAFF_ORDER_DETAIL,
            element: <ShopOrderDetailPage />,
          },
          {
            path: APP_PATHS.STAFF_SHOP_INFO,
            element: <StaffShopDetailPage />,
          },
        ],
      },
    ],
  },

  // Store owner
  {
    element: <ProtectedRoute allowedRoles={STORE_OWNER_ROLES} />,
    children: [
      {
        path: APP_PATHS.STORE_OWNER,
        element: <StoreOwnerLayout />,
        children: [
          {
            index: true,
            element: <StoreOwnerDashboardPage />,
          },
          {
            path: APP_PATHS.STORE_OWNER_PRODUCTS,
            element: <ProductsWorkspace />,
            children: [
              {
                index: true,
                element: <ProductListPage />,
              },
              {
                path: APP_PATHS.STORE_OWNER_PRODUCT_CREATE,
                element: <CreateProductPage />,
              },
              {
                path: APP_PATHS.STORE_OWNER_PRODUCT_DETAIL,
                element: <ProductDetailPage />,
              },
              {
                path: APP_PATHS.STORE_OWNER_PRODUCT_FLASH_DEALS,
                element: <ProductHistoryPage />,
              },
            ],
          },
          {
            path: APP_PATHS.STORE_OWNER_RESTOCK,
            element: <RestockPage />,
          },
          {
            path: APP_PATHS.STORE_OWNER_ORDERS,
            element: <ShopOrdersPage />,
          },
          {
            path: APP_PATHS.STORE_OWNER_ORDER_DETAIL,
            element: <ShopOrderDetailPage />,
          },
          {
            path: APP_PATHS.STORE_OWNER_REFUNDS,
            element: <ShopRefundsPage />,
          },
          {
            path: APP_PATHS.STORE_OWNER_REFUND_DETAIL,
            element: <ShopRefundDetailPage />,
          },
          {
            path: APP_PATHS.STORE_OWNER_FLASH_DEALS,
            element: <StoreOwnerFlashDealsPage />,
          },
          {
            path: APP_PATHS.STORE_OWNER_FLASH_DEAL_CREATE,
            element: <StoreOwnerFlashDealFormPage />,
          },
          {
            path: APP_PATHS.STORE_OWNER_FLASH_DEAL_DETAIL,
            element: <StoreOwnerFlashDealDetailPage />,
          },
          {
            path: APP_PATHS.STORE_OWNER_FLASH_DEAL_EDIT,
            element: <StoreOwnerFlashDealFormPage />,
          },
          {
            path: APP_PATHS.STORE_OWNER_STAFF,
            element: <StaffManagementPage />,
          },
          {
            path: APP_PATHS.STORE_OWNER_STAFF_INVITATIONS,
            element: <StoreOwnerStaffInvitationsPage />,
          },
          {
            path: APP_PATHS.STORE_OWNER_STAFF_DETAIL,
            element: <StaffDetailPage />,
          },
          {
            path: APP_PATHS.STORE_OWNER_PAYMENT_SETTINGS,
            element: <PaymentSettingsPage />,
          },
          {
            path: APP_PATHS.STORE_OWNER_PLATFORM_FEES,
            element: <PlatformFeesPage />,
          },
          {
            path: APP_PATHS.STORE_OWNER_PLATFORM_FEE_DETAIL,
            element: <PlatformFeeDetailPage />,
          },
          {
            path: APP_PATHS.STORE_OWNER_PROFILE,
            element: <ShopProfilePage />,
          },
          {
            path: APP_PATHS.STORE_OWNER_WARNINGS,
            element: <WarningHistoryPage />,
          },
          {
            path: APP_PATHS.STORE_OWNER_REVIEWS,
            element: <StoreOwnerReviewsPage />,
          },
        ],
      },
    ],
  },

  // Fallback
  {
    path: APP_PATHS.NOT_FOUND,
    element: <NotFoundPage />,
  },
];
