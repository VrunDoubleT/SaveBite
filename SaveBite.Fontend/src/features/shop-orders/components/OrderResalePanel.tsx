import { Link, useParams } from "react-router-dom";
import { useEffect, useRef, useState } from "react";
import { httpClient, getApiErrorMessage } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";
import { checkoutTime } from "@/features/checkout/utils/checkoutPolicy";

import type { ResaleDecision } from "@/features/restock/restockApi";
import { formatCartMoney } from "@/features/cart/utils/formatCartMoney";

export function OrderResalePanel({
  shopId,
  orderId,
  revision,
  initialData,
  onChanged,
  readOnly = false,
}: {
  shopId: string;
  orderId: string;
  revision: number;
  initialData?: ResaleDecision;
  onChanged?: () => void;
  readOnly?: boolean;
}) {
  const { shopId: staffShopId } = useParams();
  const [data, setData] = useState<ResaleDecision | null>(initialData ?? null);
  const [quantities, setQuantities] = useState<Record<string, number | "">>({});
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const [attempt, setAttempt] = useState(0);
  const [now, setNow] = useState(Date.now);
  const lock = useRef(false);
  const url = `/orders/shop/${encodeURIComponent(shopId)}/${encodeURIComponent(orderId)}/resale-decision`;
  useEffect(() => {
    if (initialData) return;
    const abort = new AbortController();
    void httpClient
      .get<ApiResponse<ResaleDecision>>(url, { signal: abort.signal })
      .then(({ data: response }) => {
        if (abort.signal.aborted) return;
        setData(response.data);
        setError("");
      })
      .catch((cause) => {
        if (!abort.signal.aborted) setError(getApiErrorMessage(cause));
      });
    return () => abort.abort();
  }, [url, revision, attempt, initialData]);
  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(timer);
  }, []);
  const canDecide = !!data?.canDecide && !!data.deadlineAt && Date.parse(data.deadlineAt) > now;
  async function save() {
    if (!data || readOnly || !canDecide || lock.current) return;
    if (
      data.items.some(
        (item) =>
          item.status === "Pending" &&
          (!Number.isInteger(quantities[item.orderItemId] ?? 0) ||
            Number(quantities[item.orderItemId] ?? 0) < 0 ||
            Number(quantities[item.orderItemId] ?? 0) > item.quantity),
      )
    ) {
      setError("Restock quantity must be an integer between 0 and the returnable quantity.");
      return;
    }
    lock.current = true;
    setBusy(true);
    try {
      const result = await httpClient.post<ApiResponse<ResaleDecision>>(url, {
        items: data.items
          .filter((item) => item.status === "Pending")
          .map((item) => ({
            orderItemId: item.orderItemId,
            resaleQuantity: quantities[item.orderItemId] ?? 0,
          })),
      });
      setData({
        ...result.data.data,
        items: result.data.data.items.map((item) => ({
          ...item,
          imageUrl:
            item.imageUrl ??
            data.items.find((previous) => previous.orderItemId === item.orderItemId)?.imageUrl ??
            null,
        })),
      });
      onChanged?.();
      setError("");
    } catch (cause) {
      setError(getApiErrorMessage(cause));
      onChanged?.();
    } finally {
      lock.current = false;
      setBusy(false);
    }
  }
  if (data && !data.deadlineAt) return null;
  return (
    <section
      className={`rounded-xl border bg-white p-4 ${canDecide ? "border-orange-300 ring-1 ring-orange-100" : "border-neutral-200"}`}
    >
      <h2 className="text-sm font-bold">
        {readOnly ? "Stock decisions for this deal" : "Return quantities to sale"}
      </h2>
      {error && (
        <p role="alert" className="mt-2 text-sm text-red-700">
          {error}{" "}
          <button
            type="button"
            className="min-h-11 px-2 underline"
            disabled={busy}
            onClick={() => {
              setAttempt((value) => value + 1);
              onChanged?.();
            }}
          >
            Retry
          </button>
        </p>
      )}
      {!data && !error && (
        <p role="status" className="mt-2 text-sm">
          Loading return quantities…
        </p>
      )}
      {data && (
        <>
          <p className="mt-2 text-xs text-neutral-500">
            {readOnly
              ? "Recorded stock decisions. Pending items become unsellable when the decision window expires."
              : "Choose how many items can be sold again, up to the returnable quantity shown. The remaining quantity is recorded as unsellable. Decisions are final; unhandled items become unsellable after five minutes."}
          </p>
          {data.deadlineAt && (
            <p className="mt-2 text-xs font-semibold">
              {canDecide ? "Decide before" : "Decision window closed"}:{" "}
              {checkoutTime(data.deadlineAt)}
              {canDecide && (
                <span className="ml-2 text-orange-700">
                  ({Math.ceil(Math.max(0, Date.parse(data.deadlineAt) - now) / 1000)}s remaining)
                </span>
              )}
            </p>
          )}
          <div className="mt-3 space-y-3">
            {data.items.map((item) => (
              <div key={item.orderItemId} className="rounded-lg bg-neutral-50 p-3 text-sm">
                <div className="flex items-center gap-3">
                  {item.imageUrl ? (
                    <img
                      src={item.imageUrl}
                      alt={item.productName}
                      loading="lazy"
                      className="size-16 shrink-0 rounded-lg object-cover"
                    />
                  ) : (
                    <div className="flex size-16 shrink-0 items-center justify-center rounded-lg bg-neutral-200 text-center text-xs text-neutral-500">
                      No image
                    </div>
                  )}
                  <div className="min-w-0">
                    <p className="font-semibold">
                      {item.productName} × {item.quantity}
                    </p>
                    <p className="mt-1 text-xs text-neutral-500">
                      {item.variantName || "Standard"} · {formatCartMoney(item.unitPrice)} each
                    </p>
                    {item.flashDealId &&
                      item.flashDealId !== "00000000-0000-0000-0000-000000000000" && (
                        <Link
                          to={
                            staffShopId
                              ? `/staff/shops/${staffShopId}/flash-deals/${item.flashDealId}`
                              : `/store-owner/flash-deals/${item.flashDealId}`
                          }
                          className="mt-1 inline-flex min-h-8 items-center text-xs text-emerald-700 hover:underline"
                        >
                          View flash deal →
                        </Link>
                      )}
                  </div>
                </div>
                {item.status === "Pending" && canDecide && !readOnly ? (
                  <label className="mt-2 block text-xs">
                    Quantity to sell again (maximum {item.quantity})
                    <input
                      type="number"
                      min={0}
                      max={item.quantity}
                      step={1}
                      required
                      disabled={busy}
                      value={quantities[item.orderItemId] ?? 0}
                      onChange={(event) =>
                        setQuantities((values) => ({
                          ...values,
                          [item.orderItemId]:
                            event.target.value === ""
                              ? ""
                              : Number.isFinite(event.target.valueAsNumber)
                                ? Math.min(
                                    item.quantity,
                                    Math.max(0, Math.trunc(event.target.valueAsNumber)),
                                  )
                                : "",
                        }))
                      }
                      className="ml-3 min-h-11 w-24 rounded-lg border border-neutral-300 bg-white px-2"
                    />
                    <span className="ml-3">
                      Unsellable: {item.quantity - Number(quantities[item.orderItemId] ?? 0)}
                    </span>
                  </label>
                ) : (
                  <p className="mt-1 text-xs">
                    Returned to sale: {item.resaleQuantity} · Unsellable:{" "}
                    {item.status === "Pending"
                      ? canDecide
                        ? 0
                        : item.quantity
                      : item.unsellableQuantity}
                    {item.status === "Pending" && canDecide && (
                      <span> · Awaiting decision: {item.quantity}</span>
                    )}
                  </p>
                )}
              </div>
            ))}
          </div>
          {canDecide && !readOnly && (
            <button
              type="button"
              disabled={
                busy ||
                data.items.some(
                  (item) =>
                    item.status === "Pending" &&
                    (!Number.isInteger(quantities[item.orderItemId] ?? 0) ||
                      Number(quantities[item.orderItemId] ?? 0) < 0 ||
                      Number(quantities[item.orderItemId] ?? 0) > item.quantity),
                )
              }
              onClick={() => void save()}
              className="mt-3 min-h-11 rounded-lg bg-emerald-600 px-4 text-sm font-semibold text-white disabled:opacity-50"
            >
              {busy ? "Saving…" : "Confirm return quantities"}
            </button>
          )}
        </>
      )}
    </section>
  );
}
