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
  STAFF:{
    INVITATIONS: "/staff/invitations",
    ACCEPT_INVITATION:
      (id:string)=>
      `/staff/invitations/${id}/accept`,

    DECLINE_INVITATION:
      (id:string)=>
      `/staff/invitations/${id}/decline`,

    SHOPS:"/staff/shops",

    SHOP_DETAIL:
      (shopId:string)=>
      `/staff/shops/${shopId}`
    },
   OWNER_SHOPS:{
    ME: "/owner/shops/me",


   INVITATIONS: (shopId: string) =>
    `/owner/shops/${shopId}/invitations`,

  INVITATION_DETAIL: (
    shopId: string,
    invitationId: string,
  ) =>
    `/owner/shops/${shopId}/invitations/${invitationId}`,

     STAFFS:
      (shopId:string)=>
      `/owner/shops/${shopId}/staffs`,

      STAFF_DETAIL:
      (
        shopId:string,
        staffId:string
      )=>
      `/owner/shops/${shopId}/staffs/${staffId}`,

     ACTIVITY_LOGS:
      (
        shopId:string,
        staffId:string
      )=>
      `/owner/shops/${shopId}/staffs/${staffId}/activity-logs`

  

  },
} as const;
