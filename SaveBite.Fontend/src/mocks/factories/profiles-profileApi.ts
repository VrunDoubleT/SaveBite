// Generated from real service response DTOs. Regenerate with npm run mock:generate.
import { person } from "./values";
import type { ApiResponse } from "@/shared/types";
import type { CurrentUserResponse } from "@/features/auth/types/auth.types";

export const response0 = {
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
export const response1 = {
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
export default {
  "profiles-profileApi_updateProfile_0": response0,
  "profiles-profileApi_uploadAvatar_1": response1,
};
