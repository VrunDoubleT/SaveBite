// Generated from real service response DTOs. Regenerate with npm run mock:generate.
import { past } from "./values";
import type { ApiResponse } from "@/shared/types";
import type { Category } from "@/features/categories/types/adminCategory.types";
interface BackendPagedCategory {
  items: Category[];
  page: number;
  pageSize: number;
  totalItems: number; // C# trả về trường này
  totalPages: number;
}
export const response0 = {
  data: {
    items: [
      {
        id: "00000000-0000-4000-8000-000000000001",
        name: "Bánh mì",
        description: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
        imageUrl: "/images/deals/banh-mi/bm-overview.jpg",
        isActive: true,
        createdAt: past,
        updatedAt: past,
      },
      {
        id: "00000000-0000-4000-8000-000000000002",
        name: "Rau củ",
        description: "Thực phẩm tươi ngon trong ngày, giảm lãng phí cùng SaveBite.",
        imageUrl: "/images/deals/banh-mi/bm-overview.jpg",
        isActive: true,
        createdAt: past,
        updatedAt: past,
      },
      {
        id: "00000000-0000-4000-8000-000000000003",
        name: "Bánh ngọt",
        isActive: true,
        createdAt: past,
        updatedAt: past,
      },
    ],
    page: 1,
    pageSize: 10,
    totalItems: 1,
    totalPages: 1,
  },
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<BackendPagedCategory>;
export const response1 = {
  data: "Tiêu chuẩn",
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<string>;
export const response2 = {
  data: "Tiêu chuẩn",
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<string>;
export const response3 = {
  data: "Tiêu chuẩn",
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<string>;
export const response4 = {
  data: "Tiêu chuẩn",
  message: "Tiêu chuẩn",
  success: true,
} satisfies ApiResponse<string>;
export default {
  "categories-adminCategoryApi_getCategories_0": response0,
  "categories-adminCategoryApi_createCategory_1": response1,
  "categories-adminCategoryApi_updateCategory_2": response2,
  "categories-adminCategoryApi_toggleStatus_3": response3,
  "categories-adminCategoryApi_deleteCategory_4": response4,
};
