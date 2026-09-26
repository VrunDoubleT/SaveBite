export interface ApiResponse<T> {
  data: T;
  message?: string;
  success: boolean;
}

export interface PaginatedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
}

export interface ApiErrorResponse {
  message: string;
  errorCode?: string;
  errors?: Record<string, string[]>;
  statusCode?: number;
}

export interface PaginationParams {
  page?: number;
  pageSize?: number;
}
