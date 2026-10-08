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
  FLASH_DEALS: {
    NEARBY: "/flash-deals/nearby",
    BY_SHOP: (shopId: string) => `/flash-deals/shop/${shopId}`,
    DETAIL: (id: string) => `/flash-deals/${id}`,
    CATEGORIES: "/flash-deals/categories",
  },
  CATEGORIES: {
    LIST: "/categories",
  },
  SHOPS: {
    NEARBY: "/shops/nearby",
    PROFILE: (id: string) => `/shops/${id}`,
    REVIEWS: (id: string) => `/shops/${id}/reviews`,
  },
} as const;
