import { CircleAlert, Inbox, type LucideIcon } from "lucide-react";
import { useId, type ComponentProps, type ReactNode } from "react";
import { Button } from "./Controls";

const badgeTones = {
  primary: "bg-primary-50 text-primary-700",
  success: "bg-primary-50 text-primary-700",
  warning: "bg-accent-50 text-accent-700",
  danger: "bg-danger-soft text-danger-strong",
  info: "bg-info-soft text-info-strong",
  neutral: "bg-neutral-100 text-neutral-700",
};
export function Badge({
  tone = "neutral",
  className = "",
  ...props
}: ComponentProps<"span"> & { tone?: keyof typeof badgeTones }) {
  return (
    <span
      {...props}
      className={`inline-flex items-center gap-1 rounded-full px-3 py-1 text-caption font-medium ${badgeTones[tone]} ${className}`}
    />
  );
}
export function Card({ className = "", ...props }: ComponentProps<"section">) {
  return (
    <section
      {...props}
      className={`rounded-lg border border-border-default bg-bg-surface p-4 shadow-sm sm:p-6 ${className}`}
    />
  );
}
export function PageHeader({
  title,
  description,
  actions,
  breadcrumb,
}: {
  title: string;
  description?: string;
  actions?: ReactNode;
  breadcrumb?: ReactNode;
}) {
  return (
    <header className="space-y-3">
      {breadcrumb && (
        <nav aria-label="Breadcrumb" className="text-caption text-text-secondary">
          {breadcrumb}
        </nav>
      )}
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="min-w-0">
          <h1 className="text-page font-semibold text-text-primary">{title}</h1>
          {description && (
            <p className="mt-1 max-w-2xl text-body text-text-secondary">{description}</p>
          )}
        </div>
        {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
      </div>
    </header>
  );
}
export function EmptyState({
  title,
  description,
  icon: Icon = Inbox,
  action,
}: {
  title: string;
  description?: string;
  icon?: LucideIcon;
  action?: ReactNode;
}) {
  return (
    <div className="flex flex-col items-center gap-3 rounded-lg border border-dashed border-border-default bg-bg-surface p-8 text-center">
      <span className="flex size-10 items-center justify-center rounded-full bg-neutral-100 text-text-secondary">
        <Icon className="size-5" aria-hidden="true" />
      </span>
      <div>
        <p className="text-body font-semibold text-text-primary">{title}</p>
        {description && (
          <p className="mt-1 max-w-md text-body text-text-secondary">{description}</p>
        )}
      </div>
      {action}
    </div>
  );
}
export function ErrorState({
  title = "Unable to load data",
  description,
  onRetry,
}: {
  title?: string;
  description?: string;
  onRetry?: () => void;
}) {
  return (
    <div role="alert">
      <EmptyState
        title={title}
        description={description}
        icon={CircleAlert}
        action={
          onRetry && (
            <Button variant="secondary" onClick={onRetry}>
              Try again
            </Button>
          )
        }
      />
    </div>
  );
}
export function Skeleton({ className = "", ...props }: ComponentProps<"div">) {
  return (
    <div
      {...props}
      aria-hidden="true"
      className={`animate-pulse rounded-md bg-neutral-200 ${className}`}
    />
  );
}
export function StatCard({
  label,
  value,
  hint,
  icon: Icon,
}: {
  label: string;
  value: ReactNode;
  hint?: string;
  icon?: LucideIcon;
}) {
  return (
    <Card>
      <div className="flex items-center justify-between gap-3">
        <p className="text-body text-text-secondary">{label}</p>
        {Icon && <Icon className="size-5 text-text-muted" aria-hidden="true" />}
      </div>
      <p className="mt-3 break-words text-page font-semibold tabular-nums text-primary-700">
        {value}
      </p>
      {hint && <p className="mt-1 text-caption text-text-secondary">{hint}</p>}
    </Card>
  );
}
export interface DataColumn<T> {
  key: string;
  header: string;
  cell: (row: T) => ReactNode;
  className?: string;
}
export function DataTable<T>({
  columns,
  rows,
  rowKey,
  caption,
  loading,
  error,
  onRetry,
  emptyTitle = "No results",
  emptyDescription,
}: {
  columns: DataColumn<T>[];
  rows: T[];
  rowKey: (row: T) => string;
  caption: string;
  loading?: boolean;
  error?: string;
  onRetry?: () => void;
  emptyTitle?: string;
  emptyDescription?: string;
}) {
  if (loading)
    return (
      <div
        role="status"
        aria-label="Loading data"
        className="space-y-3 rounded-lg border border-border-default bg-bg-surface p-4"
      >
        <span className="sr-only">Loading data</span>
        {[0, 1, 2].map((i) => (
          <Skeleton key={i} className="h-10" />
        ))}
      </div>
    );
  if (error) return <ErrorState description={error} onRetry={onRetry} />;
  if (!rows.length) return <EmptyState title={emptyTitle} description={emptyDescription} />;
  return (
    <div
      className="max-h-96 overflow-auto rounded-lg border border-border-default bg-bg-surface shadow-sm"
      role="region"
      aria-label={caption}
      tabIndex={0}
    >
      <table className="w-full text-left text-body">
        <caption className="sr-only">{caption}</caption>
        <thead className="sticky top-0 z-10 bg-neutral-50 text-text-secondary">
          <tr>
            {columns.map((c) => (
              <th
                key={c.key}
                scope="col"
                className={`whitespace-nowrap px-4 py-3 font-medium ${c.className ?? ""}`}
              >
                {c.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-border-default">
          {rows.map((row) => (
            <tr key={rowKey(row)} className="transition-colors hover:bg-neutral-50">
              {columns.map((c) => (
                <td key={c.key} className={`px-4 py-3 ${c.className ?? ""}`}>
                  {c.cell(row)}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
export function Tabs({
  items,
  value,
  onChange,
  label,
  children,
  appearance = "line",
}: {
  appearance?: "line" | "cards";
  items: { value: string; label: string; icon?: LucideIcon; description?: string }[];
  value: string;
  onChange: (value: string) => void;
  label: string;
  children: ReactNode;
}) {
  const id = useId();
  return (
    <div className="space-y-4">
      <div
        role="tablist"
        aria-label={label}
        className={
          appearance === "cards"
            ? "grid grid-cols-2 gap-2 lg:grid-cols-4"
            : "flex gap-1 overflow-x-auto border-b border-border-default"
        }
        onKeyDown={(event) => {
          const index = items.findIndex((item) => item.value === value);
          const next =
            event.key === "ArrowRight"
              ? (index + 1) % items.length
              : event.key === "ArrowLeft"
                ? (index - 1 + items.length) % items.length
                : event.key === "Home"
                  ? 0
                  : event.key === "End"
                    ? items.length - 1
                    : -1;
          if (next < 0 || !items.length) return;
          event.preventDefault();
          onChange(items[next].value);
          event.currentTarget.querySelectorAll<HTMLButtonElement>('[role="tab"]')[next]?.focus();
        }}
      >
        {items.map((item) => (
          <button
            key={item.value}
            type="button"
            role="tab"
            id={`${id}-${item.value}`}
            aria-controls={`${id}-panel`}
            aria-selected={value === item.value}
            tabIndex={value === item.value ? 0 : -1}
            onClick={() => onChange(item.value)}
            className={
              appearance === "cards"
                ? `min-w-0 rounded-lg border p-3 text-left transition-colors ${value === item.value ? "border-primary-200 bg-primary-50 text-primary-800" : "border-border-default bg-bg-surface text-text-secondary hover:bg-neutral-50"}`
                : `shrink-0 border-b-2 px-4 py-3 text-body font-medium transition-colors ${value === item.value ? "border-primary-700 text-primary-700" : "border-transparent text-text-secondary hover:bg-neutral-100"}`
            }
          >
            <span
              className={
                appearance === "cards"
                  ? "flex min-h-6 items-start gap-2 text-body font-semibold"
                  : undefined
              }
            >
              {item.icon && <item.icon className="size-4 shrink-0" aria-hidden="true" />}
              {item.label}
            </span>
            {appearance === "cards" && item.description && (
              <span className="mt-1 block text-caption font-normal">{item.description}</span>
            )}
          </button>
        ))}
      </div>
      <div role="tabpanel" id={`${id}-panel`} aria-labelledby={`${id}-${value}`} tabIndex={0}>
        {children}
      </div>
    </div>
  );
}
