export const API_ENDPOINTS = {
  AUTH: {
    LOGIN: "/auth/login",
    LOGOUT: "/auth/logout",
    ME: "/auth/me",
    REFRESH: "/auth/refresh",
    REGISTER: "/auth/register",
    VERIFY_REGISTRATION: "/auth/register/verify",
    FORGOT_PASSWORD: "/auth/forgot-password",
    RESET_PASSWORD: "/auth/reset-password",
  },
  SHOP_APPLICATION: {
    BASE: "/shop-applications",
    ME: "/shop-applications/me",
    HISTORY: "/shop-applications/me/history",
    RESUBMIT: (applicationId: string) => `/shop-applications/${applicationId}/resubmit`,
    CANCEL: (applicationId: string) => `/shop-applications/${applicationId}/cancel`,
  },
  ADMIN_SHOP_APPLICATION: {
    BASE: "/admin/shop-applications",
    DETAIL: (applicationId: string) => `/admin/shop-applications/${applicationId}`,
    REVIEW: (applicationId: string) => `/admin/shop-applications/${applicationId}/review`,
  },
} as const;
