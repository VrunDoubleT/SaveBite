export type AuthRole = "user" | "storeOwner" | "staff" | "admin";

<<<<<<< HEAD
export type CustomerStatus = "active" | "suspended";
=======
export interface UserAddress {
  id: string;
  label?: string | null;
  addressLine: string;
  ward?: string | null;
  district?: string | null;
  city?: string | null;
  latitude: number;
  longitude: number;
  isDefault: boolean;
}
>>>>>>> origin/feature/iss-4-store-information-feedback-management-view-side

export interface AuthUser {
  id: string;
  email: string;
  phone: string | null;
  fullName: string;
  avatarUrl: string | null;
  role: AuthRole;
<<<<<<< HEAD
  customerStatus: CustomerStatus;
=======
  defaultAddress?: UserAddress | null;
>>>>>>> origin/feature/iss-4-store-information-feedback-management-view-side
}

export interface LoginCredentials {
  email: string;
  password: string;
}

export interface RegisterInput {
  fullName: string;
  email: string;
  password: string;
}

export interface VerifyOtpInput {
  email: string;
  otp: string;
}

export interface ForgotPasswordInput {
  email: string;
  newPassword: string;
}

export interface TokenPair {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
}

export interface CurrentUserResponse {
  id: string;
  email: string;
  phone: string | null;
  fullName: string;
  avatarUrl: string | null;
  role: string;
<<<<<<< HEAD
  customerStatus: CustomerStatus;
=======
  defaultAddress?: UserAddress | null;
>>>>>>> origin/feature/iss-4-store-information-feedback-management-view-side
}
