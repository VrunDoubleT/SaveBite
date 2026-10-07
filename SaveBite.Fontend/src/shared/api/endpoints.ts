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
  USER_PROFILE: {
    PROFILE: "/profile",
    AVATAR: "/profile/avatar",
    ADDRESSES: "/profile/addresses",
    CREATE_ADDRESS: "/profile/addresses",
    ADDRESS_DEFAULT: (addressId: string) => `/profile/addresses/${addressId}/default`,
    ADDRESS_BY_ID: (addressId: string) => `/profile/addresses/${addressId}`,
  },
} as const;
