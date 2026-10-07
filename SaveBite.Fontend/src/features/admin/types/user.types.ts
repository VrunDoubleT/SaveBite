export interface AdminUser {
  id: string;
  email: string;
  fullName: string;
  role: string;
  status: string;
  customerStatus: string;
  createdAt: string;
}

// DTO Log
export interface UserLogResponse {
  action: string;
  reason: string;
  changedValues: string;
  createdAt: string;
  adminId: string;
}

// DTO Chi tiết User
export interface UserDetailsResponse {
  id: string;
  email: string;
  phone: string | null;
  fullName: string;
  avatarUrl: string | null;
  role: string;
  status: string;
  customerStatus: string;
  shopStatus: string;
  createdAt: string;
  updatedAt: string | null;
  logs: UserLogResponse[];
}

// DTO Request Update
export interface UpdateUserStatusRequest {
  isSuspended: boolean;
  isCustomerProfile: boolean;
  reason: string;
}
