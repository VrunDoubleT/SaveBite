import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";
import type {
  PlatformFeeConfigResponse,
  PlatformPayOsConfigResponse,
  PlatformPayOsConfigHistoryItem,
  UpdatePlatformPayOsConfigRequest,
  UpdatePlatformFeeConfigRequest,
  PlatformFeeStatementSummary,
  PlatformFeeStatementDetail,
  PlatformFeePaymentLinkResponse,
  GetShopPlatformFeesParams,
  GetShopMonthlyPlatformFeeReportParams,
  ShopMonthlyPlatformFeeReport,
  ShopCurrentPlatformFeeRate,
  GetAdminPlatformFeesParams,
  PagedResult,
} from "../types/platformFee.types";
export const platformFeeApi = {
  async getPayOsConfig(): Promise<PlatformPayOsConfigResponse> {
    const response = await httpClient.get<ApiResponse<PlatformPayOsConfigResponse>>(
      API_ENDPOINTS.PLATFORM_FEES.ADMIN_PAYOS_CONFIG,
    );
    return response.data.data;
  },
  async updatePayOsConfig(payload: UpdatePlatformPayOsConfigRequest): Promise<PlatformPayOsConfigResponse> {
    const response = await httpClient.put<ApiResponse<PlatformPayOsConfigResponse>>(
      API_ENDPOINTS.PLATFORM_FEES.ADMIN_PAYOS_CONFIG,
      payload,
    );
    return response.data.data;
  },
  async getPayOsConfigHistory(): Promise<PlatformPayOsConfigHistoryItem[]> {
    const response = await httpClient.get<ApiResponse<PlatformPayOsConfigHistoryItem[]>>(
      API_ENDPOINTS.PLATFORM_FEES.ADMIN_PAYOS_CONFIG_HISTORY,
    );
    return response.data.data;
  },
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
  async cancelScheduledConfig(configId: string, reason: string): Promise<string> {
    const response = await httpClient.delete<ApiResponse<string>>(
      API_ENDPOINTS.PLATFORM_FEES.ADMIN_CANCEL_SCHEDULED_CONFIG(configId),
      { data: { reason } },
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

  async getShopMonthlyReport(
    shopId: string,
    params: GetShopMonthlyPlatformFeeReportParams,
  ): Promise<ShopMonthlyPlatformFeeReport> {
    const response = await httpClient.get<ApiResponse<ShopMonthlyPlatformFeeReport>>(
      API_ENDPOINTS.PLATFORM_FEES.SHOP_MONTHLY_REPORT(shopId),
      { params },
    );
    return response.data.data;
  },

  async getShopCurrentRate(shopId: string): Promise<ShopCurrentPlatformFeeRate> {
    const response = await httpClient.get<ApiResponse<ShopCurrentPlatformFeeRate>>(
      API_ENDPOINTS.PLATFORM_FEES.SHOP_CURRENT_RATE(shopId),
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
