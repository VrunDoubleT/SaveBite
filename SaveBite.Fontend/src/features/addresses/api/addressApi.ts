import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";
import type {
  CreateUserAddressInput,
  UpdateAddressInput,
  UserAddress,
} from "../types/address.types";

export const addressApi = {
  async getAddresses(): Promise<UserAddress[]> {
    const response = await httpClient.get<ApiResponse<UserAddress[]>>(
      API_ENDPOINTS.ADDRESSES.BASE,
    );

    return response.data.data;
  },

  async createAddress(input: CreateUserAddressInput): Promise<UserAddress> {
    const response = await httpClient.post<ApiResponse<UserAddress>>(
      API_ENDPOINTS.ADDRESSES.BASE,
      input,
    );

    return response.data.data;
  },

  async updateAddress(addressId: string, input: UpdateAddressInput): Promise<UserAddress> {
    const response = await httpClient.put<ApiResponse<UserAddress>>(
      API_ENDPOINTS.ADDRESSES.DETAIL(addressId),
      input,
    );

    return response.data.data;
  },

  async setDefaultAddress(addressId: string): Promise<UserAddress> {
    const response = await httpClient.put<ApiResponse<UserAddress>>(
      API_ENDPOINTS.ADDRESSES.DEFAULT(addressId),
    );

    return response.data.data;
  },

  async deleteAddress(addressId: string): Promise<void> {
    await httpClient.delete(API_ENDPOINTS.ADDRESSES.DETAIL(addressId));
  },
};
