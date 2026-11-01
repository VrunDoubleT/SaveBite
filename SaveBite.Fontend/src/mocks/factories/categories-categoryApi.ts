// Generated from real service response DTOs. Regenerate with npm run mock:generate.
import type { ApiResponse } from "@/shared/types";
import type { Category } from "@/features/categories/types/category.types";

export const response0 = {
  data: [
    {
      id: "00000000-0000-4000-8000-000000000001",
      name: "Bánh mì",
      description: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
      imageUrl: "/images/deals/banh-mi/bm-overview.jpg",
      isActive: true,
    },
    {
      id: "00000000-0000-4000-8000-000000000002",
      name: "Rau củ",
      description: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
      imageUrl: "/images/deals/banh-mi/bm-overview.jpg",
      isActive: true,
    },
    {
      id: "00000000-0000-4000-8000-000000000003",
      name: "Bánh ngọt",
      isActive: true,
    },
  ],
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<Category[]>;
export const response1 = {
  data: [
    {
      id: "00000000-0000-4000-8000-000000000001",
      name: "Bánh mì",
      description: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
      imageUrl: "/images/deals/banh-mi/bm-overview.jpg",
      isActive: true,
    },
    {
      id: "00000000-0000-4000-8000-000000000002",
      name: "Rau củ",
      description: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
      imageUrl: "/images/deals/banh-mi/bm-overview.jpg",
      isActive: true,
    },
    {
      id: "00000000-0000-4000-8000-000000000003",
      name: "Bánh ngọt",
      isActive: true,
    },
  ],
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<Category[]>;
export default {
  "categories-categoryApi_getCategories_0": response0,
  "categories-categoryApi_getCategories_1": response1,
};
