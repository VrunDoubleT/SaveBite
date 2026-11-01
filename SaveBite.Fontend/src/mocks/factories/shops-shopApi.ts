// Generated from real service response DTOs. Regenerate with npm run mock:generate.
import { past } from "./values";
import type { ApiResponse } from "@/shared/types";
import type {
  NearbyShopsPagedResponse,
  ShopProfile,
  StoreReviewsSummary,
} from "@/features/shops/types/shop.types";
import type {
  ShopContentManagement,
  ShopListManagement,
} from "@/features/shops/types/shopManagement.types";
import type { ModerationActionResult } from "@/shared/types/moderationAction.types";

export const response0 = {
  data: {
    items: [
      {
        id: "00000000-0000-4000-8000-000000000001",
        name: "Bếp Xanh Sài Gòn",
        description: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
        address: "12 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
        latitude: 10.7769,
        longitude: 106.7009,
        distanceInKm: 1,
        logoUrl: "/images/deals/banh-mi/bm-overview.jpg",
        coverImageUrl: "/images/deals/banh-mi/bm-overview.jpg",
        openingTime: "07:00:00",
        closingTime: "23:00:00",
        isOpen: true,
        averageRating: 4,
        totalReviews: 1,
        flashDealCount: 1,
      },
      {
        id: "00000000-0000-4000-8000-000000000002",
        name: "Tiệm Bánh An Nhiên",
        description: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
        address: "12 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
        latitude: 10.777899999999999,
        longitude: 106.70190000000001,
        distanceInKm: 2,
        logoUrl: "/images/deals/banh-mi/bm-overview.jpg",
        coverImageUrl: "/images/deals/banh-mi/bm-overview.jpg",
        openingTime: "07:00:00",
        closingTime: "23:00:00",
        isOpen: true,
        averageRating: 5,
        totalReviews: 2,
        flashDealCount: 2,
      },
      {
        id: "00000000-0000-4000-8000-000000000003",
        name: "Góc Rau Đà Lạt",
        address: "12 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
        latitude: 10.7789,
        longitude: 106.7029,
        distanceInKm: 3,
        isOpen: true,
        averageRating: 4,
        totalReviews: 3,
      },
    ],
    page: 1,
    pageSize: 10,
    totalItems: 1,
    totalPages: 1,
    hasPrevious: false,
    hasNext: false,
    allStoresCount: 1,
    flashDealsStoresCount: 1,
  },
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<NearbyShopsPagedResponse>;
export const response1 = {
  data: {
    id: "00000000-0000-4000-8000-000000000001",
    name: "Bếp Xanh Sài Gòn",
    description: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
    addressLine: "12 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
    ward: "Bến Nghé",
    district: "Quận 1",
    city: "TP. Hồ Chí Minh",
    latitude: 10.7769,
    longitude: 106.7009,
    logoUrl: "/images/deals/banh-mi/bm-overview.jpg",
    coverImageUrl: "/images/deals/banh-mi/bm-overview.jpg",
    openingTime: "07:00:00",
    closingTime: "23:00:00",
    isOpen: true,
    averageRating: 4,
    totalReviews: 1,
    ratingBreakdown: {},
  },
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<ShopProfile>;
export const response2 = {
  data: {
    averageRating: 4,
    totalReviews: 1,
    ratingBreakdown: {},
    reviews: {
      items: [
        {
          id: "00000000-0000-4000-8000-000000000001",
          userId: "00000000-0000-4000-8000-000000000001",
          userFullName: "Bếp Xanh Sài Gòn",
          userAvatarUrl: "/images/deals/banh-mi/bm-overview.jpg",
          userTier: "Tiêu chuẩn",
          rating: 4,
          comment: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
          productId: "00000000-0000-4000-8000-000000000001",
          productName: "Bếp Xanh Sài Gòn",
          productImageUrl: "/images/deals/banh-mi/bm-overview.jpg",
          variantName: "Bếp Xanh Sài Gòn",
          createdAt: past,
          images: [
            "/images/deals/banh-mi/bm-overview.jpg",
            "/images/deals/banh-mi/bm-overview.jpg",
          ],
        },
        {
          id: "00000000-0000-4000-8000-000000000002",
          userId: "00000000-0000-4000-8000-000000000001",
          userFullName: "Tiệm Bánh An Nhiên",
          userAvatarUrl: "/images/deals/banh-mi/bm-overview.jpg",
          userTier: "Cỡ lớn",
          rating: 5,
          comment: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
          productId: "00000000-0000-4000-8000-000000000002",
          productName: "Tiệm Bánh An Nhiên",
          productImageUrl: "/images/deals/banh-mi/bm-overview.jpg",
          variantName: "Tiệm Bánh An Nhiên",
          createdAt: past,
          images: [
            "/images/deals/banh-mi/bm-overview.jpg",
            "/images/deals/banh-mi/bm-overview.jpg",
          ],
        },
        {
          id: "00000000-0000-4000-8000-000000000003",
          userId: "00000000-0000-4000-8000-000000000001",
          userFullName: "Góc Rau Đà Lạt",
          rating: 4,
          productId: "00000000-0000-4000-8000-000000000003",
          productName: "Góc Rau Đà Lạt",
          createdAt: past,
        },
      ],
      page: 1,
      pageSize: 10,
      totalItems: 1,
      totalPages: 1,
      hasPrevious: false,
      hasNext: false,
    },
  },
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<StoreReviewsSummary>;
export const response3 = {
  data: {
    items: [
      {
        id: "00000000-0000-4000-8000-000000000001",
        name: "Bếp Xanh Sài Gòn",
        description: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
        status: "Active",
        city: "TP. Hồ Chí Minh",
        ownerUserId: "00000000-0000-4000-8000-000000000001",
        ownerName: "Bếp Xanh Sài Gòn",
        createdAt: past,
      },
      {
        id: "00000000-0000-4000-8000-000000000002",
        name: "Tiệm Bánh An Nhiên",
        description: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
        status: "Active",
        city: "TP. Hồ Chí Minh",
        ownerUserId: "00000000-0000-4000-8000-000000000002",
        ownerName: "Tiệm Bánh An Nhiên",
        createdAt: past,
      },
      {
        id: "00000000-0000-4000-8000-000000000003",
        name: "Góc Rau Đà Lạt",
        description:
          "Thực phẩm được chuẩn bị trong ngày, đóng gói cẩn thận. Vui lòng nhận trước giờ đóng cửa; sản phẩm có thể chứa đậu phộng và sữa.",
        status: "Active",
        city: "TP. Hồ Chí Minh",
        ownerUserId: "00000000-0000-4000-8000-000000000003",
        ownerName: "Góc Rau Đà Lạt",
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
} satisfies ApiResponse<{
  items: ShopListManagement[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}>;
export const response4 = {
  data: {
    currentStatusReason: "Active",
    statusHistory: [
      {
        id: "00000000-0000-4000-8000-000000000001",
        fromStatus: "Active",
        toStatus: "Active",
        reason: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
        createdAt: past,
      },
      {
        id: "00000000-0000-4000-8000-000000000002",
        fromStatus: "Active",
        toStatus: "Active",
        reason: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
        createdAt: past,
      },
    ],
    owner: {
      id: "00000000-0000-4000-8000-000000000001",
      fullName: "Bếp Xanh Sài Gòn",
      email: "minh1@example.test",
      phoneNumber: "0901234567",
    },
    addressLine: "12 Nguyễn Huệ, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
    ward: "Bến Nghé",
    district: "Quận 1",
    latitude: 10.7769,
    longitude: 106.7009,
    logoUrl: "/images/deals/banh-mi/bm-overview.jpg",
    coverImageUrl: "/images/deals/banh-mi/bm-overview.jpg",
    openingTime: "07:00:00",
    closingTime: "23:00:00",
    businessLicenseNo: "Tiêu chuẩn",
    documents: [
      {
        id: "00000000-0000-4000-8000-000000000001",
        documentType: "Tiêu chuẩn",
        fileUrl: "/images/deals/banh-mi/bm-overview.jpg",
        originalFileName: "Bếp Xanh Sài Gòn",
        contentType: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
        uploadedAt: past,
      },
      {
        id: "00000000-0000-4000-8000-000000000002",
        documentType: "Cỡ lớn",
        fileUrl: "/images/deals/banh-mi/bm-overview.jpg",
        originalFileName: "Tiệm Bánh An Nhiên",
        contentType: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
        uploadedAt: past,
      },
    ],
    updatedAt: past,
    id: "00000000-0000-4000-8000-000000000001",
    name: "Bếp Xanh Sài Gòn",
    description: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
    status: "Active",
    city: "TP. Hồ Chí Minh",
    ownerUserId: "00000000-0000-4000-8000-000000000001",
    ownerName: "Bếp Xanh Sài Gòn",
    createdAt: past,
  },
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<ShopContentManagement>;
export const response5 = {
  data: {
    targetId: "00000000-0000-4000-8000-000000000001",
    action: "Tiêu chuẩn",
    message: "Tiêu chuẩn",
  },
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<ModerationActionResult>;
export default {
  "shops-shopApi_getNearbyShops_0": response0,
  "shops-shopApi_getShopProfile_1": response1,
  "shops-shopApi_getStoreReviews_2": response2,
  "shops-shopApi_getShopsForManagement_3": response3,
  "shops-shopApi_getShopContentForManagement_4": response4,
  "shops-shopApi_updateModerationStatus_5": response5,
};
