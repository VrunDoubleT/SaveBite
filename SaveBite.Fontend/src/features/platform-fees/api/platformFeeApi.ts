import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";
import type {
  PlatformFeeConfigResponse,
  UpdatePlatformFeeConfigRequest,
  PlatformFeeStatementSummary,
  PlatformFeeStatementDetail,
  PlatformFeePaymentLinkResponse,
  GetShopPlatformFeesParams,
  GetAdminPlatformFeesParams,
  RecordExternalPaymentRequest,
  PagedResult,
} from "../types/platformFee.types";

export const platformFeeApi = {
  // UC-148: View active platform fee config
  async getActiveConfig(): Promise<PlatformFeeConfigResponse> {
    const response = await httpClient.get<ApiResponse<PlatformFeeConfigResponse>>(
      API_ENDPOINTS.PLATFORM_FEES.ADMIN_CONFIG,
    );
    return response.data.data;
  },
  // View all fee configs (history + scheduled)
  async getAllConfigs(): Promise<PlatformFeeConfigResponse[]> {
    const response = await httpClient.get<ApiResponse<PlatformFeeConfigResponse[]>>(
      API_ENDPOINTS.PLATFORM_FEES.ADMIN_CONFIGS,
    );
    return response.data.data;
  },
  // Admin: View all platform fee statements across all shops
  async getAdminStatements(
    params?: GetAdminPlatformFeesParams,
  ): Promise<PagedResult<PlatformFeeStatementSummary>> {
    const response = await httpClient.get<ApiResponse<PagedResult<PlatformFeeStatementSummary>>>(
      API_ENDPOINTS.PLATFORM_FEES.ADMIN_STATEMENTS,
      {
        params,
      },
    );
    return response.data.data;
  },

  // Admin: View platform fee statement details for any shop
  async getAdminStatementDetail(statementId: string): Promise<PlatformFeeStatementDetail> {
    const response = await httpClient.get<ApiResponse<PlatformFeeStatementDetail>>(
      `${API_ENDPOINTS.PLATFORM_FEES.ADMIN_STATEMENTS}/${statementId}`,
    );
    return response.data.data;
  },
  // UC-149: Update platform fee config
  async updateConfig(payload: UpdatePlatformFeeConfigRequest): Promise<PlatformFeeConfigResponse> {
    const response = await httpClient.put<ApiResponse<PlatformFeeConfigResponse>>(
      API_ENDPOINTS.PLATFORM_FEES.ADMIN_CONFIG,
      payload,
    );
    return response.data.data;
  },

    // Admin: Cancel upcoming scheduled fee config
  async cancelScheduledConfig(configId: string): Promise<string> {
    const response = await httpClient.delete<ApiResponse<string>>(
      API_ENDPOINTS.PLATFORM_FEES.ADMIN_CANCEL_SCHEDULED_CONFIG(configId),
    );
    return response.data.data;
  },

  // Admin: Process overdue statements and suspend delinquent shops
  async processOverdue(): Promise<number> {
    const response = await httpClient.post<ApiResponse<number>>(
      API_ENDPOINTS.PLATFORM_FEES.ADMIN_PROCESS_OVERDUE,
    );
    return response.data.data;
  },

  // Admin: Generate monthly fee statements
  async generateMonthly(year: number, month: number): Promise<number> {
    const response = await httpClient.post<ApiResponse<number>>(
      API_ENDPOINTS.PLATFORM_FEES.ADMIN_GENERATE_MONTHLY,
      null,
      { params: { year, month } },
    );
    return response.data.data;
  },

  // Admin: Record external reconciliation payment
  async recordExternalPayment(
    statementId: string,
    payload: RecordExternalPaymentRequest,
  ): Promise<string> {
    const response = await httpClient.post<ApiResponse<string>>(
      API_ENDPOINTS.PLATFORM_FEES.ADMIN_RECONCILIATION(statementId),
      payload,
    );
    return response.data.data;
  },

  // UC-79: View monthly platform fee statements for shop
  async getShopStatements(
    shopId: string,
    params?: GetShopPlatformFeesParams,
  ): Promise<PagedResult<PlatformFeeStatementSummary>> {
    const response = await httpClient.get<ApiResponse<PagedResult<PlatformFeeStatementSummary>>>(
      API_ENDPOINTS.PLATFORM_FEES.SHOP_STATEMENTS(shopId),
      {
        params,
      },
    );
    return response.data.data;
  },

  // UC-80: View fee statement details
  async getStatementDetail(
    shopId: string,
    statementId: string,
  ): Promise<PlatformFeeStatementDetail> {
    const response = await httpClient.get<ApiResponse<PlatformFeeStatementDetail>>(
      API_ENDPOINTS.PLATFORM_FEES.SHOP_STATEMENT_DETAIL(shopId, statementId),
    );
    return response.data.data;
  },

  // UC-81: Pay platform fee online via PayOS
  async createPaymentLink(
    shopId: string,
    statementId: string,
  ): Promise<PlatformFeePaymentLinkResponse> {
    const response = await httpClient.post<ApiResponse<PlatformFeePaymentLinkResponse>>(
      API_ENDPOINTS.PLATFORM_FEES.SHOP_PAYMENT_LINK(shopId, statementId),
    );
    return response.data.data;
  },

  // UC-151: Simulate or call PayOS Webhook
  async simulateWebhook(orderCode: number, amount?: number): Promise<void> {
    await httpClient.post(API_ENDPOINTS.PLATFORM_FEES.WEBHOOK, {
      code: "00",
      desc: "success",
      data: {
        orderCode,
        amount,
      },
    });
  },
};
