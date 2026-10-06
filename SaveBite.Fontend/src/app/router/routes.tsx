import { Navigate, type RouteObject } from "react-router-dom";
import { APP_PATHS } from "@/app/router/paths";
import { ProtectedRoute } from "@/app/router/ProtectedRoute";
import { CustomerRoute } from "@/app/router/CustomerRoute";
import {
  ADMIN_ROLES,
  CUSTOMER_ROLES,
  STAFF_ROLES,
} from "@/app/router/routeAccess";

import { AdminLayout } from "@/shared/layout/AdminLayout";
import { CustomerLayout } from "@/shared/layout/CustomerLayout";
import { StaffLayout } from "@/shared/layout/StaffLayout";
import { StoreOwnerLayout } from "@/shared/layout/StoreOwnerLayout";
import { AccountLayout } from "@/shared/layout/AccountLayout";

import { ForgotPasswordPage } from "@/pages/auth/ForgotPasswordPage";
import { LoginPage } from "@/pages/auth/LoginPage";
import { OtpVerificationPage } from "@/pages/auth/OtpVerificationPage";
import { RegisterPage } from "@/pages/auth/RegisterPage";

import { CustomerHomePage } from "@/pages/customer/CustomerHomePage";

import { ComingSoonPage } from "@/pages/commons/ComingSoonPage";
import { NotFoundPage } from "@/pages/commons/NotFoundPage";

import { StaffManagementPage } from "@/pages/store-owner/StaffManagementPage";
import { StaffDetailPage } from "@/pages/store-owner/StaffDetailPage";

import { StaffShopDetailPage } from "@/pages/staff/StaffShopDetailPage";
import {
  StaffInvitationsPage as CustomerStaffInvitationsPage,
} from "@/pages/customer/StaffInvitationsPage";
import {
  StaffInvitationsPage as OwnerStaffInvitationsPage,
} from "@/pages/store-owner/StaffInvitationsPage";
import { WorkspacePage } from "@/pages/customer/WorkspacePage";

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
                element: (
                  <ComingSoonPage
                    title="Cart"
                    description="Your cart page is ready for checkout integration."
                  />
                ),
              },
              {
                path: APP_PATHS.ACCOUNT,
                element: <AccountLayout />,
                children: [
                  {
                    index: true,
                    element: (
                      <Navigate
                        to={APP_PATHS.ACCOUNT_PROFILE}
                        replace
                      />
                    ),
                  },
                  {
                    path: "profile",
                  },
                  {
                    path: "orders",
                  },
                  {
                    path: "reviews",
                  },
                  {
                    path: "trust-scores",
                  },
                  {
                    path: "shop-registration",
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
        ],
      },
    ],
  },

  {
  element: <ProtectedRoute allowedRoles={STAFF_ROLES} />,
  children: [
    {
      path: APP_PATHS.STAFF,
      element: (
        <Navigate
          to={APP_PATHS.ACCOUNT_WORKSPACE}
          replace
        />
      ),
    },
    {
      path: `${APP_PATHS.STAFF}/shops/:shopId`,
      element: <StaffLayout />,
      children: [
        {
          index: true,
          element: (
            <ComingSoonPage
              title="Staff overview"
              description="Staff operations will appear here when their APIs are ready."
              backTo={APP_PATHS.ACCOUNT_WORKSPACE}
            />
          ),
        },
        {
          path: "info",
          element: <StaffShopDetailPage />,
        },
      ],
    },
  ],
},
  {
    path: APP_PATHS.STORE_OWNER,
    element: <StoreOwnerLayout />,
    children: [
      {
        index: true,
        element: (
          <ComingSoonPage
            title="Store owner overview"
            description="Store management data will appear here when its APIs are ready."
            backTo={APP_PATHS.STORE_OWNER}
          />
        ),
      },
      {
        path: "staff",
        element: <StaffManagementPage />,
      },
     {
  path: APP_PATHS.STORE_OWNER_STAFF_INVITATIONS,
  element: <OwnerStaffInvitationsPage />,
},
      {
        path: APP_PATHS.STORE_OWNER_STAFF_DETAIL,
        element: <StaffDetailPage />,
      },
    ],
  },

  {
    path: APP_PATHS.NOT_FOUND,
    element: <NotFoundPage />,
  },
  
];