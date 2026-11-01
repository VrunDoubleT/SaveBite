// Generated from real service response DTOs. Regenerate with npm run mock:generate.
import { person, past, future } from "./values";
import type { ApiResponse } from "@/shared/types";
import type { StaffInvitation } from "@/features/staff-invitations/types/staffInvitation.types";

export const response0 = {
  data: [
    {
      id: "00000000-0000-4000-8000-000000000001",
      shopId: "00000000-0000-4000-8000-000000000001",
      shopName: "Bếp Xanh Sài Gòn",
      shopLogoUrl: "/images/deals/banh-mi/bm-overview.jpg",
      shopCoverImageUrl: "/images/deals/banh-mi/bm-overview.jpg",
      shopAddress: "12 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
      invitedUserId: "00000000-0000-4000-8000-000000000001",
      invitedUserName: person(0),
      invitedUserEmail: "minh1@example.test",
      status: "Pending",
      createdAt: past,
      expiresAt: future,
    },
    {
      id: "00000000-0000-4000-8000-000000000002",
      shopId: "00000000-0000-4000-8000-000000000001",
      shopName: "Tiệm Bánh An Nhiên",
      shopLogoUrl: "/images/deals/banh-mi/bm-overview.jpg",
      shopCoverImageUrl: "/images/deals/banh-mi/bm-overview.jpg",
      shopAddress: "12 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
      invitedUserId: "00000000-0000-4000-8000-000000000002",
      invitedUserName: person(1),
      invitedUserEmail: "minh2@example.test",
      status: "Cancelled",
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
      status: "Accepted",
      createdAt: past,
      expiresAt: future,
    },
    {
      id: "00000000-0000-4000-8000-000000000004",
      shopId: "00000000-0000-4000-8000-000000000001",
      shopName: "Bếp Xanh Sài Gòn",
      shopLogoUrl: "/images/deals/banh-mi/bm-overview.jpg",
      shopCoverImageUrl: "/images/deals/banh-mi/bm-overview.jpg",
      shopAddress: "12 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
      invitedUserId: "00000000-0000-4000-8000-000000000004",
      invitedUserName: person(3),
      invitedUserEmail: "minh4@example.test",
      status: "Declined",
      createdAt: past,
      expiresAt: future,
    },
    {
      id: "00000000-0000-4000-8000-000000000005",
      shopId: "00000000-0000-4000-8000-000000000001",
      shopName: "Tiệm Bánh An Nhiên",
      shopLogoUrl: "/images/deals/banh-mi/bm-overview.jpg",
      shopCoverImageUrl: "/images/deals/banh-mi/bm-overview.jpg",
      shopAddress: "12 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
      invitedUserId: "00000000-0000-4000-8000-000000000005",
      invitedUserName: person(4),
      invitedUserEmail: "minh5@example.test",
      status: "Expired",
      createdAt: past,
      expiresAt: future,
    },
  ],
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<StaffInvitation[]>;
export const response1 = {
  success: true,
  message: "Thao tác thành công.",
} satisfies unknown;
export const response2 = {
  success: true,
  message: "Thao tác thành công.",
} satisfies unknown;
export default {
  "staff-invitations-staffInvitationApi_getMyInvitations_0": response0,
  "staff-invitations-staffInvitationApi_acceptInvitation_1": response1,
  "staff-invitations-staffInvitationApi_declineInvitation_2": response2,
};
