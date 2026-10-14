import { ChevronLeft, ChevronRight } from "lucide-react";

const secondaryButton =
  "inline-flex min-h-10 items-center justify-center gap-2 rounded-lg border border-border-default bg-white px-4 py-2.5 text-sm font-semibold text-neutral-700 transition hover:bg-neutral-50 focus-visible:outline-none focus-visible:ring-3 focus-visible:ring-primary-200 disabled:cursor-not-allowed disabled:opacity-50";

export function Pagination({
  page,
  pages,
  total,
  onPage,
}: {
  page: number;
  pages: number;
  total: number;
  onPage: (page: number) => void;
}) {
  const pageCount = Math.max(1, pages);
  const currentPage = Math.min(Math.max(1, page), pageCount);
  const visiblePages =
    pageCount <= 7
      ? Array.from({ length: pageCount }, (_, index) => index + 1)
      : [
          ...new Set([
            1,
            pageCount,
            ...Array.from(
              { length: 5 },
              (_, index) => Math.min(Math.max(currentPage - 2, 2), pageCount - 5) + index,
            ),
          ]),
        ].sort((a, b) => a - b);
  return (
    <nav
      aria-label="Pagination"
      className="flex flex-wrap items-center justify-between gap-3 py-4 text-sm text-text-secondary"
    >
      <span>
        {total} results · Page {currentPage}/{pageCount}
      </span>
      <div className="flex flex-wrap items-center gap-2">
        <button
          type="button"
          className={secondaryButton}
          disabled={currentPage <= 1}
          onClick={() => onPage(currentPage - 1)}
        >
          <ChevronLeft className="size-4" />
          Previous
        </button>
        {visiblePages.map((number, index) => (
          <span key={number} className="inline-flex items-center gap-2">
            {index > 0 && number - visiblePages[index - 1] > 1 && (
              <span aria-hidden="true" className="px-1 text-neutral-400">
                …
              </span>
            )}
            <button
              type="button"
              aria-label={`Go to page ${number}`}
              aria-current={number === currentPage ? "page" : undefined}
              className={`${secondaryButton} min-w-10 px-3! ${number === currentPage ? "border-primary-600! bg-primary-600! text-white! hover:bg-primary-700!" : ""}`}
              onClick={() => {
                if (number !== currentPage) onPage(number);
              }}
            >
              {number}
            </button>
          </span>
        ))}
        <button
          type="button"
          className={secondaryButton}
          disabled={currentPage >= pageCount}
          onClick={() => onPage(currentPage + 1)}
        >
          Next
          <ChevronRight className="size-4" />
        </button>
      </div>
    </nav>
  );
}
