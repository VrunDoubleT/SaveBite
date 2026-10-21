import { useCallback } from "react";
import { useSearchParams } from "react-router-dom";

/** Items per page for every paged list on the Trust Score screens. */
export const TRUST_PAGE_SIZE = 10;

const PAGE_PARAM = "page";

export function useListParams<F extends string>(filterParam: string, allowedFilters: readonly F[]) {
  const [searchParams, setSearchParams] = useSearchParams();

  const rawPage = Number(searchParams.get(PAGE_PARAM));
  const page = Number.isInteger(rawPage) && rawPage >= 1 ? rawPage : 1;

  const rawFilter = searchParams.get(filterParam);
  const filter = allowedFilters.find((value) => value === rawFilter) ?? null;

  const setPage = useCallback(
    (nextPage: number) => {
      setSearchParams((previous) => {
        const next = new URLSearchParams(previous);
        next.set(PAGE_PARAM, String(nextPage));
        return next;
      });
    },
    [setSearchParams],
  );

  const setFilter = useCallback(
    (value: F | null) => {
      setSearchParams((previous) => {
        const next = new URLSearchParams(previous);
        next.delete(PAGE_PARAM);
        if (value) next.set(filterParam, value);
        else next.delete(filterParam);
        return next;
      });
    },
    [filterParam, setSearchParams],
  );

  /** Query string of the current list on `targetPage` (used to correct an out-of-range page). */
  const pageSearch = useCallback(
    (targetPage: number) => {
      const next = new URLSearchParams(searchParams);
      next.set(PAGE_PARAM, String(targetPage));
      const query = next.toString();
      return query ? `?${query}` : "";
    },
    [searchParams],
  );

  return { page, filter, setPage, setFilter, pageSearch };
}