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
import { OrderDetailPage } from "@/pages/customer/OrderDetailPage";
import { OrdersPage } from "@/pages/customer/OrdersPage";
import { ProfilePage } from "@/pages/customer/ProfilePage";

import { CategoryManagementPage } from "@/pages/admin/CategoryManagementPage";
import { UserManagementPage } from "@/pages/admin/UserManagementPage";
import { TrustConfigurationPage } from "@/pages/admin/TrustConfigurationPage";

import { StaffManagementPage } from "@/pages/store-owner/StaffManagementPage";
import { StaffDetailPage } from "@/pages/store-owner/StaffDetailPage";
import { StaffShopDetailPage } from "@/pages/staff/StaffShopDetailPage";
import { StaffFlashDealsPage } from "@/pages/staff/StaffFlashDealsPage";
import { StaffFlashDealDetailPage } from "@/pages/staff/StaffFlashDealDetailPage";

import { CustomerShopPage } from "@/pages/customer/CustomerShopPage";
import { CustomerDealDetailPage } from "@/pages/customer/CustomerDealDetailPage";
import { ComingSoonPage } from "@/pages/commons/ComingSoonPage";
import { NotFoundPage } from "@/pages/commons/NotFoundPage";
import { AdminShopApplicationsPage } from "@/pages/admin/AdminShopApplicationsPage";
import { PaymentSettingsPage } from "@/pages/store-owner/PaymentSettingsPage";
import { ShopProfilePage } from "@/pages/store-owner/ShopProfilePage";

import { ShopOrdersPage } from "@/pages/shop/ShopOrdersPage";
import { ShopOrderDetailPage } from "@/pages/shop/ShopOrderDetailPage";
import { StoreOwnerFlashDealsPage } from "@/pages/store-owner/StoreOwnerFlashDealsPage";
import { StoreOwnerFlashDealDetailPage } from "@/pages/store-owner/StoreOwnerFlashDealDetailPage";
import { StoreOwnerFlashDealFormPage } from "@/pages/store-owner/StoreOwnerFlashDealFormPage";

export const routes: RouteObject[] = [
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
            path: "/deals/:id",
            element: <CustomerDealDetailPage />,
          },
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
                path: "/checkout/simulation",
                element: <CheckoutSimulationPage />,
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
                    path: "profile",
                    element: <ProfilePage />,
                  },
                  {
                    path: "orders",
                    element: <OrdersPage />,
                  },
                  { path: "payments", element: <PaymentsPage /> },
                  { path: "refunds", element: <RefundsPage /> },
                  { path: "refunds/:refundId", element: <CustomerRefundDetailPage /> },
                  { path: "payments/:paymentId", element: <PaymentDetailPage /> },
                  {
                    path: "reviews",
                    element: <ReviewsPage />,
                  },
                  {
                    path: "trust-score/:tab?",
                    element: <TrustScorePage />,
                  },
                  {
                    path: "shop-application",
                    element: <ShopApplicationPage />,
                  },
                  {
                    path: "shop-application/new",
                    element: <ShopApplicationFormPage />,
                  },
                  {
                    path: "shop-application/edit/:applicationId",
                    element: <ShopApplicationFormPage />,
                  },
                  {
                    path: "workspace",
                    element: <WorkspacePage />,
                  },
                  {
                    path: "staff-invitations",
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

  {
    element: <ProtectedRoute allowedRoles={ADMIN_ROLES} />,
    children: [
      {
        path: APP_PATHS.ADMIN,
        element: <AdminLayout />,
        children: [
          {
            index: true,
            element: (
              <ComingSoonPage
                title="Admin overview"
                description="The admin dashboard is ready for API integration."
                backTo={APP_PATHS.ADMIN}
              />
            ),
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
            path: `${APP_PATHS.ADMIN_TRUST}/:tab?`,
            element: <TrustConfigurationPage />,
          },
          {
            path: APP_PATHS.ADMIN_PRODUCTS,
            element: (
              <ComingSoonPage
                title="Products"
                description="Product management will appear here when its data source is ready."
                backTo={APP_PATHS.ADMIN}
              />
            ),
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
            element: (
              <ComingSoonPage
                title="Stores"
                description="The store management module is being prepared."
                backTo={APP_PATHS.ADMIN}
              />
            ),
          },
          {
            path: APP_PATHS.ADMIN_SHOP_APPLICATIONS,
            element: <AdminShopApplicationsPage />,
          },
        ],
      },
    ],
  },

  {
    element: <ProtectedRoute allowedRoles={STAFF_ROLES} />,
    children: [
      {
        path: APP_PATHS.STAFF,
        element: <Navigate to={APP_PATHS.HOME} replace />,
      },
      {
        path: `${APP_PATHS.STAFF}/shops/:shopId`,
        element: <StaffLayout />,
        children: [
          {
            index: true,
            element: <Navigate to="flash-deals" replace />,
          },
          {
            path: "flash-deals",
            element: <StaffFlashDealsPage />,
          },
          {
            path: "flash-deals/:id",
            element: <StaffFlashDealDetailPage />,
          },
          { path: "orders", element: <ShopOrdersPage /> },
          { path: "orders/:orderId", element: <ShopOrderDetailPage /> },
          {
            path: "info",
            element: <StaffShopDetailPage />,
          },
        ],
      },
    ],
  },

  {
    element: <ProtectedRoute allowedRoles={STORE_OWNER_ROLES} />,
    children: [
      {
        path: APP_PATHS.STORE_OWNER,
        element: <StoreOwnerLayout />,
        children: [
          {
            index: true,
            element: <Navigate to={APP_PATHS.STORE_OWNER_PRODUCTS} replace />,
          },
          {
            path: "products",
            lazy: async () => ({
              Component: (await import("@/features/products/pages/ProductsWorkspace"))
                .ProductsWorkspace,
            }),
            children: [
              {
                index: true,
                lazy: async () => ({
                  Component: (await import("@/features/products/pages/ProductListPage"))
                    .ProductListPage,
                }),
              },
              {
                path: "new",
                lazy: async () => ({
                  Component: (await import("@/features/products/pages/CreateProductPage"))
                    .CreateProductPage,
                }),
              },
              {
                path: ":productId",
                lazy: async () => ({
                  Component: (await import("@/features/products/pages/ProductDetailPage"))
                    .ProductDetailPage,
                }),
              },
              {
                path: ":productId/flash-deals",
                lazy: async () => ({
                  Component: (await import("@/features/products/pages/ProductHistoryPage"))
                    .ProductHistoryPage,
                }),
              },
            ],
          },
          { path: "orders", element: <ShopOrdersPage /> },
          { path: "orders/:orderId", element: <ShopOrderDetailPage /> },
          { path: "refunds", element: <ShopRefundsPage /> },
          { path: "refunds/:refundId", element: <ShopRefundDetailPage /> },
          {
            path: "flash-deals",
            element: <StoreOwnerFlashDealsPage />,
          },
          {
            path: "flash-deals/create",
            element: <StoreOwnerFlashDealFormPage />,
          },
          {
            path: "flash-deals/:id",
            element: <StoreOwnerFlashDealDetailPage />,
          },
          {
            path: "flash-deals/:id/edit",
            element: <StoreOwnerFlashDealFormPage />,
          },
          {
            path: "staff",
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
            path: APP_PATHS.STORE_OWNER_PROFILE,
            element: <ShopProfilePage />,
          },
        ],
      },
    ],
  },
  {
    path: APP_PATHS.NOT_FOUND,
    element: <NotFoundPage />,
  },
];
