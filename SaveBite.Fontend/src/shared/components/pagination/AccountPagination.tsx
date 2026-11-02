import { Pagination, type PaginationProps } from "./Pagination";

/** Account pages share the same four navigation controls and compact appearance. */
export function AccountPagination({
  className = "",
  ...props
}: Omit<PaginationProps, "size" | "showFirstLast" | "hideOnSinglePage">) {
  return (
    <Pagination
      {...props}
      size="md"
      showFirstLast
      hideOnSinglePage={false}
      className={`[&_button]:min-h-11 [&_button]:min-w-11 [&_button[aria-label^='Page_']]:hidden sm:[&_button[aria-label^='Page_']]:inline-flex ${className}`}
    />
  );
}
