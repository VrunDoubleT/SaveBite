// Generated from real service response DTOs. Regenerate with npm run mock:generate.
import { person, future } from "./values";
import type { ApiResponse } from "@/shared/types";
import type { CurrentUserResponse, TokenPair } from "@/features/auth/types/auth.types";
interface ApiMessageResponse {
  success: boolean;
  message?: string;
}
export const response0 = {
  success: true,
  message: "Tiêu chuẩn",
} satisfies ApiMessageResponse;
export const response1 = {
  data: {
    accessToken: "Tiêu chuẩn",
    accessTokenExpiresAt: future,
    refreshToken: "Tiêu chuẩn",
    refreshTokenExpiresAt: future,
  },
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<TokenPair>;
export const response2 = {
  data: {
    id: "00000000-0000-4000-8000-000000000001",
    email: "minh1@example.test",
    phone: "0901234567",
    fullName: person(0),
    avatarUrl: "/images/deals/banh-mi/bm-overview.jpg",
    role: "user",
    customerStatus: "active",
    defaultAddress: {
      id: "00000000-0000-4000-8000-000000000001",
      label: "Đơn hàng mới",
      addressLine: "12 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
      ward: "Bến Nghé",
      district: "Quận 1",
      city: "TP. Hồ Chí Minh",
      latitude: 10.7769,
      longitude: 106.7009,
      isDefault: true,
    },
  },
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<CurrentUserResponse>;
export const response3 = {
  success: true,
  message: "Tiêu chuẩn",
} satisfies ApiMessageResponse;
export const response4 = {
  success: true,
  message: "Tiêu chuẩn",
} satisfies ApiMessageResponse;
export const response5 = {
  success: true,
  message: "Tiêu chuẩn",
} satisfies ApiMessageResponse;
export const response6 = {
  success: true,
  message: "Tiêu chuẩn",
} satisfies ApiMessageResponse;
export const response7 = {
  success: true,
  message: "Tiêu chuẩn",
} satisfies ApiMessageResponse;
export default {
  "auth-authApi_changePassword_0": response0,
  "auth-authApi_login_1": response1,
  "auth-authApi_getMe_2": response2,
  "auth-authApi_logout_3": response3,
  "auth-authApi_register_4": response4,
  "auth-authApi_verifyRegistration_5": response5,
  "auth-authApi_forgotPassword_6": response6,
  "auth-authApi_resetPassword_7": response7,
};
