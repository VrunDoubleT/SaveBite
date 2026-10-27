import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";
import type {
  AuditLogFilterOptions,
  AuditLogItem,
  AuditLogPageResult,
  AuditLogQueryParams,
} from "../types/auditLog.types";

interface BackendAuditLogPageResponse {
  page: {
    items: AuditLogItem[];
    page: number;
    pageSize: number;
    totalItems: number;
    totalPages: number;
  };
  filterOptions: AuditLogFilterOptions;
}

export const auditLogApi = {
  async getLogs(params?: AuditLogQueryParams): Promise<AuditLogPageResult> {
    const cleanParams = Object.fromEntries(
      Object.entries(params ?? {}).filter(([, v]) => v !== undefined && v !== ""),
    );
    const res = await httpClient.get<ApiResponse<BackendAuditLogPageResponse>>(
      API_ENDPOINTS.ADMIN_AUDIT_LOGS.BASE,
      { params: cleanParams },
    );
    const { page, filterOptions } = res.data.data;
    return {
      items: page.items,
      page: page.page,
      pageSize: page.pageSize,
      totalItems: page.totalItems,
      totalPages: page.totalPages,
      filterOptions,
    };
  },

  async getLogDetail(id: string): Promise<AuditLogItem> {
    const res = await httpClient.get<ApiResponse<AuditLogItem>>(
      API_ENDPOINTS.ADMIN_AUDIT_LOGS.DETAIL(id),
    );
    return res.data.data;
  },
};