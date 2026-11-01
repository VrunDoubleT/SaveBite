// Generated from real service response DTOs. Regenerate with npm run mock:generate.
import { past, future } from "./values";
import type { ApiResponse } from "@/shared/types";
import type { Warning, WarningPage } from "@/features/warnings/types/warning.types";
import type { ModerationActionResult } from "@/shared/types/moderationAction.types";

export const response0 = {
  data: {
    items: [
      {
        id: "00000000-0000-4000-8000-000000000001",
        shopId: "00000000-0000-4000-8000-000000000001",
        productId: "00000000-0000-4000-8000-000000000001",
        productName: "Bánh mì rau củ",
        requiredCorrection: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
        action: "Tiêu chuẩn",
        reason: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
        oldValuesJson: "Tiêu chuẩn",
        newValuesJson: "Tiêu chuẩn",
        issuedByUserId: "00000000-0000-4000-8000-000000000001",
        createdAt: past,
        dueAt: future,
        firstEditedAt: past,
        resolvedAt: past,
        dealBlocked: false,
      },
      {
        id: "00000000-0000-4000-8000-000000000002",
        shopId: "00000000-0000-4000-8000-000000000001",
        productId: "00000000-0000-4000-8000-000000000002",
        productName: "Túi bánh cuối ngày",
        requiredCorrection: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
        action: "Cỡ lớn",
        reason: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
        oldValuesJson: "Cỡ lớn",
        newValuesJson: "Cỡ lớn",
        issuedByUserId: "00000000-0000-4000-8000-000000000002",
        createdAt: past,
        dueAt: future,
        firstEditedAt: past,
        resolvedAt: past,
        dealBlocked: false,
      },
      {
        id: "00000000-0000-4000-8000-000000000003",
        shopId: "00000000-0000-4000-8000-000000000001",
        productId: "00000000-0000-4000-8000-000000000003",
        productName: "Hộp rau tươi Đà Lạt",
        requiredCorrection:
          "Thực phẩm được chuẩn bị trong ngày, đóng gói cẩn thận. Vui lòng nhận trước giờ đóng cửa; sản phẩm có thể chứa đậu phộng và sữa.",
        action: "Không cay",
        reason:
          "Thực phẩm được chuẩn bị trong ngày, đóng gói cẩn thận. Vui lòng nhận trước giờ đóng cửa; sản phẩm có thể chứa đậu phộng và sữa.",
        oldValuesJson: "Không cay",
        newValuesJson: "Không cay",
        issuedByUserId: "00000000-0000-4000-8000-000000000003",
        createdAt: past,
        dueAt: future,
        firstEditedAt: past,
        resolvedAt: past,
        dealBlocked: false,
      },
    ],
    page: 1,
    pageSize: 10,
    totalItems: 1,
    totalPages: 1,
  },
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<WarningPage>;
export const response1 = {
  data: {
    id: "00000000-0000-4000-8000-000000000001",
    shopId: "00000000-0000-4000-8000-000000000001",
    productId: "00000000-0000-4000-8000-000000000001",
    productName: "Bánh mì rau củ",
    requiredCorrection: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
    action: "Tiêu chuẩn",
    reason: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
    oldValuesJson: "Tiêu chuẩn",
    newValuesJson: "Tiêu chuẩn",
    issuedByUserId: "00000000-0000-4000-8000-000000000001",
    createdAt: past,
    dueAt: future,
    firstEditedAt: past,
    resolvedAt: past,
    dealBlocked: false,
  },
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<Warning>;
export const response2 = {
  data: {
    targetId: "00000000-0000-4000-8000-000000000001",
    action: "Tiêu chuẩn",
    message: "Tiêu chuẩn",
  },
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<ModerationActionResult>;
export default {
  "warnings-warningApi_history_0": response0,
  "warnings-warningApi_details_1": response1,
  "warnings-warningApi_issueForProduct_2": response2,
};
