import type { APIRequestContext } from "@playwright/test";
import { API_URL } from "./authHelper.js";

export interface UserProfileSnapshot {
  id: string;
  email: string;
  fullName: string;
  phone: string | null;
  avatarUrl: string | null;
  customerStatus: string;
}

export interface UserAddressItem {
  id: string;
  label: string | null;
  addressLine: string;
  ward: string | null;
  district: string | null;
  city: string | null;
  latitude: number;
  longitude: number;
  isDefault: boolean;
}

export interface AddressPayload {
  label?: string | null;
  addressLine: string;
  ward?: string | null;
  district?: string | null;
  city?: string | null;
  latitude: number;
  longitude: number;
  isDefault: boolean;
}

export interface NearbyShopItem {
  id: string;
  name: string;
  isOpen: boolean;
}

export interface FollowedShopItem {
  shopId: string;
  shopName: string;
  notificationStatus: "Enabled" | "Disabled";
}

function authHeaders(accessToken: string) {
  return {
    Authorization: `Bearer ${accessToken}`,
  };
}

export async function getMeViaApi(
  request: APIRequestContext,
  accessToken: string,
): Promise<UserProfileSnapshot> {
  const response = await request.get(`${API_URL}/users/me`, {
    headers: authHeaders(accessToken),
  });
  if (!response.ok()) {
    throw new Error(`GET /users/me failed (${response.status()}): ${await response.text()}`);
  }
  const json = (await response.json()) as { data: UserProfileSnapshot };
  return json.data;
}

export async function updateProfileViaApi(
  request: APIRequestContext,
  accessToken: string,
  input: { fullName: string; phone: string | null },
): Promise<void> {
  const response = await request.put(`${API_URL}/users/me`, {
    headers: authHeaders(accessToken),
    data: input,
  });
  if (!response.ok()) {
    throw new Error(`PUT /users/me failed (${response.status()}): ${await response.text()}`);
  }
}

export async function getAddressesViaApi(
  request: APIRequestContext,
  accessToken: string,
): Promise<UserAddressItem[]> {
  const response = await request.get(`${API_URL}/user-addresses`, {
    headers: authHeaders(accessToken),
  });
  if (!response.ok()) {
    throw new Error(
      `GET /user-addresses failed (${response.status()}): ${await response.text()}`,
    );
  }
  const json = (await response.json()) as { data: UserAddressItem[] };
  return json.data;
}

export async function createAddressViaApi(
  request: APIRequestContext,
  accessToken: string,
  input: AddressPayload,
): Promise<UserAddressItem> {
  const response = await request.post(`${API_URL}/user-addresses`, {
    headers: authHeaders(accessToken),
    data: input,
  });
  if (!response.ok()) {
    throw new Error(
      `POST /user-addresses failed (${response.status()}): ${await response.text()}`,
    );
  }
  const json = (await response.json()) as { data: UserAddressItem };
  return json.data;
}

export async function setDefaultAddressViaApi(
  request: APIRequestContext,
  accessToken: string,
  addressId: string,
): Promise<void> {
  const response = await request.put(`${API_URL}/user-addresses/${addressId}/default`, {
    headers: authHeaders(accessToken),
  });
  if (!response.ok()) {
    throw new Error(
      `PUT /user-addresses/${addressId}/default failed (${response.status()}): ${await response.text()}`,
    );
  }
}

export async function deleteAddressViaApi(
  request: APIRequestContext,
  accessToken: string,
  addressId: string,
): Promise<{ status: number; ok: boolean; body: string }> {
  const response = await request.delete(`${API_URL}/user-addresses/${addressId}`, {
    headers: authHeaders(accessToken),
  });
  return {
    status: response.status(),
    ok: response.ok(),
    body: await response.text(),
  };
}

/**
 * Khôi phục danh sách địa chỉ về đúng trạng thái trước khi test chạy:
 * - Đặt lại địa chỉ mặc định ban đầu (nếu bị đổi)
 * - Xóa tất cả địa chỉ mới được tạo trong test
 */
export async function restoreAddresses(
  request: APIRequestContext,
  accessToken: string,
  originalAddresses: UserAddressItem[],
): Promise<void> {
  try {
    const originalIds = new Set(originalAddresses.map((item) => item.id));
    const originalDefault = originalAddresses.find((item) => item.isDefault);
    const current = await getAddressesViaApi(request, accessToken);

    if (originalDefault && current.some((x) => x.id === originalDefault.id)) {
      const currentDefault = current.find((item) => item.isDefault);
      if (currentDefault?.id !== originalDefault.id) {
        try {
          await setDefaultAddressViaApi(request, accessToken, originalDefault.id);
        } catch {
          // ignore if already default or cannot change
        }
      }
    }

    const refreshed = await getAddressesViaApi(request, accessToken).catch(() => current);
    for (const addr of refreshed) {
      if (!originalIds.has(addr.id) && !addr.isDefault) {
        try {
          await deleteAddressViaApi(request, accessToken, addr.id);
        } catch {
          // ignore deletion error for non-critical address
        }
      }
    }
  } catch {
    // protect test tear-down from failing the suite
  }
}

export async function findActiveShopsViaApi(
  request: APIRequestContext,
  accessToken: string,
): Promise<NearbyShopItem[]> {
  const response = await request.get(`${API_URL}/shops`, {
    headers: authHeaders(accessToken),
    params: {
      latitude: 10.012452,
      longitude: 105.732432,
      radiusInKm: 10,
      page: 1,
      pageSize: 20,
    },
  });
  if (!response.ok()) {
    throw new Error(`GET /shops failed (${response.status()}): ${await response.text()}`);
  }
  const json = (await response.json()) as { data?: { items?: NearbyShopItem[] } };
  return json.data?.items ?? [];
}

export async function getFollowedShopsViaApi(
  request: APIRequestContext,
  accessToken: string,
): Promise<FollowedShopItem[]> {
  const response = await request.get(`${API_URL}/me/followed-shops`, {
    headers: authHeaders(accessToken),
    params: { page: 1, pageSize: 50 },
  });
  if (!response.ok()) {
    throw new Error(
      `GET /me/followed-shops failed (${response.status()}): ${await response.text()}`,
    );
  }
  const json = (await response.json()) as { data?: { items?: FollowedShopItem[] } };
  return json.data?.items ?? [];
}

export async function followShopViaApi(
  request: APIRequestContext,
  accessToken: string,
  shopId: string,
): Promise<void> {
  const response = await request.put(`${API_URL}/shops/${encodeURIComponent(shopId)}/follow`, {
    headers: authHeaders(accessToken),
  });
  if (!response.ok()) {
    throw new Error(
      `PUT /shops/${shopId}/follow failed (${response.status()}): ${await response.text()}`,
    );
  }
}

export async function unfollowShopViaApi(
  request: APIRequestContext,
  accessToken: string,
  shopId: string,
): Promise<void> {
  const response = await request.delete(`${API_URL}/shops/${encodeURIComponent(shopId)}/follow`, {
    headers: authHeaders(accessToken),
  });
  if (!response.ok() && response.status() !== 404) {
    throw new Error(
      `DELETE /shops/${shopId}/follow failed (${response.status()}): ${await response.text()}`,
    );
  }
}
