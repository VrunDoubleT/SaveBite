// Generated from real service response DTOs. Regenerate with npm run mock:generate.
import { past } from "./values";
import type { ApiResponse } from "@/shared/types";
import type { OwnerShop, ShopPaymentConfig } from "@/features/shops/types/ownerShop.types";

export const response0 = {
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
    status: "Active",
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
    averageRating: 4,
    totalReviews: 1,
    ratingBreakdown: {},
  },
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<OwnerShop>;
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
    status: "Active",
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
    averageRating: 4,
    totalReviews: 1,
    ratingBreakdown: {},
  },
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<OwnerShop>;
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
    bankName: "Bếp Xanh Sài Gòn",
    bankAccountNumber: "0123456789",
    bankAccountHolder: "Bếp Xanh Sài Gòn",
    payosClientId: "00000000-0000-4000-8000-000000000001",
    hasPayosApiKey: true,
    hasPayosChecksumKey: true,
    updatedAt: past,
  },
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<ShopPaymentConfig>;
export const response5 = {
  data: {
    bankName: "Bếp Xanh Sài Gòn",
    bankAccountNumber: "0123456789",
    bankAccountHolder: "Bếp Xanh Sài Gòn",
    payosClientId: "00000000-0000-4000-8000-000000000001",
    hasPayosApiKey: true,
    hasPayosChecksumKey: true,
    updatedAt: past,
  },
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<ShopPaymentConfig>;
export default {
  "shops-ownerShopApi_getMyShop_0": response0,
  "shops-ownerShopApi_getMyShopProfile_1": response1,
  "shops-ownerShopApi_updateBusinessInfo_2": response2,
  "shops-ownerShopApi_updateImages_3": response3,
  "shops-ownerShopApi_getMyPaymentConfig_4": response4,
  "shops-ownerShopApi_updateMyPaymentConfig_5": response5,
};
