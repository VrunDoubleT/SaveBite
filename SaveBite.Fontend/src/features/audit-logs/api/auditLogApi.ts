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

function toBackendQueryParams(params?: AuditLogQueryParams) {
  const query = {
    page: params?.page,
    pageSize: params?.pageSize,
    search: params?.search?.trim(),
    actorType: params?.actorType || undefined,
    targetType: params?.targetType?.trim() || undefined,
    action: params?.action?.trim() || undefined,
    fromDate: params?.fromDate || undefined,
    toDate: params?.toDate || undefined,
    actorUserId: params?.actorUserId?.trim() || undefined,
    targetId: params?.targetId?.trim() || undefined,
    correlationId: params?.correlationId?.trim() || undefined,
  } satisfies AuditLogQueryParams;

  return Object.fromEntries(
    Object.entries(query).filter(([, value]) => value !== undefined && value !== ""),
  );
}

export const auditLogApi = {
  async getLogs(params?: AuditLogQueryParams): Promise<AuditLogPageResult> {
    const res = await httpClient.get<ApiResponse<BackendAuditLogPageResponse>>(
      API_ENDPOINTS.ADMIN_AUDIT_LOGS.BASE,
      { params: toBackendQueryParams(params) },
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
