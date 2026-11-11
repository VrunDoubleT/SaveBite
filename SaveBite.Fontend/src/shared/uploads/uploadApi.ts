import { httpClient } from "@/shared/api/httpClient";
import type { ApiResponse } from "@/shared/types";

export interface UploadSession {
  id: string;
  status: string;
  expiresAt: string;
  assets: { id: string; status: string; url: string; fileName: string; metadata?: unknown }[];
}

export interface UploadedAsset {
  assetId: string;
  url: string;
  metadata?: unknown;
}

export const uploadApi = {
  async createSession(
    purpose: string,
    targetId?: string,
  ): Promise<Pick<UploadSession, "id" | "expiresAt">> {
    const response = await httpClient.post<ApiResponse<Pick<UploadSession, "id" | "expiresAt">>>(
      "/uploads/sessions",
      { purpose, targetId },
    );
    return response.data.data;
  },

  async getSession(id: string): Promise<UploadSession> {
    const response = await httpClient.get<ApiResponse<UploadSession>>(`/uploads/sessions/${id}`);
    return response.data.data;
  },

  async upload(
    sessionId: string,
    assetId: string,
    file: File,
    signal?: AbortSignal,
  ): Promise<UploadedAsset> {
    const body = new FormData();
    body.append("file", file);
    const response = await httpClient.post<ApiResponse<UploadedAsset>>(
      `/uploads/sessions/${sessionId}/assets/${assetId}`,
      body,
      { timeout: 120_000, signal },
    );
    return response.data.data;
  },

  async cancelSession(id: string): Promise<void> {
    await httpClient.delete(`/uploads/sessions/${id}`);
  },
};
