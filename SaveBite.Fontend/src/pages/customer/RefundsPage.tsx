import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { ArrowRight, RotateCcw, Store } from "lucide-react";
import { APP_PATHS } from "@/app/router/paths";
import { getApiErrorMessage } from "@/shared/api";
import { AccountPagination as Pagination } from "@/shared/components/pagination";
import { useRealtimeSubscriptions } from "@/shared/realtime/realtimeContext";
import { REALTIME_EVENTS } from "@/shared/realtime/realtime.types";
import { refundApi } from "@/features/refunds/api/refundApi";
import { RefundStatusBadge } from "@/features/refunds/components/RefundStatusBadge";
import {
  AmountSummary,
  ButtonLink,
  Card,
  EmptyState,
  ErrorState,
  FilterBar,
  MediaImage,
  Skeleton,
} from "@/shared/components/ui";
import { checkoutTime } from "@/features/checkout/utils/checkoutPolicy";
import type { RefundPage } from "@/features/refunds/types/refund.types";
const statuses: Record<string, string> = {
  All: "All statuses",
  AwaitingCustomerDetails: "Bank details required",
  PendingVerification: "Waiting for shop review",
  AwaitingTransfer: "Waiting for transfer",
  Rejected: "Declined",
  Transferred: "Transferred",
  Withdrawn: "Withdrawn",
};
export function RefundsPage() {
  const [params, setParams] = useSearchParams();
  const rawPage = Number(params.get("page") ?? 1);
  const page = Number.isSafeInteger(rawPage) && rawPage > 0 ? rawPage : 1;
  const status = Object.hasOwn(statuses, params.get("status") ?? "")
    ? params.get("status")!
    : "All";
  const type = ["CustomerRequest", "ShopCancellation"].includes(params.get("type") ?? "")
    ? params.get("type")!
    : "All";
  const [revision, setRevision] = useState(0);
  const [result, setResult] = useState<{ key: string; data?: RefundPage; error?: string } | null>(
    null,
  );
  const realtime = useRealtimeSubscriptions();
  const key = `${page}:${status}:${type}:${revision}`;
  useEffect(() => {
    const abort = new AbortController();
    void refundApi
      .page(page, status, type, abort.signal)
      .then((data) => {
        if (abort.signal.aborted) return;
        if (page > Math.max(1, data.totalPages)) {
          setParams(
            (old) => {
              const next = new URLSearchParams(old);
              next.set("page", String(Math.max(1, data.totalPages)));
              return next;
            },
            { replace: true },
          );
          return;
        }
        setResult({ key, data });
      })
      .catch((e) => {
        if (!abort.signal.aborted) setResult({ key, error: getApiErrorMessage(e) });
      });
    return () => abort.abort();
  }, [key, page, status, type, setParams]);
  useEffect(() => {
    const off = realtime.on(REALTIME_EVENTS.CUSTOMER_ORDERS_CHANGED, (event) => {
      const data = event.data as { reason?: string } | null;
      if (["refund-changed", "reject", "confirmation-expired"].includes(data?.reason ?? ""))
        setRevision((value) => value + 1);
    });
    const connected = realtime.onConnected((reconnected) => {
      if (reconnected) setRevision((value) => value + 1);
    });
    return () => {
      off();
      connected();
    };
  }, [realtime]);
  const current = result?.key === key ? result : null;
  function filter(name: string, value: string) {
    setParams((old) => {
      const next = new URLSearchParams(old);
      next.delete("page");
      if (value === "All") next.delete(name);
      else next.set(name, value);
      return next;
    });
  }
  return (
    <div className="min-w-0 space-y-4">
      <FilterBar
        inline
        filters={[
          {
            label: "Refund type",
            value: type,
            options: [
              { value: "All", label: "All refunds" },
              { value: "CustomerRequest", label: "My requests" },
              { value: "ShopCancellation", label: "Shop cancellations" },
            ],
            onChange: (value) => filter("type", value),
          },
          {
            label: "Refund status",
            value: status,
            options: Object.entries(statuses).map(([value, label]) => ({ value, label })),
            onChange: (value) => filter("status", value),
          },
        ]}
      />
      <div className="flex flex-wrap items-center justify-between gap-2 text-caption text-text-secondary">
        <p role="status">
          {current?.data ? `${current.data.totalItems} refunds · GMT+7` : "Refund history · GMT+7"}
        </p>
        <Link
          to={APP_PATHS.ACCOUNT_ORDERS}
          className="relative inline-flex min-h-6 items-center font-semibold text-primary-700 after:absolute after:inset-x-0 after:-inset-y-2.5 hover:underline"
        >
          Request from order details
        </Link>
      </div>
      {current?.error && (
        <ErrorState
          title="Couldn't load refunds"
          description={current.error}
          onRetry={() => setRevision((value) => value + 1)}
        />
      )}
      {!current && (
        <div role="status" aria-label="Loading refunds" className="space-y-4">
          {[0, 1, 2].map((key) => (
            <Card padding="compact" key={key} className="space-y-4">
              <Skeleton className="h-12 w-2/3" />
              <Skeleton className="h-20" />
            </Card>
          ))}
        </div>
      )}
      {current?.data?.items.length === 0 && (
        <EmptyState
          icon={RotateCcw}
          title="No refunds found"
          description="Try another filter, or open an order to check refund eligibility."
          action={
            <ButtonLink to={APP_PATHS.ACCOUNT_ORDERS} variant="secondary">
              View orders
            </ButtonLink>
          }
        />
      )}
      {current?.data?.items.map((refund) => (
        <Card
          padding="none"
          key={refund.id}
          aria-label={`Refund for order ${refund.orderCode}`}
          className="overflow-hidden"
        >
          <header className="flex flex-wrap items-start justify-between gap-3 border-b border-border-default bg-primary-50/40 p-4 sm:p-5">
            <div className="flex w-full min-w-0 items-center gap-3 sm:w-auto sm:flex-1">
              <MediaImage
                src={refund.shopLogoUrl}
                alt=""
                fallbackIcon={Store}
                showFallbackLabel={false}
                className="size-10 shrink-0 rounded-md"
              />
              <div className="min-w-0">
                <h2 className="text-section font-semibold text-text-primary [overflow-wrap:anywhere]">
                  <Link
                    to={APP_PATHS.SHOP_DETAIL.replace(":id", encodeURIComponent(refund.shopId))}
                    className="hover:text-primary-700 hover:underline"
                  >
                    {refund.shopName}
                  </Link>
                </h2>
                <p className="mt-1 text-caption text-text-secondary">
                  Order{" "}
                  <Link
                    to={APP_PATHS.ORDER_DETAIL.replace(
                      ":orderId",
                      encodeURIComponent(refund.orderId),
                    )}
                    className="font-semibold text-primary-700 hover:underline"
                  >
                    {refund.orderCode}
                  </Link>
                </p>
              </div>
            </div>
            <RefundStatusBadge status={refund.status} overdue={refund.isOverdue} />
          </header>
          <div className="space-y-3 p-4 sm:p-5">
            <div className="flex flex-wrap justify-between gap-2 text-caption text-text-secondary">
              <span className="font-medium">
                {refund.type === "CustomerRequest" ? "Your request" : "Required shop refund"}
              </span>
              <time dateTime={refund.createdAt}>Requested {checkoutTime(refund.createdAt)}</time>
            </div>
            <AmountSummary
              amounts={[
                { label: "Requested amount", amount: refund.amount },
                { label: "Transferred", amount: refund.transferredAmount, tone: "primary" },
              ]}
            />
            {refund.status === "AwaitingCustomerDetails" && (
              <p className="rounded-md bg-accent-50 p-3 text-caption text-accent-700">
                Confirm your refund destination
                {refund.customerDetailsDeadlineAt
                  ? ` by ${checkoutTime(refund.customerDetailsDeadlineAt)} · GMT+7`
                  : " to continue"}
                .
              </p>
            )}
          </div>
          <footer className="flex justify-end border-t border-border-default px-4 py-3 sm:px-5">
            <ButtonLink
              to={APP_PATHS.REFUND_DETAIL.replace(":refundId", encodeURIComponent(refund.id))}
              variant="secondary"
              className="min-h-11 w-full sm:w-auto"
            >
              View refund details
              <ArrowRight size={16} aria-hidden="true" />
            </ButtonLink>
          </footer>
        </Card>
      ))}
      {current?.data && current.data.totalItems > 0 && (
        <Pagination
          currentPage={current.data.page}
          totalPages={current.data.totalPages}
          totalItems={current.data.totalItems}
          pageSize={current.data.pageSize}
          itemLabel="refunds"
          onPageChange={(value) =>
            setParams((old) => {
              const next = new URLSearchParams(old);
              next.set("page", String(value));
              return next;
            })
          }
        />
      )}
    </div>
  );
}
