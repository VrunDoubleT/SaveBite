import { useEffect, useRef, useState } from "react";
import { useParams } from "react-router-dom";
import { Store } from "lucide-react";
import { APP_PATHS } from "@/app/router/paths";
import { ButtonLink, Card, ErrorState, MediaImage, Skeleton } from "@/shared/components/ui";
import { getApiErrorMessage } from "@/shared/api";
import { useRealtimeSubscriptions } from "@/shared/realtime/realtimeContext";
import { REALTIME_EVENTS } from "@/shared/realtime/realtime.types";
import { refundApi } from "@/features/refunds/api/refundApi";
import { CustomerRefundDetailContent } from "@/features/refunds/components/CustomerRefundDetailContent";
import { RefundStatusBadge } from "@/features/refunds/components/RefundStatusBadge";
import type { Refund } from "@/features/refunds/types/refund.types";
import { checkoutTime } from "@/features/checkout/utils/checkoutPolicy";

export function CustomerRefundDetailPage() {
  const { refundId = "" } = useParams();
  return <RefundDetail key={refundId} refundId={refundId} />;
}
function RefundDetail({ refundId }: { refundId: string }) {
  const [refund, setRefund] = useState<Refund | null>(null);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const [revision, setRevision] = useState(0);
  const operation = useRef(false);
  const alive = useRef(true);
  const realtime = useRealtimeSubscriptions();
  useEffect(() => {
    alive.current = true;
    return () => {
      alive.current = false;
    };
  }, []);
  useEffect(() => {
    if (busy) return;
    const abort = new AbortController();
    void refundApi
      .detail(refundId, abort.signal)
      .then((data) => {
        if (!abort.signal.aborted) {
          setRefund(data);
          setError("");
        }
      })
      .catch((e) => {
        if (!abort.signal.aborted) setError(getApiErrorMessage(e));
      });
    return () => abort.abort();
  }, [refundId, revision, busy]);
  useEffect(() => {
    const off = realtime.on(REALTIME_EVENTS.CUSTOMER_ORDERS_CHANGED, (event) => {
      const data = event.data as { orderId?: string; reason?: string } | null;
      if (data?.orderId === refund?.orderId && data?.reason === "refund-changed")
        setRevision((old) => old + 1);
    });
    const connected = realtime.onConnected((again) => {
      if (again) setRevision((old) => old + 1);
    });
    return () => {
      off();
      connected();
    };
  }, [realtime, refund?.orderId]);
  async function mutate(action: () => Promise<Refund>) {
    if (operation.current) return;
    operation.current = true;
    setBusy(true);
    setError("");
    try {
      const changed = await action();
      if (alive.current) setRefund(changed);
    } catch (e) {
      if (alive.current) setError(getApiErrorMessage(e));
    } finally {
      operation.current = false;
      if (alive.current) setBusy(false);
    }
  }
  return (
    <div className="w-full min-w-0 space-y-4">
      <ButtonLink to={APP_PATHS.ACCOUNT_REFUNDS} variant="ghost" className="min-h-11 !px-0">
        All refunds
      </ButtonLink>
      {error && (
        <ErrorState
          title="Couldn't load refund details"
          description={error}
          onRetry={busy ? undefined : () => setRevision((old) => old + 1)}
        />
      )}
      {!refund && !error && (
        <div role="status" aria-label="Loading refund details" className="space-y-4">
          <Skeleton className="h-24" />
          <Skeleton className="h-48" />
          <Skeleton className="h-80" />
        </div>
      )}
      {refund && (
        <>
          <Card
            aria-label="Refund details"
            className="flex flex-wrap items-center justify-between gap-3 !bg-primary-50/40"
          >
            <div className="flex w-full min-w-0 items-center gap-3 sm:w-auto sm:flex-1">
              <MediaImage
                src={refund.shopLogoUrl}
                alt=""
                fallbackIcon={Store}
                showFallbackLabel={false}
                className="size-12 shrink-0 rounded-md"
              />
              <div className="min-w-0">
                <h2 className="text-section font-semibold text-text-primary [overflow-wrap:anywhere]">
                  {refund.shopName}
                </h2>
                <p className="mt-1 text-caption text-text-secondary">
                  Order {refund.orderCode} · Requested {checkoutTime(refund.createdAt)} · GMT+7
                </p>
              </div>
            </div>
            <RefundStatusBadge status={refund.status} overdue={refund.isOverdue} />
          </Card>
          <CustomerRefundDetailContent refund={refund} busy={busy} mutate={mutate} />
        </>
      )}
    </div>
  );
}
