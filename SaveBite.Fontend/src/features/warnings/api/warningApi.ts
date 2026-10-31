import { API_ENDPOINTS } from "@/shared/api/endpoints";
import { httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";

import type { IssueViolationWarning, Warning, WarningPage } from "../types/warning.types";
import type { ModerationActionResult } from "@/shared/types/moderationAction.types";

export const warningApi = {
  async history(
    page: number,
    pageSize: number,
    signal?: AbortSignal,
    status = "all",
  ): Promise<WarningPage> {
    const response = await httpClient.get<ApiResponse<WarningPage>>(API_ENDPOINTS.WARNINGS.BASE, {
      params: {
        page,
        pageSize,
        status,
      },
      signal,
    });

    return response.data.data;
  },

  async details(warningId: string, signal?: AbortSignal): Promise<Warning> {
    const response = await httpClient.get<ApiResponse<Warning>>(
      API_ENDPOINTS.WARNINGS.DETAIL(warningId),
      {
        signal,
      },
    );

    return response.data.data;
  },

  async issueForProduct(
    productId: string,
    reason: string,
    requiredCorrection: string,
  ): Promise<ModerationActionResult> {
    const request: IssueViolationWarning = { reason, requiredCorrection };
    const response = await httpClient.post<ApiResponse<ModerationActionResult>>(
      API_ENDPOINTS.WARNINGS.ISSUE_FOR_PRODUCT(productId),
      request,
    );
    return response.data.data;
  },
};
