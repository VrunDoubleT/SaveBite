// Generated from real service response DTOs. Regenerate with npm run mock:generate.
import { person, past } from "./values";
import type { ApiResponse } from "@/shared/types";
import type { FeedbackPage } from "@/features/feedback/types/feedback.types";

export const response0 = {
  data: {
    items: [
      {
        id: "00000000-0000-4000-8000-000000000001",
        productId: "00000000-0000-4000-8000-000000000001",
        productName: "Bánh mì rau củ",
        shopName: "Bếp Xanh Sài Gòn",
        userId: "00000000-0000-4000-8000-000000000001",
        customerName: person(0),
        customerPhone: "0901234567",
        customerEmail: "minh1@example.test",
        orderItemId: "00000000-0000-4000-8000-000000000001",
        rating: 4,
        comment: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
        status: "Visible",
        currentReason: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
        createdAt: past,
        hiddenAt: past,
      },
      {
        id: "00000000-0000-4000-8000-000000000002",
        productId: "00000000-0000-4000-8000-000000000002",
        productName: "Túi bánh cuối ngày",
        shopName: "Tiệm Bánh An Nhiên",
        userId: "00000000-0000-4000-8000-000000000001",
        customerName: person(1),
        customerPhone: "0901234567",
        customerEmail: "minh2@example.test",
        orderItemId: "00000000-0000-4000-8000-000000000002",
        rating: 5,
        comment: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
        status: "HiddenByCustomer",
        currentReason: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
        createdAt: past,
        hiddenAt: past,
      },
      {
        id: "00000000-0000-4000-8000-000000000003",
        productId: "00000000-0000-4000-8000-000000000003",
        productName: "Hộp rau tươi Đà Lạt",
        shopName: "Góc Rau Đà Lạt",
        userId: "00000000-0000-4000-8000-000000000001",
        customerName: person(2),
        customerPhone: "0901234567",
        customerEmail: "minh3@example.test",
        orderItemId: "00000000-0000-4000-8000-000000000003",
        rating: 4,
        comment:
          "Thực phẩm được chuẩn bị trong ngày, đóng gói cẩn thận. Vui lòng nhận trước giờ đóng cửa; sản phẩm có thể chứa đậu phộng và sữa.",
        status: "HiddenByAdmin",
        currentReason:
          "Thực phẩm được chuẩn bị trong ngày, đóng gói cẩn thận. Vui lòng nhận trước giờ đóng cửa; sản phẩm có thể chứa đậu phộng và sữa.",
        createdAt: past,
        hiddenAt: past,
      },
    ],
    page: 1,
    pageSize: 10,
    totalItems: 1,
    totalPages: 1,
  },
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<FeedbackPage>;
export const response1 = {
  success: true,
  message: "Thao tác thành công.",
} satisfies unknown;
export const response2 = {
  success: true,
  message: "Thao tác thành công.",
} satisfies unknown;
export default {
  "feedback-feedbackApi_list_0": response0,
  "feedback-feedbackApi_remove_1": response1,
  "feedback-feedbackApi_restore_2": response2,
};
