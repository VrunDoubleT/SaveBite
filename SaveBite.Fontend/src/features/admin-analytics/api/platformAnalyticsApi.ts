import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";
import type { AnalyticsPeriodQuery } from "@/features/store-analytics/types/storeAnalytics.types";
import type {
  PlatformDashboardAnalytics,
  PlatformFeeStatistics,
  PlatformFlashDealStatistics,
  PlatformPaymentAndRefundStatistics,
  PlatformRevenueAndOrderStatistics,
  PlatformShopStatistics,
  PlatformUserStatistics,
} from "../types/platformAnalytics.types";

export const platformAnalyticsApi = {
  async getDashboard(params?: AnalyticsPeriodQuery): Promise<PlatformDashboardAnalytics> {
    const res = await httpClient.get<ApiResponse<PlatformDashboardAnalytics>>(
      API_ENDPOINTS.ADMIN_ANALYTICS.DASHBOARD,
      { params },
    );
    return res.data.data;
  },

  async getPlatformFees(params?: AnalyticsPeriodQuery): Promise<PlatformFeeStatistics> {
    const res = await httpClient.get<ApiResponse<PlatformFeeStatistics>>(
      API_ENDPOINTS.ADMIN_ANALYTICS.PLATFORM_FEES,
      { params },
    );
    return res.data.data;
  },

  async getUsers(params?: AnalyticsPeriodQuery): Promise<PlatformUserStatistics> {
    const res = await httpClient.get<ApiResponse<PlatformUserStatistics>>(
      API_ENDPOINTS.ADMIN_ANALYTICS.USERS,
      { params },
    );
    return res.data.data;
  },

  async getShops(params?: AnalyticsPeriodQuery): Promise<PlatformShopStatistics> {
    const res = await httpClient.get<ApiResponse<PlatformShopStatistics>>(
      API_ENDPOINTS.ADMIN_ANALYTICS.SHOPS,
      { params },
    );
    return res.data.data;
  },

  async getRevenueOrders(params?: AnalyticsPeriodQuery): Promise<PlatformRevenueAndOrderStatistics> {
    const res = await httpClient.get<ApiResponse<PlatformRevenueAndOrderStatistics>>(
      API_ENDPOINTS.ADMIN_ANALYTICS.REVENUE_ORDERS,
      { params },
    );
    return res.data.data;
  },

  async getFlashDeals(params?: AnalyticsPeriodQuery): Promise<PlatformFlashDealStatistics> {
    const res = await httpClient.get<ApiResponse<PlatformFlashDealStatistics>>(
      API_ENDPOINTS.ADMIN_ANALYTICS.FLASH_DEALS,
      { params },
    );
    return res.data.data;
  },

  async getPaymentsRefunds(
    params?: AnalyticsPeriodQuery,
  ): Promise<PlatformPaymentAndRefundStatistics> {
    const res = await httpClient.get<ApiResponse<PlatformPaymentAndRefundStatistics>>(
      API_ENDPOINTS.ADMIN_ANALYTICS.PAYMENTS_REFUNDS,
      { params },
    );
    return res.data.data;
  },
};