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
  STAFF: {
    INVITATIONS: "/staff/invitations",
    ACCEPT_INVITATION: (id: string) => `/staff/invitations/${id}/accept`,
    DECLINE_INVITATION: (id: string) => `/staff/invitations/${id}/decline`,
    SHOPS: "/staff/shops",
    SHOP_DETAIL: (shopId: string) => `/staff/shops/${shopId}`,
    LEAVE_SHOP: (shopId: string) => `/staff/shops/${shopId}`,
  },
  OWNER_SHOPS: {
    ME: "/owner/shops",
    STAFF_CANDIDATES: "/owner/shops/staff-candidates",
    INVITATIONS: "/owner/shops/invitations",
    INVITATION_DETAIL: (invitationId: string) => `/owner/shops/invitations/${invitationId}`,
    STAFFS: "/owner/shops/staffs",
    STAFF_DETAIL: (staffId: string) => `/owner/shops/staffs/${staffId}`,
    ACTIVITY_LOGS: (staffId: string) => `/owner/shops/staffs/${staffId}/activity-logs`,
  },
  ADMIN: {
    USERS: "/admin/users",
    CATEGORIES: "/admin/categories"
  },
  SHOP_APPLICATION: {
    BASE: "/shop-applications",
    ME: "/shop-applications/me",
    HISTORY: "/shop-applications/me/history",
    RESUBMIT: (applicationId: string) => `/shop-applications/${applicationId}/resubmit`,
    CANCEL: (applicationId: string) => `/shop-applications/${applicationId}/cancel`,
  },
  USER_PROFILE: {
    PROFILE: "/profile",
    AVATAR: "/profile/avatar",
    ADDRESSES: "/profile/addresses",
    CREATE_ADDRESS: "/profile/addresses",
    ADDRESS_DEFAULT: (addressId: string) => `/profile/addresses/${addressId}/default`,
    ADDRESS_BY_ID: (addressId: string) => `/profile/addresses/${addressId}`,
  ADMIN_SHOP_APPLICATION: {
    BASE: "/admin/shop-applications",
    DETAIL: (applicationId: string) => `/admin/shop-applications/${applicationId}`,
    REVIEW: (applicationId: string) => `/admin/shop-applications/${applicationId}/review`,
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
