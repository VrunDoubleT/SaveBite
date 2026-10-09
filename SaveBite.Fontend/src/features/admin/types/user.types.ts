export interface AdminUser {
  id: string;
  email: string;
  fullName: string;
  role: string;
  status: string;
  customerStatus: string;
  createdAt: string;
}

export interface UserLogResponse {
  action: string;
  reason: string;
  changedValues: string;
  createdAt: string;
  adminId: string;
}

// interface Role Log
export interface UserRoleLogResponse {
  oldRole: string;
  newRole: string;
  reason: string;
  createdAt: string;
}

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
  roleLogs: UserRoleLogResponse[];
}

export interface UpdateUserStatusRequest {
  isSuspended: boolean;
  isCustomerProfile: boolean;
  reason: string;
}
