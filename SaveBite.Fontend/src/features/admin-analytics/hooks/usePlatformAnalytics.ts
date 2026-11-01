import { useEffect, useState } from "react";
import { platformAnalyticsApi } from "../api/platformAnalyticsApi";
import type { PlatformDashboardAnalytics } from "../types/platformAnalytics.types";
import { useAnalyticsPeriod } from "@/features/store-analytics/hooks/useAnalyticsPeriod";
import { getApiErrorMessage } from "@/shared/api/httpClient";

/** The active tab now lives in the URL path (see utils/analyticsTabs.ts), not in state. */
export function usePlatformAnalytics() {
  const period = useAnalyticsPeriod();
  const [data, setData] = useState<PlatformDashboardAnalytics | null>(null);
  const [loading, setLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);

  const { rangeError, query } = period;
  const queryKey = JSON.stringify(query);

  useEffect(() => {
    if (rangeError) return;
    let ignore = false;
    async function load() {
      try {
        setLoading(true);
        const result = await platformAnalyticsApi.getDashboard(JSON.parse(queryKey));
        if (!ignore) {
          setData(result);
          setErrorMessage(null);
        }
      } catch (error) {
        if (!ignore) {
          setErrorMessage(getApiErrorMessage(error) || "Unable to load platform analytics.");
        }
      } finally {
        if (!ignore) setLoading(false);
      }
    }
    void load();
    return () => {
      ignore = true;
    };
  }, [queryKey, reloadKey, rangeError]);

  return {
    ...period,
    data,
    loading,
    errorMessage,
    clearError: () => setErrorMessage(null),
    refresh: () => setReloadKey((key) => key + 1),
  };
}