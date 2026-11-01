// Generated from real service response DTOs. Regenerate with npm run mock:generate.
import { person, past } from "./values";
import type { ApiResponse } from "@/shared/types";
import type { AdminUser, UserDetailsResponse } from "@/features/users/types/user.types";
interface BackendPagedUser {
  items: AdminUser[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}
export const response0 = {
  data: {
    items: [
      {
        id: "00000000-0000-4000-8000-000000000001",
        email: "minh1@example.test",
        fullName: person(0),
        role: "user",
        status: "Active",
        customerStatus: "Active",
        createdAt: past,
      },
      {
        id: "00000000-0000-4000-8000-000000000002",
        email: "minh2@example.test",
        fullName: person(1),
        role: "user",
        status: "Active",
        customerStatus: "Active",
        createdAt: past,
      },
      {
        id: "00000000-0000-4000-8000-000000000003",
        email: "minh3@example.test",
        fullName: person(2),
        role: "user",
        status: "Active",
        customerStatus: "Active",
        createdAt: past,
      },
    ],
    page: 1,
    pageSize: 10,
    totalItems: 1,
    totalPages: 1,
  },
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<BackendPagedUser>;
export const response1 = {
  data: {
    id: "00000000-0000-4000-8000-000000000001",
    email: "minh1@example.test",
    phone: "0901234567",
    fullName: person(0),
    avatarUrl: "/images/deals/banh-mi/bm-overview.jpg",
    role: "user",
    status: "Active",
    customerStatus: "Active",
    shopStatus: "Active",
    createdAt: past,
    updatedAt: past,
    logs: [
      {
        action: "Tiêu chuẩn",
        target: "Account",
        reason: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
        changedValues: {},
        createdAt: past,
        adminId: "00000000-0000-4000-8000-000000000001",
      },
      {
        action: "Cỡ lớn",
        target: "Customer",
        reason: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
        changedValues: {},
        createdAt: past,
        adminId: "00000000-0000-4000-8000-000000000002",
      },
    ],
    roleLogs: [
      {
        oldRole: "user",
        newRole: "user",
        reason: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
        createdAt: past,
      },
      {
        oldRole: "user",
        newRole: "user",
        reason: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
        createdAt: past,
      },
    ],
  },
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<UserDetailsResponse>;
export const response2 = {
  data: "Tiêu chuẩn",
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<string>;
export default {
  "users-userApi_getUsers_0": response0,
  "users-userApi_getUserDetails_1": response1,
  "users-userApi_updateStatus_2": response2,
};
