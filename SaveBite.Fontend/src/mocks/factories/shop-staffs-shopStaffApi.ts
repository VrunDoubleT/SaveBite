// Generated from real service response DTOs. Regenerate with npm run mock:generate.
import { person, past, future } from "./values";
import type { ApiResponse, PaginatedResponse } from "@/shared/types";
import type {
  ShopStaff,
  StaffActivityLog,
  StaffInvitation,
  StaffCandidate,
} from "@/features/shop-staffs/types/shopStaff.types";

export const response0 = {
  data: {
    items: [
      {
        id: "00000000-0000-4000-8000-000000000001",
        userId: "00000000-0000-4000-8000-000000000001",
        userEmail: "minh1@example.test",
        displayName: "Bếp Xanh Sài Gòn",
        staffNickname: "Bếp Xanh Sài Gòn",
        status: "Active",
        joinedAt: past,
      },
      {
        id: "00000000-0000-4000-8000-000000000002",
        userId: "00000000-0000-4000-8000-000000000001",
        userEmail: "minh2@example.test",
        displayName: "Tiệm Bánh An Nhiên",
        staffNickname: "Tiệm Bánh An Nhiên",
        status: "Suspended",
        joinedAt: past,
      },
      {
        id: "00000000-0000-4000-8000-000000000003",
        userId: "00000000-0000-4000-8000-000000000001",
        userEmail: "minh3@example.test",
        displayName: "Góc Rau Đà Lạt",
        staffNickname: "Góc Rau Đà Lạt",
        status: "Removed",
        joinedAt: past,
      },
    ],
    page: 1,
    pageSize: 10,
    total: 1,
    totalPages: 1,
  },
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<PaginatedResponse<ShopStaff>>;
export const response1 = {
  data: {
    items: [
      {
        id: "00000000-0000-4000-8000-000000000001",
        actionBy: "Tiêu chuẩn",
        actorName: person(0),
        actorRole: person(0),
        action: "Tiêu chuẩn",
        description: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
        orderId: "00000000-0000-4000-8000-000000000001",
        order_id: "Tiêu chuẩn",
        createdAt: past,
      },
      {
        id: "00000000-0000-4000-8000-000000000002",
        actionBy: "Cỡ lớn",
        actorName: person(1),
        actorRole: person(1),
        action: "Cỡ lớn",
        description: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
        orderId: "00000000-0000-4000-8000-000000000002",
        order_id: "Cỡ lớn",
        createdAt: past,
      },
      {
        id: "00000000-0000-4000-8000-000000000003",
        actionBy: "Không cay",
        action: "Không cay",
        description:
          "Thực phẩm được chuẩn bị trong ngày, đóng gói cẩn thận. Vui lòng nhận trước giờ đóng cửa; sản phẩm có thể chứa đậu phộng và sữa.",
        createdAt: past,
      },
    ],
    page: 1,
    pageSize: 10,
    total: 1,
    totalPages: 1,
  },
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<PaginatedResponse<StaffActivityLog>>;
export const response2 = {
  data: null,
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<null>;
export const response3 = {
  data: null,
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<null>;
export const response4 = {
  data: {
    id: "00000000-0000-4000-8000-000000000001",
    shopId: "00000000-0000-4000-8000-000000000001",
    shopName: "Bếp Xanh Sài Gòn",
    invitedUserId: "00000000-0000-4000-8000-000000000001",
    invitedUserName: person(0),
    invitedUserEmail: "minh1@example.test",
    status: "Active",
    createdAt: past,
    expiresAt: future,
  },
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<StaffInvitation>;
export const response5 = {
  data: {
    items: [
      {
        userId: "00000000-0000-4000-8000-000000000001",
        fullName: person(0),
        email: "minh1@example.test",
      },
      {
        userId: "00000000-0000-4000-8000-000000000001",
        fullName: person(1),
        email: "minh2@example.test",
      },
      {
        userId: "00000000-0000-4000-8000-000000000001",
        fullName: person(2),
        email: "minh3@example.test",
      },
    ],
    page: 1,
    pageSize: 10,
    total: 1,
    totalPages: 1,
  },
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<PaginatedResponse<StaffCandidate>>;
export const response6 = {
  data: {
    items: [
      {
        id: "00000000-0000-4000-8000-000000000001",
        shopId: "00000000-0000-4000-8000-000000000001",
        shopName: "Bếp Xanh Sài Gòn",
        invitedUserId: "00000000-0000-4000-8000-000000000001",
        invitedUserName: person(0),
        invitedUserEmail: "minh1@example.test",
        status: "Active",
        createdAt: past,
        expiresAt: future,
      },
      {
        id: "00000000-0000-4000-8000-000000000002",
        shopId: "00000000-0000-4000-8000-000000000001",
        shopName: "Tiệm Bánh An Nhiên",
        invitedUserId: "00000000-0000-4000-8000-000000000002",
        invitedUserName: person(1),
        invitedUserEmail: "minh2@example.test",
        status: "Active",
        createdAt: past,
        expiresAt: future,
      },
      {
        id: "00000000-0000-4000-8000-000000000003",
        shopId: "00000000-0000-4000-8000-000000000001",
        shopName: "Góc Rau Đà Lạt",
        invitedUserId: "00000000-0000-4000-8000-000000000003",
        invitedUserName: person(2),
        invitedUserEmail: "minh3@example.test",
        status: "Active",
        createdAt: past,
        expiresAt: future,
      },
    ],
    page: 1,
    pageSize: 10,
    total: 1,
    totalPages: 1,
  },
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<PaginatedResponse<StaffInvitation>>;
export const response7 = {
  data: null,
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<null>;
export default {
  "shop-staffs-shopStaffApi_getStaffList_0": response0,
  "shop-staffs-shopStaffApi_getStaffActivityLogs_1": response1,
  "shop-staffs-shopStaffApi_updateStaff_2": response2,
  "shop-staffs-shopStaffApi_removeStaff_3": response3,
  "shop-staffs-shopStaffApi_inviteStaff_4": response4,
  "shop-staffs-shopStaffApi_searchStaffCandidates_5": response5,
  "shop-staffs-shopStaffApi_getInvitations_6": response6,
  "shop-staffs-shopStaffApi_revokeInvitation_7": response7,
};
