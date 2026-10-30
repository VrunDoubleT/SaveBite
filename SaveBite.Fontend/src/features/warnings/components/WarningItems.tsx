import { useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import {
  AlertCircle,
  ChevronDown,
  Clock,
  Loader2,
  Package,
  Pencil,
  Tag,
  Trash2,
  TriangleAlert,
  Wrench,
} from "lucide-react";
import type { Warning } from "../types/warning.types";
// Adjust this import to wherever your product API lives.
import { productApi } from "@/features/products/api/productApi";

/* ---------- Helpers ---------- */

const PRODUCTS_PATH = "/store-owner/products";

const clockFormatter = new Intl.DateTimeFormat("vi-VN", { timeStyle: "short" });
const dayLabelFormatter = new Intl.DateTimeFormat("vi-VN", {
  weekday: "long",
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
});

const dayKey = (date: Date) => `${date.getFullYear()}-${date.getMonth()}-${date.getDate()}`;

const formatClock = (value: string | null | undefined) => {
  const date = value ? new Date(value) : null;
  return date && !Number.isNaN(date.getTime()) ? clockFormatter.format(date) : "—";
};

/** "PRODUCT_HIDDEN" / "productHidden" -> "Product hidden" */
const formatActionLabel = (action: string | null | undefined) => {
  if (!action) return "—";
  const text = action
    .replace(/[_-]+/g, " ")
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .trim()
    .toLowerCase();
  return text.charAt(0).toUpperCase() + text.slice(1);
};

type WarningGroup = { key: string; label: string; isToday: boolean; items: Warning[] };

/** Groups warnings by calendar day, keeping the order the API returned them in. */
function groupByDay(items: Warning[]): WarningGroup[] {
  const today = new Date();
  const yesterday = new Date(today);
  yesterday.setDate(today.getDate() - 1);

  const groups = new Map<string, WarningGroup>();

  for (const item of items) {
    const date = new Date(item.createdAt);
    const valid = !Number.isNaN(date.getTime());
    const key = valid ? dayKey(date) : "unknown";

    if (!groups.has(key)) {
      const label = !valid
        ? "Unknown date"
        : key === dayKey(today)
          ? "Today"
          : key === dayKey(yesterday)
            ? "Yesterday"
            : dayLabelFormatter.format(date);
      groups.set(key, { key, label, isToday: valid && key === dayKey(today), items: [] });
    }
    groups.get(key)!.items.push(item);
  }

  return [...groups.values()];
}

type WarningContent = {
  product: string | null;
  productId: string | null;
  category: string | null;
  correction: string | null;
};

const EMPTY_CONTENT: WarningContent = {
  product: null,
  productId: null,
  category: null,
  correction: null,
};

const asText = (value: unknown) => (typeof value === "string" && value ? value : null);

/** Historical audit JSON may have a different shape, so every field is optional. */
function parseWarningContent(item: Warning): WarningContent {
  if (!item.newValuesJson) return EMPTY_CONTENT;
  try {
    const data: unknown = JSON.parse(item.newValuesJson);
    if (data && typeof data === "object") {
      const fields = data as Record<string, unknown>;
      const product = asText(fields.ProductName);
      return {
        product,
        // Prefer the id stored in the audit JSON; fall back to the warning's entityId.
        productId:
          asText(fields.ProductId) ??
          (product ? asText((item as unknown as { entityId?: unknown }).entityId) : null),
        category: asText(fields.CategoryName) ?? asText(fields.Category),
        correction: asText(fields.RequiredCorrection),
      };
    }
  } catch {
    /* ignore malformed JSON */
  }
  return EMPTY_CONTENT;
}

/** The API answered "not found" (axios-style or fetch-wrapper-style error). */
const isNotFound = (err: unknown) => {
  const e = err as { response?: { status?: number }; status?: number } | null;
  return e?.response?.status === 404 || e?.status === 404;
};

/** Some backends soft-delete and still return the product with a flag. */
const isSoftDeleted = (product: unknown) =>
  Boolean((product as { isDeleted?: boolean } | null)?.isDeleted);

type ProductStatus = "idle" | "checking" | "deleted" | "error";

/* ---------- Styles ---------- */

const focusRing =
  "focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-700";

const solidPill = `inline-flex items-center justify-center gap-2 rounded-full bg-primary-700 px-5 py-2.5 text-[13px] font-medium text-white shadow-sm transition-all hover:bg-primary-700/90 hover:shadow-md active:scale-[0.98] ${focusRing}`;

/* ---------- Row + expanded detail ---------- */

function WarningRow({
  item,
  open,
  onToggle,
}: {
  item: Warning;
  open: boolean;
  onToggle: () => void;
}) {
  const content = parseWarningContent(item);
  const { productId, correction } = content;
  const showEdit = Boolean(content.product || productId);
  const title = content.product || formatActionLabel(item.action);
  const panelId = `warning-panel-${item.id}`;

  const navigate = useNavigate();
  const [status, setStatus] = useState<ProductStatus>("idle");
  const deleted = status === "deleted";

  /** Make sure the product still exists before sending the user to its edit page. */
  const handleEdit = async () => {
    if (!productId || status === "checking") return;
    setStatus("checking");
    try {
      const product = await productApi.get(productId);
      if (isSoftDeleted(product)) {
        setStatus("deleted");
        return;
      }
      navigate(`${PRODUCTS_PATH}/${productId}`);
    } catch (err) {
      setStatus(isNotFound(err) ? "deleted" : "error");
    }
  };

  return (
    <li className="relative">
      {/* Timeline dot */}
      <span
        aria-hidden="true"
        className={`absolute -left-6 top-[30px] size-[11px] rounded-full border-2 border-white ring-1 transition-colors ${
          open ? "bg-amber-500 ring-amber-300" : "bg-amber-300 ring-amber-200"
        }`}
      />

      <div
        className={`overflow-hidden rounded-3xl border bg-white transition-all duration-200 ${
          open
            ? "border-amber-200 shadow-[0_8px_30px_-12px_rgba(217,119,6,0.35)]"
            : "border-border-default shadow-[0_1px_2px_rgba(16,24,40,0.04)] hover:border-neutral-300 hover:shadow-[0_6px_20px_-10px_rgba(16,24,40,0.18)]"
        }`}
      >
        <button
          type="button"
          onClick={onToggle}
          aria-expanded={open}
          aria-controls={panelId}
          className={`group flex w-full items-center gap-4 p-4 text-left sm:px-5 ${focusRing} focus-visible:-outline-offset-2`}
        >
          <span
            className={`flex size-11 shrink-0 items-center justify-center rounded-2xl ring-1 ring-inset transition-colors ${
              open
                ? "bg-amber-100 text-amber-700 ring-amber-200"
                : "bg-amber-50 text-amber-600 ring-amber-100"
            }`}
          >
            <Package className="size-5" aria-hidden="true" />
          </span>

          <span className="min-w-0 flex-1 space-y-1.5">
            <span className="flex min-w-0 items-center gap-2">
              <span
                className="truncate text-[15px] font-semibold tracking-[-0.005em] text-text-primary"
                title={title}
              >
                {title}
              </span>
              {deleted && (
                <span className="inline-flex shrink-0 items-center gap-1 rounded-full bg-neutral-100 px-2 py-0.5 text-xs font-medium text-text-secondary">
                  <Trash2 className="size-3" aria-hidden="true" />
                  Deleted
                </span>
              )}
            </span>
            <span className="flex flex-wrap items-center gap-x-2.5 gap-y-1.5">
              <span className="inline-flex items-center gap-1 text-xs font-medium text-amber-700">
                <TriangleAlert className="size-3.5" aria-hidden="true" />
                {formatActionLabel(item.action)}
              </span>
              {content.category && (
                <span className="inline-flex min-w-0 max-w-full items-center gap-1 rounded-full bg-primary-50 px-2.5 py-0.5 text-xs font-medium text-primary-700">
                  <Tag className="size-3 shrink-0" aria-hidden="true" />
                  <span className="truncate">{content.category}</span>
                </span>
              )}
            </span>
          </span>

          <time
            dateTime={item.createdAt}
            className="inline-flex shrink-0 items-center gap-1.5 rounded-full bg-neutral-100 px-2.5 py-1 text-xs font-medium tabular-nums text-text-secondary"
          >
            <Clock className="size-3.5" aria-hidden="true" />
            {formatClock(item.createdAt)}
          </time>

          <span
            aria-hidden="true"
            className={`flex size-8 shrink-0 items-center justify-center rounded-full border transition-colors ${
              open
                ? "border-neutral-900 bg-neutral-900 text-white"
                : "border-border-default bg-white text-text-secondary group-hover:bg-neutral-50"
            }`}
          >
            <ChevronDown
              className={`size-4 transition-transform duration-200 motion-reduce:transition-none ${
                open ? "rotate-180" : ""
              }`}
            />
          </span>
        </button>

        {open && (
          <div id={panelId} className="border-t border-amber-100 bg-neutral-50/60 p-4 sm:p-5">
            <div className={`grid gap-3 ${correction ? "lg:grid-cols-2" : ""}`}>
              <section
                aria-labelledby={`${panelId}-reason`}
                className="space-y-3 rounded-2xl border border-border-default bg-white p-4"
              >
                <h3
                  id={`${panelId}-reason`}
                  className="flex items-center gap-2 text-[13px] font-semibold text-text-primary"
                >
                  <span className="flex size-7 items-center justify-center rounded-full bg-red-50 text-red-600">
                    <AlertCircle className="size-4" aria-hidden="true" />
                  </span>
                  Reason
                </h3>
                <p className="max-w-prose whitespace-pre-wrap break-words text-sm leading-6 text-text-secondary">
                  {item.reason || "No reason provided"}
                </p>
              </section>

              {correction && (
                <section
                  aria-labelledby={`${panelId}-fix`}
                  className="space-y-3 rounded-2xl border border-amber-200 bg-amber-50 p-4"
                >
                  <h3
                    id={`${panelId}-fix`}
                    className="flex items-center gap-2 text-[13px] font-semibold text-amber-900"
                  >
                    <span className="flex size-7 items-center justify-center rounded-full bg-amber-100 text-amber-700">
                      <Wrench className="size-4" aria-hidden="true" />
                    </span>
                    What you need to fix
                  </h3>
                  <p className="max-w-prose whitespace-pre-wrap break-words text-sm leading-6 text-amber-950">
                    {correction}
                  </p>
                </section>
              )}
            </div>

            {deleted ? (
              <div
                role="status"
                className="mt-4 flex flex-wrap items-center justify-between gap-3 rounded-2xl border border-border-default bg-white p-4"
              >
                <div className="flex min-w-0 items-start gap-3">
                  <span className="flex size-8 shrink-0 items-center justify-center rounded-full bg-neutral-100 text-text-secondary">
                    <Trash2 className="size-4" aria-hidden="true" />
                  </span>
                  <div className="min-w-0">
                    <p className="text-sm font-semibold text-text-primary">
                      This product has been deleted
                    </p>
                    <p className="text-sm leading-6 text-text-secondary">
                      It's no longer in your shop, so there's nothing left to edit.
                    </p>
                  </div>
                </div>
                <Link to={PRODUCTS_PATH} className={solidPill}>
                  Go to products
                </Link>
              </div>
            ) : (
              showEdit && (
                <div className="mt-4 flex flex-col items-end gap-2">
                  {productId ? (
                    <button
                      type="button"
                      onClick={() => void handleEdit()}
                      disabled={status === "checking"}
                      className={`${solidPill} disabled:cursor-wait disabled:opacity-70`}
                    >
                      {status === "checking" ? (
                        <Loader2
                          className="size-4 animate-spin motion-reduce:animate-none"
                          aria-hidden="true"
                        />
                      ) : (
                        <Pencil className="size-4" aria-hidden="true" />
                      )}
                      {status === "checking" ? "Checking…" : "Edit product"}
                    </button>
                  ) : (
                    <Link to={PRODUCTS_PATH} className={solidPill}>
                      <Pencil className="size-4" aria-hidden="true" />
                      Go to products
                    </Link>
                  )}
                  {status === "error" && (
                    <p role="alert" className="text-xs text-red-600">
                      We couldn't check this product. Try again.
                    </p>
                  )}
                </div>
              )
            )}
          </div>
        )}
      </div>
    </li>
  );
}

/* ---------- List ---------- */

/** Warnings grouped by day. Click a row to expand its detail; one row is open at a time. */
export function WarningItems({ items }: { items: Warning[] }) {
  const [openId, setOpenId] = useState<string | null>(null);
  const groups = useMemo(() => groupByDay(items), [items]);

  return (
    <div className="space-y-9">
      {groups.map((group) => (
        <section key={group.key} aria-label={group.label} className="space-y-4">
          <div className="flex items-center gap-3">
            <h2
              className={`rounded-full px-3 py-1 text-[13px] font-semibold ${
                group.isToday ? "bg-primary-700 text-white" : "bg-neutral-100 text-text-secondary"
              }`}
            >
              {group.label}
            </h2>
            <span className="h-px flex-1 bg-border-default" aria-hidden="true" />
            <span className="text-xs tabular-nums text-text-muted">
              {group.items.length} {group.items.length === 1 ? "warning" : "warnings"}
            </span>
          </div>

          <ul className="relative space-y-3 pl-6 before:absolute before:bottom-6 before:left-[5px] before:top-6 before:w-px before:bg-border-default">
            {group.items.map((item) => (
              <WarningRow
                key={item.id}
                item={item}
                open={openId === item.id}
                onToggle={() => setOpenId((cur) => (cur === item.id ? null : item.id))}
              />
            ))}
          </ul>
        </section>
      ))}
    </div>
  );
}
