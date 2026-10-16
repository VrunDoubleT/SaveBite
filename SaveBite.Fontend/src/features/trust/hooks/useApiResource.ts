import { useCallback, useEffect, useState } from "react";
import { getApiErrorMessage } from "@/shared/api";
import { useAuthStore } from "@/shared/stores/authStore";

interface ResourceState<T> {
  /** Cache key (user + resource) this result belongs to. */
  cacheKey: string;
  version: number;
  data: T | null;
  error: string | null;
}

export interface ApiResource<T> {
  /** Best data available: fresh, else cached for this key, else the previous result. */
  data: T | null;
  error: string | null;
  /**
   * True only while there is nothing cached for this key yet, so the UI may show a skeleton
   * or dim the old content. Returning to an already-loaded view is NOT "loading".
   */
  isLoading: boolean;
  reload: () => void;
}

// In-memory, per-user cache. A revisited tab renders its last result immediately and
// refreshes it in the background (stale-while-revalidate), so switching tabs does not flash.
const cache = new Map<string, unknown>();

/**
 * Loads data with `fetcher` and reloads it whenever `key`/`fetcher` change (memoise the
 * fetcher with useCallback) or `reload()` is called. `key` must identify the request,
 * including every parameter that changes the result (page, filters...).
 */
export function useApiResource<T>(key: string, fetcher: () => Promise<T>): ApiResource<T> {
  const userId = useAuthStore((state) => state.user?.id);
  const cacheKey = `${userId ?? "anonymous"}:${key}`;

  const [state, setState] = useState<ResourceState<T> | null>(null);
  const [version, setVersion] = useState(0);

  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      try {
        const data = await fetcher();
        cache.set(cacheKey, data);
        if (!cancelled) setState({ cacheKey, version, data, error: null });
      } catch (error) {
        if (!cancelled) {
          setState((previous) => ({
            cacheKey,
            version,
            data: previous?.data ?? null,
            error: getApiErrorMessage(error),
          }));
        }
      }
    };

    void load();
    return () => {
      cancelled = true;
    };
  }, [fetcher, cacheKey, version]);

  const reload = useCallback(() => setVersion((value) => value + 1), []);

  const current =
    state !== null && state.cacheKey === cacheKey && state.version === version ? state : null;
  const cached = cache.get(cacheKey) as T | undefined;

  return {
    data: current?.data ?? cached ?? state?.data ?? null,
    error: current?.error ?? null,
    isLoading: current === null && cached === undefined,
    reload,
  };
}
