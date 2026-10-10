import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";
import type {
  CreateUserAddressInput,
  UpdateAddressInput,
  UpdateUserProfileInput,
  UserAddress,
} from "@/features/account/profile/types/profile.types";
import type { CurrentUserResponse } from "@/features/auth/types/auth.types";

export const accountApi = {
  async updateProfile(input: UpdateUserProfileInput): Promise<CurrentUserResponse> {
    const response = await httpClient.put<ApiResponse<CurrentUserResponse>>(
      API_ENDPOINTS.USER_PROFILE.PROFILE,
      input,
    );

    return response.data.data;
  },

  async uploadAvatar(file: File): Promise<CurrentUserResponse> {
    const formData = new FormData();
    formData.append("file", file);

    const response = await httpClient.post<ApiResponse<CurrentUserResponse>>(
      API_ENDPOINTS.USER_PROFILE.AVATAR,
      formData,
      {
        headers: { "Content-Type": "multipart/form-data" },
      },
    );

    return response.data.data;
  },

  async getAddresses(): Promise<UserAddress[]> {
    const response = await httpClient.get<ApiResponse<UserAddress[]>>(
      API_ENDPOINTS.USER_PROFILE.ADDRESSES,
    );

    return response.data.data;
  },

  async createAddress(input: CreateUserAddressInput): Promise<UserAddress> {
    const response = await httpClient.post<ApiResponse<UserAddress>>(
      API_ENDPOINTS.USER_PROFILE.CREATE_ADDRESS,
      input,
    );

    return response.data.data;
  },

  async updateAddress(addressId: string, input: UpdateAddressInput): Promise<UserAddress> {
    const response = await httpClient.put<ApiResponse<UserAddress>>(
      `${API_ENDPOINTS.USER_PROFILE.ADDRESSES}/${addressId}`,
      input,
    );

    return response.data.data;
  },

  async setDefaultAddress(addressId: string): Promise<UserAddress> {
    const response = await httpClient.put<ApiResponse<UserAddress>>(
      API_ENDPOINTS.USER_PROFILE.ADDRESS_DEFAULT(addressId),
    );

    return response.data.data;
  },

  async deleteAddress(addressId: string): Promise<void> {
    await httpClient.delete(API_ENDPOINTS.USER_PROFILE.ADDRESS_BY_ID(addressId));
  },
};
