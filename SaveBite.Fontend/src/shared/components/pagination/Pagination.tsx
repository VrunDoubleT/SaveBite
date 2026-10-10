import { useMemo } from "react";
import { ChevronLeft, ChevronRight, ChevronsLeft, ChevronsRight } from "lucide-react";

export interface PaginationProps {
  /** Current active page (1-indexed) */
  currentPage: number;
  /** Total number of pages */
  totalPages: number;
  /** Callback invoked when the user selects a page */
  onPageChange: (page: number) => void;
  /** Total count of items across all pages (optional) */
  totalItems?: number;
  /** Number of items displayed per page (default: 9) */
  pageSize?: number;
  /** Number of adjacent page buttons visible on each side of the active page (default: 1) */
  siblingCount?: number;
  /** Whether to show the total items range text, e.g., "Showing 1–9 of 11 deals" (default: true if totalItems provided) */
  showTotalItems?: boolean;
  /** Whether to show first and last page quick jump buttons (default: false) */
  showFirstLast?: boolean;
  /** Whether to hide the pagination bar if totalPages <= 1 (default: false) */
  hideOnSinglePage?: boolean;
  /** Optional custom text / item label */
  itemLabel?: string;
  /** Additional CSS classes */
  className?: string;
  /** Size variant */
  size?: "sm" | "md" | "lg";
}

const DOTS = "...";

function range(start: number, end: number): number[] {
  const length = end - start + 1;
  return Array.from({ length }, (_, idx) => idx + start);
}

export function Pagination({
  currentPage,
  totalPages,
  onPageChange,
  totalItems,
  pageSize = 9,
  siblingCount = 1,
  showTotalItems = true,
  showFirstLast = false,
  hideOnSinglePage = false,
  itemLabel = "deals",
  className = "",
  size = "md",
}: PaginationProps) {
  const safeCurrentPage = Math.max(1, Math.min(currentPage, Math.max(1, totalPages)));

  // Generate pagination items array with smart ellipses
  const paginationRange = useMemo<(number | string)[]>(() => {
    const totalPageNumbers = siblingCount * 2 + 5;

    if (totalPageNumbers >= totalPages) {
      return range(1, Math.max(1, totalPages));
    }

    const leftSiblingIndex = Math.max(safeCurrentPage - siblingCount, 1);
    const rightSiblingIndex = Math.min(safeCurrentPage + siblingCount, totalPages);

    const shouldShowLeftDots = leftSiblingIndex > 2;
    const shouldShowRightDots = rightSiblingIndex < totalPages - 2;

    const firstPageIndex = 1;
    const lastPageIndex = totalPages;

    if (!shouldShowLeftDots && shouldShowRightDots) {
      const leftItemCount = 3 + 2 * siblingCount;
      const leftRange = range(1, leftItemCount);
      return [...leftRange, DOTS, totalPages];
    }

    if (shouldShowLeftDots && !shouldShowRightDots) {
      const rightItemCount = 3 + 2 * siblingCount;
      const rightRange = range(totalPages - rightItemCount + 1, totalPages);
      return [firstPageIndex, DOTS, ...rightRange];
    }

    if (shouldShowLeftDots && shouldShowRightDots) {
      const middleRange = range(leftSiblingIndex, rightSiblingIndex);
      return [firstPageIndex, DOTS, ...middleRange, DOTS, lastPageIndex];
    }

    return range(1, Math.max(1, totalPages));
  }, [totalPages, siblingCount, safeCurrentPage]);

  if (hideOnSinglePage && totalPages <= 1) {
    return null;
  }

  const sizeClasses = {
    sm: {
      btn: "h-7 min-w-7 px-2 text-xs",
      navBtn: "h-7 px-2.5 text-xs gap-1",
      iconSize: 13,
      gap: "gap-1",
    },
    md: {
      btn: "h-8 min-w-8 px-2.5 text-xs font-semibold",
      navBtn: "h-8 px-3 text-xs font-semibold gap-1.5",
      iconSize: 14,
      gap: "gap-1.5",
    },
    lg: {
      btn: "h-9 min-w-9 px-3 text-sm font-semibold",
      navBtn: "h-9 px-3.5 text-sm font-semibold gap-2",
      iconSize: 16,
      gap: "gap-2",
    },
  }[size];

  // Calculate item range
  const itemStart = pageSize ? (safeCurrentPage - 1) * pageSize + 1 : 1;
  const itemEnd =
    pageSize && totalItems !== undefined
      ? Math.min(safeCurrentPage * pageSize, totalItems)
      : undefined;

  return (
    <nav
      role="navigation"
      aria-label="Pagination Navigation"
      className={`flex flex-col items-center justify-between gap-4 rounded-xl border border-neutral-200/80 bg-white p-3.5 shadow-xs sm:flex-row sm:px-5 sm:py-3 ${className}`}
    >
      {/* Information text */}
      <div className="text-xs text-neutral-500">
        {showTotalItems && totalItems !== undefined ? (
          <span>
            Showing <strong className="text-neutral-800">{itemStart}–{itemEnd ?? totalItems}</strong> of{" "}
            <strong className="text-neutral-800">{totalItems}</strong> {itemLabel}
          </span>
        ) : (
          <span>
            Page <strong className="text-neutral-800">{safeCurrentPage}</strong> of{" "}
            <strong className="text-neutral-800">{totalPages}</strong>
          </span>
        )}
      </div>

      {/* Controls */}
      <div className={`flex items-center ${sizeClasses.gap}`}>
        {/* First page button */}
        {showFirstLast && (
          <button
            type="button"
            onClick={() => onPageChange(1)}
            disabled={safeCurrentPage <= 1}
            title="First page"
            aria-label="First page"
            className={`inline-flex items-center justify-center rounded-lg border border-neutral-200 bg-white font-medium text-neutral-600 shadow-2xs transition hover:border-emerald-300 hover:bg-neutral-50 hover:text-neutral-900 disabled:cursor-not-allowed disabled:opacity-40 disabled:hover:border-neutral-200 disabled:hover:bg-white ${sizeClasses.btn}`}
          >
            <ChevronsLeft size={sizeClasses.iconSize} />
          </button>
        )}

        {/* Previous page button */}
        <button
          type="button"
          onClick={() => onPageChange(safeCurrentPage - 1)}
          disabled={safeCurrentPage <= 1}
          aria-label="Previous page"
          className={`inline-flex items-center justify-center rounded-lg border border-neutral-200 bg-white text-neutral-700 shadow-2xs transition hover:border-emerald-300 hover:bg-neutral-50 hover:text-emerald-700 disabled:cursor-not-allowed disabled:opacity-40 disabled:hover:border-neutral-200 disabled:hover:bg-white disabled:hover:text-neutral-700 ${sizeClasses.navBtn}`}
        >
          <ChevronLeft size={sizeClasses.iconSize} />
          <span>Previous</span>
        </button>

        {/* Page buttons */}
        <div className="flex items-center gap-1">
          {paginationRange.map((pageNumber, idx) => {
            if (pageNumber === DOTS) {
              return (
                <span
                  key={`dots-${idx}`}
                  className="flex h-8 w-6 items-center justify-center text-xs font-semibold text-neutral-400"
                >
                  &#8230;
                </span>
              );
            }

            const page = Number(pageNumber);
            const isCurrent = page === safeCurrentPage;

            return (
              <button
                key={page}
                type="button"
                onClick={() => onPageChange(page)}
                aria-current={isCurrent ? "page" : undefined}
                aria-label={`Page ${page}`}
                className={`inline-flex items-center justify-center rounded-lg transition ${
                  sizeClasses.btn
                } ${
                  isCurrent
                    ? "bg-emerald-600 text-white shadow-2xs font-bold ring-2 ring-emerald-500/20"
                    : "border border-neutral-200 bg-white text-neutral-700 shadow-2xs hover:border-emerald-300 hover:bg-neutral-50 hover:text-emerald-700 font-medium"
                }`}
              >
                {page}
              </button>
            );
          })}
        </div>

        {/* Next page button */}
        <button
          type="button"
          onClick={() => onPageChange(safeCurrentPage + 1)}
          disabled={safeCurrentPage >= totalPages}
          aria-label="Next page"
          className={`inline-flex items-center justify-center rounded-lg border border-neutral-200 bg-white text-neutral-700 shadow-2xs transition hover:border-emerald-300 hover:bg-neutral-50 hover:text-emerald-700 disabled:cursor-not-allowed disabled:opacity-40 disabled:hover:border-neutral-200 disabled:hover:bg-white disabled:hover:text-neutral-700 ${sizeClasses.navBtn}`}
        >
          <span>Next</span>
          <ChevronRight size={sizeClasses.iconSize} />
        </button>

        {/* Last page button */}
        {showFirstLast && (
          <button
            type="button"
            onClick={() => onPageChange(totalPages)}
            disabled={safeCurrentPage >= totalPages}
            title="Last page"
            aria-label="Last page"
            className={`inline-flex items-center justify-center rounded-lg border border-neutral-200 bg-white font-medium text-neutral-600 shadow-2xs transition hover:border-emerald-300 hover:bg-neutral-50 hover:text-neutral-900 disabled:cursor-not-allowed disabled:opacity-40 disabled:hover:border-neutral-200 disabled:hover:bg-white ${sizeClasses.btn}`}
          >
            <ChevronsRight size={sizeClasses.iconSize} />
          </button>
        )}
      </div>
    </nav>
  );
}
