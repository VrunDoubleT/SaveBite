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
    RESUBMIT: (applicationId: string) => `/shop-applications/${applicationId}/resubmit`,
    CANCEL: (applicationId: string) => `/shop-applications/${applicationId}/cancel`,
  },
} as const;
