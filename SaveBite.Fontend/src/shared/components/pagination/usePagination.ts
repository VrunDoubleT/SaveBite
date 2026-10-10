import { useState, useMemo, useEffect } from "react";

export interface UsePaginationOptions<T> {
  /** Array of items to paginate (optional if doing server-side pagination) */
  items?: T[];
  /** Number of items per page (default: 9) */
  initialPageSize?: number;
  /** Initial active page (default: 1) */
  initialPage?: number;
  /** Total count of items if performing server-side pagination */
  totalItemsCount?: number;
}

export interface UsePaginationResult<T> {
  /** Current active page (1-indexed) */
  currentPage: number;
  /** Change active page */
  setCurrentPage: (page: number) => void;
  /** Total number of pages */
  totalPages: number;
  /** Items per page */
  pageSize: number;
  /** Change items per page */
  setPageSize: (size: number) => void;
  /** Slice of items for the active page (if items was provided) */
  paginatedItems: T[];
  /** Total count of items */
  totalItems: number;
  /** Helper to navigate to next page */
  goToNextPage: () => void;
  /** Helper to navigate to previous page */
  goToPreviousPage: () => void;
  /** Helper to reset to page 1 */
  resetPage: () => void;
  /** Whether there is a previous page */
  hasNextPage: boolean;
  /** Whether there is a next page */
  hasPreviousPage: boolean;
}

/**
 * Reusable hook for managing pagination state and item slicing.
 * Supports both client-side slicing and server-side pagination state.
 */
export function usePagination<T = unknown>({
  items,
  initialPageSize = 9,
  initialPage = 1,
  totalItemsCount,
}: UsePaginationOptions<T> = {}): UsePaginationResult<T> {
  const [currentPage, setCurrentPage] = useState<number>(initialPage);
  const [pageSize, setPageSize] = useState<number>(initialPageSize);

  const totalItems = totalItemsCount ?? (items ? items.length : 0);
  const totalPages = Math.max(1, Math.ceil(totalItems / (pageSize || 1)));

  // Auto-clamp currentPage when totalPages shrinks (e.g., when applying filters)
  useEffect(() => {
    if (currentPage > totalPages) {
      setCurrentPage(Math.max(1, totalPages));
    }
  }, [totalPages, currentPage]);

  const safePage = Math.max(1, Math.min(currentPage, totalPages));

  // Sliced items for client-side pagination
  const paginatedItems = useMemo(() => {
    if (!items) return [];
    const startIndex = (safePage - 1) * pageSize;
    return items.slice(startIndex, startIndex + pageSize);
  }, [items, safePage, pageSize]);

  const handleSetPage = (page: number) => {
    const validPage = Math.max(1, Math.min(page, totalPages));
    setCurrentPage(validPage);
  };

  const handleSetPageSize = (newSize: number) => {
    setPageSize(newSize);
    setCurrentPage(1); // Reset to first page whenever page size changes
  };

  const goToNextPage = () => {
    if (currentPage < totalPages) {
      setCurrentPage((prev) => prev + 1);
    }
  };

  const goToPreviousPage = () => {
    if (currentPage > 1) {
      setCurrentPage((prev) => prev - 1);
    }
  };

  const resetPage = () => {
    setCurrentPage(1);
  };

  return {
    currentPage,
    setCurrentPage: handleSetPage,
    totalPages,
    pageSize,
    setPageSize: handleSetPageSize,
    paginatedItems,
    totalItems,
    goToNextPage,
    goToPreviousPage,
    resetPage,
    hasNextPage: currentPage < totalPages,
    hasPreviousPage: currentPage > 1,
  };
}
