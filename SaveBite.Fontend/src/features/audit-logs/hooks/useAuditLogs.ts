import { useCallback, useEffect, useRef, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { auditLogApi } from "../api/auditLogApi";
import type { AuditLogFilterOptions, AuditLogItem } from "../types/auditLog.types";
import { getApiErrorMessage } from "@/shared/api/httpClient";
import { useDebounce } from "@/shared/hooks/useDebounce";

export const AUDIT_LOG_PAGE_SIZE = 10;

// The page and the filters live in the URL, like the Trust Score lists:
// no `page` on the first visit, then ?page=N (and ?page=1 when going back to page 1).
const P_PAGE = "page";
const P_SEARCH = "q";
const P_ACTOR = "actor";
const P_TARGET = "type";
const P_ACTION = "action";
const P_FROM = "from";
const P_TO = "to";
const DATE_PATTERN = /^\d{4}-\d{2}-\d{2}$/;

type ActorType = "" | "User" | "System";

export function useAuditLogs() {
  const [searchParams, setSearchParams] = useSearchParams();

  const rawPage = Number(searchParams.get(P_PAGE));
  const page = Number.isInteger(rawPage) && rawPage >= 1 ? rawPage : 1;

  const urlSearch = searchParams.get(P_SEARCH) ?? "";
  const rawActor = searchParams.get(P_ACTOR);
  const actorType: ActorType = rawActor === "User" || rawActor === "System" ? rawActor : "";
  const targetType = searchParams.get(P_TARGET) ?? "";
  const action = searchParams.get(P_ACTION) ?? "";
  const rawFrom = searchParams.get(P_FROM);
  const rawTo = searchParams.get(P_TO);
  const fromDate = rawFrom && DATE_PATTERN.test(rawFrom) ? rawFrom : "";
  const toDate = rawTo && DATE_PATTERN.test(rawTo) ? rawTo : "";

  const [logs, setLogs] = useState<AuditLogItem[]>([]);
  const [filterOptions, setFilterOptions] = useState<AuditLogFilterOptions>({
    actions: [],
    targetTypes: [],
  });
  const [loading, setLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);
  const [totalItems, setTotalItems] = useState(0);
  const [totalPages, setTotalPages] = useState(1);
  const [selectedLog, setSelectedLog] = useState<AuditLogItem | null>(null);

  /** Writes filters to the URL. Any change except the page itself sends the list back to page 1. */
  const updateParams = useCallback(
    (changes: Record<string, string | null>, options?: { keepPage?: boolean; replace?: boolean }) => {
      setSearchParams(
        (previous) => {
          const next = new URLSearchParams(previous);
          for (const [key, value] of Object.entries(changes)) {
            if (value) next.set(key, value);
            else next.delete(key);
          }
          if (!options?.keepPage) next.delete(P_PAGE);
          return next;
        },
        { replace: options?.replace },
      );
    },
    [setSearchParams],
  );

  // The search box is typed into freely and written to the URL after a short pause.
  const [searchInput, setSearchInput] = useState(urlSearch);
  const debouncedSearch = useDebounce(searchInput, 400);
  const syncedSearch = useRef(urlSearch);

  useEffect(() => {
    if (debouncedSearch === syncedSearch.current) return;
    syncedSearch.current = debouncedSearch;
    updateParams({ [P_SEARCH]: debouncedSearch || null });
  }, [debouncedSearch, updateParams]);

  // Back/forward or "Clear filters" changes the URL: bring the box in line with it.
  useEffect(() => {
    if (urlSearch === syncedSearch.current) return;
    syncedSearch.current = urlSearch;
    setSearchInput(urlSearch);
  }, [urlSearch]);

  // A reversed date range is reported here instead of round-tripping to the API.
  const rangeError =
    fromDate && toDate && fromDate > toDate ? "From date must not be later than To date." : null;

  useEffect(() => {
    if (rangeError) return;
    let ignore = false;
    async function load() {
      try {
        setLoading(true);
        const result = await auditLogApi.getLogs({
          page,
          pageSize: AUDIT_LOG_PAGE_SIZE,
          search: urlSearch || undefined,
          actorType: actorType || undefined,
          targetType: targetType || undefined,
          action: action || undefined,
          fromDate: fromDate || undefined,
          toDate: toDate || undefined,
        });
        if (ignore) return;

        // A page beyond the last one (typed in the URL, or left over after the data shrank)
        // is corrected to the last page instead of showing an empty list.
        const lastPage = Math.max(result.totalPages, 1);
        if (page > lastPage) {
          updateParams({ [P_PAGE]: String(lastPage) }, { keepPage: true, replace: true });
          return;
        }

        setLogs(result.items);
        setTotalItems(result.totalItems);
        setTotalPages(result.totalPages);
        setFilterOptions(result.filterOptions);
      } catch (error) {
        if (!ignore) {
          setErrorMessage(getApiErrorMessage(error) || "Unable to load administrative audit logs.");
        }
      } finally {
        if (!ignore) setLoading(false);
      }
    }
    void load();
    return () => {
      ignore = true;
    };
  }, [page, urlSearch, actorType, targetType, action, fromDate, toDate, reloadKey, rangeError, updateParams]);

  const setPage = (next: number) => updateParams({ [P_PAGE]: String(next) }, { keepPage: true });
  const updateSearch = (value: string) => setSearchInput(value);
  const updateActorType = (value: ActorType) => updateParams({ [P_ACTOR]: value || null });
  const updateTargetType = (value: string) => updateParams({ [P_TARGET]: value || null });
  const updateAction = (value: string) => updateParams({ [P_ACTION]: value || null });
  const updateFromDate = (value: string) => updateParams({ [P_FROM]: value || null });
  const updateToDate = (value: string) => updateParams({ [P_TO]: value || null });
  const resetFilters = () => {
    setSearchParams(new URLSearchParams());
  };

  const hasActiveFilters = Boolean(
    searchInput || actorType || targetType || action || fromDate || toDate,
  );

  return {
    logs,
    filterOptions,
    loading,
    errorMessage: rangeError ?? errorMessage,
    clearError: () => setErrorMessage(null),
    page,
    setPage,
    totalItems,
    totalPages,
    search: searchInput,
    updateSearch,
    actorType,
    updateActorType,
    targetType,
    updateTargetType,
    action,
    updateAction,
    fromDate,
    updateFromDate,
    toDate,
    updateToDate,
    hasActiveFilters,
    resetFilters,
    selectedLog,
    setSelectedLog,
    refresh: () => setReloadKey((key) => key + 1),
  };
}