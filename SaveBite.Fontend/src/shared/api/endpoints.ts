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
  }
} as const;
