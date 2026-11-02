import type { StoreReview, StoreReviewsSummary } from "@/features/shops/types/shop.types";
import type { ApiResponse } from "@/shared/types";
import { past } from "./values";

export function makeStoreReviewsResponse(): ApiResponse<StoreReviewsSummary> {
  const names = [
    "Nguyễn Minh Anh",
    "Trần Hoài Nam",
    "Lê Thu Hà",
    "Phạm Ngọc Linh",
    "Võ Tuấn Kiệt",
    "Đặng Thanh Mai",
    "Bùi Gia Huy",
    "Hoàng Thảo Vy",
    "Đỗ Minh Khang",
    "Ngô Bảo Trâm",
    "Hồ Phương Nhi",
    "Dương Quốc Bảo",
  ];
  const ratings = [5, 4, 5, 3, 5, 4, 2, 5, 4, 3, 5, 1];
  const comments = [
    "Bánh còn thơm, rau tươi và phần ăn vừa đủ. Mình đặt trước rồi ghé lấy rất nhanh.",
    "Giá tốt, cửa hàng đóng gói gọn gàng. Lần sau mình sẽ thử phần lớn.",
    "Nhân viên thân thiện, nhận hàng đúng giờ. Rất vui vì có thể giảm lãng phí đồ ăn.",
    "Món ăn ổn nhưng mình phải chờ thêm một chút khi nhận hàng.",
    "Phần bánh lớn và có nhiều rau, mức cay vừa rất hợp khẩu vị.",
    "Đồ ăn ngon, hướng dẫn nhận hàng rõ ràng.",
    "Bánh hơi nguội khi nhận. Mong cửa hàng chú ý hơn phần bảo quản.",
    "Mình đã quay lại lần thứ hai, chất lượng vẫn tốt.",
    "Phần ăn phù hợp với giá, sẽ tiếp tục ủng hộ.",
    "Hương vị ổn, bao bì có thể chắc chắn hơn.",
    "Nhận hàng nhanh, cửa hàng phản hồi rất nhiệt tình.",
    "Mình chưa hài lòng với thời gian chờ hôm đó.",
  ];
  const products = ["Bánh mì rau củ", "Túi bánh cuối ngày", "Hộp rau tươi Đà Lạt"];
  const uid = (value: number) => `00000000-0000-4000-8000-${String(value).padStart(12, "0")}`;
  const items: StoreReview[] = names.map((name, index) => ({
    id: uid(index + 1),
    userId: uid(index + 20),
    userFullName: name,
    userTier: index % 4 === 0 ? "Thành viên" : undefined,
    rating: ratings[index],
    comment: comments[index],
    productId: uid((index % 3) + 1),
    productName: products[index % 3],
    productImageUrl: "/images/deals/banh-mi/bm-overview.jpg",
    variantName: index % 2 ? "Lớn · Không cay" : "Thường · Cay vừa",
    createdAt: new Date(Date.parse(past) - index * 86400000).toISOString(),
    images:
      index % 3 === 0
        ? [
            "/images/deals/banh-mi/bm-thuong-khong-cay.jpg",
            "/images/deals/banh-mi/bm-lon-cay-vua.jpg",
          ]
        : index % 3 === 1
          ? ["/images/deals/banh-mi/bm-overview.jpg"]
          : [],
    reply:
      index % 3 === 0
        ? {
            id: uid(100 + index),
            content:
              "Cảm ơn bạn đã chia sẻ trải nghiệm. Cửa hàng đã ghi nhận góp ý và mong được đón bạn lần tới.",
            repliedByName: "Bếp Xanh Sài Gòn",
            createdAt: past,
          }
        : null,
  }));
  const ratingBreakdown: Record<number, number> = {};
  for (const item of items) ratingBreakdown[item.rating] = (ratingBreakdown[item.rating] ?? 0) + 1;
  return {
    success: true,
    data: {
      averageRating: items.reduce((sum, item) => sum + item.rating, 0) / items.length,
      totalReviews: items.length,
      ratingBreakdown,
      reviews: {
        items,
        page: 1,
        pageSize: items.length,
        totalItems: items.length,
        totalPages: 1,
        hasPrevious: false,
        hasNext: false,
      },
    },
  };
}
