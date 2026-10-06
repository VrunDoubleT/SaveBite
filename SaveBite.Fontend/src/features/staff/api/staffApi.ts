import { httpClient } from "@/shared/api/httpClient";
import { API_ENDPOINTS } from "@/shared/api/endpoints";
import type { ApiResponse } from "@/shared/types";

import type {
  AssociatedShop,
  StaffShopDetail,
} from "../types/staff.types";

export const staffApi = {
  async getAssociatedShops(): Promise<AssociatedShop[]> {
    const response = await httpClient.get<
      ApiResponse<AssociatedShop[]>
    >(API_ENDPOINTS.STAFF.SHOPS);

    return response.data.data;
  },


  async getShopDetail(shopId: string) {
    return httpClient.get<ApiResponse<StaffShopDetail>>(
      API_ENDPOINTS.STAFF.SHOP_DETAIL(shopId),
    );
  },
};