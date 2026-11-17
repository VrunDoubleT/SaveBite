import type { APIRequestContext } from "@playwright/test";
import { API_URL } from "./authHelper.js";

export interface FlashDealSummary {
  id: string;
  productName: string;
  originalPrice: number;
  discountPrice?: number;
  shopId: string;
  shopName: string;
  shopAddress?: string;
  category?: string;
}

function authHeaders(accessToken?: string): Record<string, string> | undefined {
  return accessToken ? { Authorization: `Bearer ${accessToken}` } : undefined;
}

/**
 * Lấy danh sách Flash Deals đang mở bán gần toạ độ mặc định Cần Thơ.
 */
export async function getNearbyFlashDealsViaApi(
  request: APIRequestContext,
  accessToken?: string,
): Promise<FlashDealSummary[]> {
  const response = await request.get(`${API_URL}/flash-deals`, {
    headers: authHeaders(accessToken),
    params: {
      latitude: 10.012452,
      longitude: 105.732432,
      radiusInKm: 50,
      limit: 20,
    },
  });

  if (!response.ok()) {
    throw new Error(`GET /flash-deals failed (${response.status()}): ${await response.text()}`);
  }

  const json = (await response.json()) as { data?: { deals?: FlashDealSummary[] } };
  return json.data?.deals ?? [];
}

/**
 * Lấy chi tiết một Flash Deal theo ID.
 */
export async function getFlashDealByIdViaApi(
  request: APIRequestContext,
  dealId: string,
  accessToken?: string,
): Promise<FlashDealSummary | null> {
  const response = await request.get(`${API_URL}/flash-deals/${dealId}`, {
    headers: authHeaders(accessToken),
  });

  if (!response.ok()) {
    return null;
  }

  const json = (await response.json()) as { data?: FlashDealSummary };
  return json.data ?? null;
}
