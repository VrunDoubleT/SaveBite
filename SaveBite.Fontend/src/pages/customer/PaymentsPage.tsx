import { paymentTypeLabel } from "@/features/payments/utils/paymentLabels";
import { useEffect, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { CreditCard, Store, ArrowRight } from "lucide-react";
import { APP_PATHS } from "@/app/router/paths";
import { paymentApi } from "@/features/payments/api/paymentApi";
import type { CustomerPaymentPage } from "@/features/payments/types/paymentHistory.types";
import { PaymentStatusBadge } from "@/features/payments/components/PaymentStatusBadge";
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
import { Pagination } from "@/shared/components/pagination";
import { getApiErrorMessage } from "@/shared/api";
import { useAuthStore } from "@/shared/stores/authStore";

const statuses = ["All", "Pending", "Succeeded", "Failed", "Cancelled", "Expired"];

export function PaymentsPage() {
  const userId = useAuthStore((state) => state.user?.id);
  const [params, setParams] = useSearchParams();
  const requestedPage = Number(params.get("page") ?? 1);
  const page = Number.isSafeInteger(requestedPage) && requestedPage > 0 ? requestedPage : 1;
  const status = statuses.includes(params.get("status") ?? "") ? params.get("status")! : "All";
  const [retry, setRetry] = useState(0);
  const requestKey = `${userId}:${page}:${status}:${retry}`;
  const [result, setResult] = useState<{
    key: string;
    data?: CustomerPaymentPage;
    error?: string;
  } | null>(null);
  useEffect(() => {
    const abort = new AbortController();
    void paymentApi
      .page(page, status, abort.signal)
      .then((data) => {
        if (abort.signal.aborted) return;
        const lastPage = Math.max(1, data.totalPages);
        if (page > lastPage) {
          setParams(
            (previous) => {
              const next = new URLSearchParams(previous);
              next.set("page", String(lastPage));
              return next;
            },
            { replace: true },
          );
          return;
        }
        setResult({ key: requestKey, data });
      })
      .catch((error) => {
        if (!abort.signal.aborted) setResult({ key: requestKey, error: getApiErrorMessage(error) });
      });
    return () => abort.abort();
  }, [page, status, requestKey, setParams]);
  const current = result?.key === requestKey ? result : null;
  const data = current?.data;
  return (
    <div className="min-w-0 space-y-4">
      <FilterBar
        filters={[
          {
            label: "Payment status",
            value: status,
            options: statuses.map((value) => ({
              value,
              label: value === "Succeeded" ? "Paid" : value === "All" ? "All payments" : value,
            })),
            onChange: (value) =>
              setParams((previous) => {
                const next = new URLSearchParams(previous);
                next.delete("page");
                if (value === "All") next.delete("status");
                else next.set("status", value);
                return next;
              }),
          },
        ]}
      />
      <p role="status" className="text-caption text-text-secondary">
        {data ? `${data.totalItems} payments · Times are GMT+7` : "Payment history · GMT+7"}
      </p>
      {!current && (
        <div role="status" aria-label="Loading payments" className="space-y-4">
          {[0, 1, 2].map((key) => (
            <Card key={key} className="space-y-4">
              <Skeleton className="h-12 w-2/3" />
              <Skeleton className="h-20" />
            </Card>
          ))}
        </div>
      )}
      {current?.error && (
        <ErrorState
          title="Couldn't load payments"
          description={current.error}
          onRetry={() => setRetry((value) => value + 1)}
        />
      )}
      {data?.items.length === 0 && (
        <EmptyState
          icon={CreditCard}
          title="No payments found"
          description="Try a different status, or view your orders for payment information."
          action={
            <ButtonLink to={APP_PATHS.ACCOUNT_ORDERS} variant="secondary">
              View orders
            </ButtonLink>
          }
        />
      )}
      {data?.items.map((payment) => (
        <Card
          key={payment.id}
          aria-label={`Payment for order ${payment.orderCode}`}
          className="!p-0 overflow-hidden"
        >
          <div className="flex flex-wrap items-start justify-between gap-3 border-b border-border-default bg-primary-50/40 p-3 sm:p-4">
            <div className="flex w-full min-w-0 items-center gap-3 sm:w-auto sm:flex-1">
              <MediaImage
                src={payment.shopLogoUrl}
                alt=""
                fallbackIcon={Store}
                showFallbackLabel={false}
                className="size-10 shrink-0 rounded-md"
              />
              <div className="min-w-0">
                <h2 className="text-body font-semibold text-text-primary [overflow-wrap:anywhere]">
                  {payment.shopName}
                </h2>
                <p className="mt-1 text-caption text-text-secondary">Order {payment.orderCode}</p>
              </div>
            </div>
            <PaymentStatusBadge status={payment.status} />
          </div>
          <div className="space-y-3 p-3 sm:p-4">
            <div className="flex flex-wrap items-start justify-between gap-3">
              <p className="inline-flex items-center gap-2 text-body font-semibold text-text-primary">
                <CreditCard size={17} aria-hidden="true" className="text-secondary-700" />
                {paymentTypeLabel(payment.type)}
              </p>
              <div className="space-y-1 text-caption text-text-secondary">
                <p>Created {checkoutTime(payment.createdAt)}</p>
                {payment.paidAt && <p>Paid {checkoutTime(payment.paidAt)}</p>}
              </div>
            </div>
            <div className="grid items-center gap-3 sm:grid-cols-[minmax(0,1fr)_auto]">
              <AmountSummary
                amounts={[
                  { label: "Requested", amount: payment.amount },
                  { label: "Collected", amount: payment.collectedAmount, tone: "primary" },
                ]}
              />
              <div className="flex justify-end">
                <ButtonLink
                  to={`${APP_PATHS.ACCOUNT_PAYMENTS}/${encodeURIComponent(payment.id)}`}
                  variant="secondary"
                  className="min-h-11 w-full sm:w-auto"
                >
                  View details
                  <ArrowRight size={16} aria-hidden="true" />
                </ButtonLink>
              </div>
            </div>
          </div>
        </Card>
      ))}
      {data && data.totalPages > 0 && (
        <Pagination
          currentPage={data.page}
          totalPages={data.totalPages}
          totalItems={data.totalItems}
          pageSize={data.pageSize}
          itemLabel="payments"
          size="sm"
          onPageChange={(nextPage) =>
            setParams((previous) => {
              const next = new URLSearchParams(previous);
              next.set("page", String(nextPage));
              return next;
            })
          }
        />
      )}
    </div>
  );
}
