import { subscribeProductSync } from "../utils/productSync";
import { useCallback, useEffect, useReducer, useState } from "react";
import { getApiErrorMessage } from "@/shared/api";

export function useProductResource<T>(
  key: string,
  load: (signal: AbortSignal) => Promise<T>,
  refreshOnProductChange = false,
) {
  const [revision, reload] = useReducer((n: number) => n + 1, 0);
  // Only list/summary consumers opt in. Refreshing workspace options would
  // unmount its Outlet and discard files and unsaved form values.
  useEffect(() => {
    if (refreshOnProductChange) return subscribeProductSync(() => reload());
  }, [refreshOnProductChange]);
  const requestKey = `${key}:${revision}`;
  const [result, setResult] = useState<{ key: string; data?: T; error?: string }>();
  useEffect(() => {
    const controller = new AbortController();
    void load(controller.signal).then(
      (data) => {
        if (!controller.signal.aborted) setResult({ key: requestKey, data });
      },
      (error: unknown) => {
        if (!controller.signal.aborted)
          setResult({ key: requestKey, error: getApiErrorMessage(error) });
      },
    );
    return () => controller.abort();
  }, [load, requestKey]);
  const current = result?.key === requestKey;
  return {
    data: current ? result.data : undefined,
    error: current ? result.error : undefined,
    loading: !current,
    reload: useCallback(() => reload(), []),
    setData: (data: T) => setResult({ key: requestKey, data }),
  };
}
