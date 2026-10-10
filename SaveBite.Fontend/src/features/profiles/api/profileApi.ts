import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";
import type { UpdateUserProfileInput } from "@/features/profiles/types/profile.types";
import type { CurrentUserResponse } from "@/features/auth/types/auth.types";

export const profileApi = {
  async updateProfile(input: UpdateUserProfileInput): Promise<CurrentUserResponse> {
    const response = await httpClient.put<ApiResponse<CurrentUserResponse>>(
      API_ENDPOINTS.PROFILES.ME,
      input,
    );

    return response.data.data;
  },

  async uploadAvatar(file: File): Promise<CurrentUserResponse> {
    const formData = new FormData();
    formData.append("file", file);

    const response = await httpClient.put<ApiResponse<CurrentUserResponse>>(
      API_ENDPOINTS.PROFILES.AVATAR,
      formData,
      {
        headers: { "Content-Type": "multipart/form-data" },
      },
    );

    return response.data.data;
  },
};
