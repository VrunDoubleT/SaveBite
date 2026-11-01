// Generated from real service response DTOs. Regenerate with npm run mock:generate.
import { person, past } from "./values";
import type { ApiResponse } from "@/shared/types";
import type { AssociatedShop, StaffShopDetail } from "@/features/staff-shops/types/staff.types";

export const response0 = {
  data: [
    {
      id: "00000000-0000-4000-8000-000000000001",
      shopId: "00000000-0000-4000-8000-000000000001",
      shopName: "Bếp Xanh Sài Gòn",
      shopLogoUrl: "/images/deals/banh-mi/bm-overview.jpg",
      shopCoverImageUrl: "/images/deals/banh-mi/bm-overview.jpg",
      status: "Active",
      joinedAt: past,
    },
    {
      id: "00000000-0000-4000-8000-000000000002",
      shopId: "00000000-0000-4000-8000-000000000001",
      shopName: "Tiệm Bánh An Nhiên",
      shopLogoUrl: "/images/deals/banh-mi/bm-overview.jpg",
      shopCoverImageUrl: "/images/deals/banh-mi/bm-overview.jpg",
      status: "Suspended",
      joinedAt: past,
    },
    {
      id: "00000000-0000-4000-8000-000000000003",
      shopId: "00000000-0000-4000-8000-000000000001",
      shopName: "Góc Rau Đà Lạt",
      status: "Removed",
      joinedAt: past,
    },
  ],
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<AssociatedShop[]>;
export const response1 = {
  data: {
    shop: {
      id: "00000000-0000-4000-8000-000000000001",
      name: "Bếp Xanh Sài Gòn",
      description: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
      addressLine: "12 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
      ward: "Bến Nghé",
      district: "Quận 1",
      city: "TP. Hồ Chí Minh",
      logoUrl: "/images/deals/banh-mi/bm-overview.jpg",
      coverImageUrl: "/images/deals/banh-mi/bm-overview.jpg",
      openingTime: "07:00:00",
      closingTime: "23:00:00",
      status: "Active",
    },
    staff: {
      id: "00000000-0000-4000-8000-000000000001",
      userId: "00000000-0000-4000-8000-000000000001",
      userEmail: "minh1@example.test",
      displayName: person(0),
      staffNickname: "Bánh mì rau củ",
      status: "Active",
      joinedAt: past,
    },
  },
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<StaffShopDetail>;
export const response2 = {
  success: true,
  message: "Thao tác thành công.",
} satisfies unknown;
export default {
  "staff-shops-staffApi_getAssociatedShops_0": response0,
  "staff-shops-staffApi_getShopDetail_1": response1,
  "staff-shops-staffApi_leaveShop_2": response2,
};
