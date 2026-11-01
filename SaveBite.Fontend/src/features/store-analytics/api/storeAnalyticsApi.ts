import { API_ENDPOINTS, httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";
import type {
  AnalyticsPeriodQuery,
  StoreDashboardAnalytics,
  StoreFlashDealStatistics,
  StoreOrderStatistics,
  StorePaymentMethodRevenue,
  StoreRevenueOverview,
} from "../types/storeAnalytics.types";

export const storeAnalyticsApi = {
  async getDashboard(params?: AnalyticsPeriodQuery): Promise<StoreDashboardAnalytics> {
    const res = await httpClient.get<ApiResponse<StoreDashboardAnalytics>>(
      API_ENDPOINTS.STORE_ANALYTICS.DASHBOARD,
      { params },
    );
    return res.data.data;
  },

  async getRevenueOverview(params?: AnalyticsPeriodQuery): Promise<StoreRevenueOverview> {
    const res = await httpClient.get<ApiResponse<StoreRevenueOverview>>(
      API_ENDPOINTS.STORE_ANALYTICS.REVENUE_OVERVIEW,
      { params },
    );
    return res.data.data;
  },

  async getOrderStatistics(params?: AnalyticsPeriodQuery): Promise<StoreOrderStatistics> {
    const res = await httpClient.get<ApiResponse<StoreOrderStatistics>>(
      API_ENDPOINTS.STORE_ANALYTICS.ORDERS,
      { params },
    );
    return res.data.data;
  },

  async getFlashDealStatistics(params?: AnalyticsPeriodQuery): Promise<StoreFlashDealStatistics> {
    const res = await httpClient.get<ApiResponse<StoreFlashDealStatistics>>(
      API_ENDPOINTS.STORE_ANALYTICS.FLASH_DEALS,
      { params },
    );
    return res.data.data;
  },

  async getPaymentMethodRevenue(params?: AnalyticsPeriodQuery): Promise<StorePaymentMethodRevenue> {
    const res = await httpClient.get<ApiResponse<StorePaymentMethodRevenue>>(
      API_ENDPOINTS.STORE_ANALYTICS.PAYMENT_METHODS,
      { params },
    );
    return res.data.data;
  },
};