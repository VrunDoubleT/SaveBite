import { useEffect, useState } from "react";
import { storeAnalyticsApi } from "../api/storeAnalyticsApi";
import type { StoreDashboardAnalytics } from "../types/storeAnalytics.types";
import { getApiErrorMessage } from "@/shared/api/httpClient";
import { useAnalyticsPeriod } from "./useAnalyticsPeriod";

export function useStoreAnalytics() {
  const period = useAnalyticsPeriod();
  const [data, setData] = useState<StoreDashboardAnalytics | null>(null);
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
        const result = await storeAnalyticsApi.getDashboard(JSON.parse(queryKey));
        if (!ignore) {
          setData(result);
          setErrorMessage(null);
        }
      } catch (error) {
        if (!ignore) {
          setErrorMessage(getApiErrorMessage(error) || "Unable to load store analytics.");
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
