import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";
import type {
  AuthRole,
  AuthUser,
  CurrentUserResponse,
  ForgotPasswordInput,
  LoginCredentials,
  RegisterInput,
  TokenPair,
  VerifyOtpInput,
} from "@/features/auth/types/auth.types";

interface ApiMessageResponse {
  success: boolean;
  message?: string;
}

const roleMap: Record<string, AuthRole> = {
  user: "user",
  storeowner: "storeOwner",
  staff: "staff",
  admin: "admin",
};

function toAuthUser(response: CurrentUserResponse): AuthUser {
  const role = roleMap[response.role.toLowerCase()];

  if (!role) {
    throw new Error("Your account role is not supported by this application.");
  }

  return { ...response, role };
}

export const authApi = {
  async login(credentials: LoginCredentials): Promise<TokenPair> {
    const response = await httpClient.post<ApiResponse<TokenPair>>(
      API_ENDPOINTS.AUTH.LOGIN,
      credentials,
    );
    return response.data.data;
  },

  async getMe(): Promise<AuthUser> {
    const response = await httpClient.get<ApiResponse<CurrentUserResponse>>(API_ENDPOINTS.AUTH.ME);
    return toAuthUser(response.data.data);
  },

  async logout(refreshToken: string): Promise<void> {
    await httpClient.post<ApiMessageResponse>(API_ENDPOINTS.AUTH.LOGOUT, { refreshToken });
  },

  async register(input: RegisterInput): Promise<string | undefined> {
    const response = await httpClient.post<ApiMessageResponse>(API_ENDPOINTS.AUTH.REGISTER, input);
    return response.data.message;
  },

  async verifyRegistration(input: VerifyOtpInput): Promise<string | undefined> {
    const response = await httpClient.post<ApiMessageResponse>(
      API_ENDPOINTS.AUTH.VERIFY_REGISTRATION,
      input,
    );
    return response.data.message;
  },

  async forgotPassword(input: ForgotPasswordInput): Promise<string | undefined> {
    const response = await httpClient.post<ApiMessageResponse>(
      API_ENDPOINTS.AUTH.FORGOT_PASSWORD,
      input,
    );
    return response.data.message;
  },

  async resetPassword(input: VerifyOtpInput): Promise<string | undefined> {
    const response = await httpClient.post<ApiMessageResponse>(
      API_ENDPOINTS.AUTH.RESET_PASSWORD,
      input,
    );
    return response.data.message;
  },
};
