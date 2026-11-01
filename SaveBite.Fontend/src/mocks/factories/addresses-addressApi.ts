// Generated from real service response DTOs. Regenerate with npm run mock:generate.
import { past } from "./values";
import type { ApiResponse } from "@/shared/types";
import type { UserAddress } from "@/features/addresses/types/address.types";

export const response0 = {
  data: [
    {
      id: "00000000-0000-4000-8000-000000000001",
      label: "Đơn hàng mới",
      addressLine: "12 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
      ward: "Bến Nghé",
      district: "Quận 1",
      city: "TP. Hồ Chí Minh",
      latitude: 10.7769,
      longitude: 106.7009,
      isDefault: true,
      createdAt: past,
      updatedAt: past,
    },
    {
      id: "00000000-0000-4000-8000-000000000002",
      label: "Đã xác nhận",
      addressLine: "12 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
      ward: "Bến Nghé",
      district: "Quận 1",
      city: "TP. Hồ Chí Minh",
      latitude: 10.777899999999999,
      longitude: 106.70190000000001,
      isDefault: true,
      createdAt: past,
      updatedAt: past,
    },
    {
      id: "00000000-0000-4000-8000-000000000003",
      label: "Cập nhật cửa hàng",
      addressLine: "12 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
      ward: "Bến Nghé",
      district: "Quận 1",
      city: "TP. Hồ Chí Minh",
      latitude: 10.7789,
      longitude: 106.7029,
      isDefault: true,
      createdAt: past,
      updatedAt: past,
    },
  ],
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<UserAddress[]>;
export const response1 = {
  data: {
    id: "00000000-0000-4000-8000-000000000001",
    label: "Đơn hàng mới",
    addressLine: "12 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
    ward: "Bến Nghé",
    district: "Quận 1",
    city: "TP. Hồ Chí Minh",
    latitude: 10.7769,
    longitude: 106.7009,
    isDefault: true,
    createdAt: past,
    updatedAt: past,
  },
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<UserAddress>;
export const response2 = {
  data: {
    id: "00000000-0000-4000-8000-000000000001",
    label: "Đơn hàng mới",
    addressLine: "12 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
    ward: "Bến Nghé",
    district: "Quận 1",
    city: "TP. Hồ Chí Minh",
    latitude: 10.7769,
    longitude: 106.7009,
    isDefault: true,
    createdAt: past,
    updatedAt: past,
  },
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<UserAddress>;
export const response3 = {
  data: {
    id: "00000000-0000-4000-8000-000000000001",
    label: "Đơn hàng mới",
    addressLine: "12 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
    ward: "Bến Nghé",
    district: "Quận 1",
    city: "TP. Hồ Chí Minh",
    latitude: 10.7769,
    longitude: 106.7009,
    isDefault: true,
    createdAt: past,
    updatedAt: past,
  },
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<UserAddress>;
export const response4 = {
  success: true,
  message: "Thao tác thành công.",
} satisfies unknown;
export default {
  "addresses-addressApi_getAddresses_0": response0,
  "addresses-addressApi_createAddress_1": response1,
  "addresses-addressApi_updateAddress_2": response2,
  "addresses-addressApi_setDefaultAddress_3": response3,
  "addresses-addressApi_deleteAddress_4": response4,
};
