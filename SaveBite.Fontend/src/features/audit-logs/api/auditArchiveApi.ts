import { httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";
import {
  ARCHIVE_PAGE_SIZE,
  type AuditArchive,
  type AuditArchiveRow,
} from "../types/auditArchive.types";

const base = "/admin/audit-archives";
const timeout = 600_000;

export const auditArchiveApi = {
  async list(): Promise<AuditArchive[]> {
    const response = await httpClient.get<ApiResponse<AuditArchive[]>>(base);
    return response.data.data;
  },
  async create(month: string): Promise<ApiResponse<AuditArchive | null>> {
    const response = await httpClient.post<ApiResponse<AuditArchive | null>>(
      `${base}/${month.replace("-", "/")}`,
      null,
      { timeout },
    );
    return response.data;
  },
  async logs(id: string, page: number, signal: AbortSignal): Promise<{ items: AuditArchiveRow[] }> {
    const response = await httpClient.get<ApiResponse<{ items: AuditArchiveRow[] }>>(
      `${base}/${id}/logs`,
      { params: { page, pageSize: ARCHIVE_PAGE_SIZE }, signal, timeout },
    );
    return response.data.data;
  },
  async download(id: string): Promise<Blob> {
    const response = await httpClient.get<Blob>(`${base}/${id}/download`, {
      responseType: "blob",
      timeout,
    });
    return response.data;
  },
};
