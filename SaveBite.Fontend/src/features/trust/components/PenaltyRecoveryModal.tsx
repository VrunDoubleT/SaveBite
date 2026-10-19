import { useCallback } from "react";
import { SharedModal } from "@/shared/components/modal/BaseModal";
import { trustApi } from "../api/trustApi";
import { useApiResource } from "../hooks/useApiResource";
import { METRIC_LABELS, formatCurrency, formatDateTime, formatMetricValue } from "../utils/format";
import { PanelError, PanelSkeleton } from "./PanelState";
import { ProgressBar } from "./ProgressBar";

interface PenaltyRecoveryModalProps {
  penaltyId: string;
  onClose: () => void;
}

// UC-49: progress of a single penalty towards recovery.
export function PenaltyRecoveryModal({ penaltyId, onClose }: PenaltyRecoveryModalProps) {
  const fetchRecovery = useCallback(() => trustApi.getPenaltyRecovery(penaltyId), [penaltyId]);
  const { data, error, reload } = useApiResource(`trust:recovery:${penaltyId}`, fetchRecovery);

  return (
    <SharedModal isOpen onClose={onClose} title="Penalty recovery" maxWidthClass="max-w-lg">
      {error && !data ? (
        <PanelError message={error} onRetry={reload} />
      ) : !data ? (
        <PanelSkeleton rows={2} />
      ) : (
        <div className="space-y-5">
          <div>
            <p className="text-sm text-text-secondary">
              No-show{data.penalty.orderCode ? ` on order ${data.penalty.orderCode}` : ""} ·{" "}
              {formatDateTime(data.penalty.createdAt)}
            </p>
            <p className="mt-1 text-sm text-text-primary">
              {data.penalty.scoreDeducted} points deducted (occurrence #
              {data.penalty.occurrenceNumber} this month).
            </p>
          </div>

          {data.isRecovered ? (
            <p className="rounded-lg bg-primary-50 p-3 text-sm text-primary-700">
              Recovered on {formatDateTime(data.penalty.recoveredAt)}.
            </p>
          ) : (
            <>
              <ProgressBar
                label="Overall recovery"
                percent={data.overallPercent}
                valueText={`${Math.round(data.overallPercent)}%`}
              />
              <div className="space-y-4 border-t border-border-default pt-4">
                {data.metrics.map((metric) => (
                  <ProgressBar
                    key={metric.key}
                    label={METRIC_LABELS[metric.key] ?? metric.key}
                    percent={metric.percent}
                    valueText={`${formatMetricValue(metric.key, metric.current)} / ${formatMetricValue(metric.key, metric.required)}`}
                  />
                ))}
              </div>
            </>
          )}

          {data.penalty.permanentOfflineLock && (
            <p className="rounded-lg bg-danger/10 p-3 text-sm text-danger">
              This penalty permanently turns off pay at shop for your account.
            </p>
          )}

          <div className="border-t border-border-default pt-4">
            <h4 className="text-sm font-semibold text-text-primary">Orders counted</h4>
            {data.recoveryOrders.length === 0 ? (
              <p className="mt-1 text-sm text-text-secondary">
                No orders counted yet. Online-paid orders you complete after this penalty count
                here.
              </p>
            ) : (
              <ul className="mt-2 divide-y divide-border-default text-sm">
                {data.recoveryOrders.map((order) => (
                  <li key={order.orderId} className="flex items-center justify-between gap-3 py-2">
                    <span className="text-text-primary">{order.orderCode ?? order.orderId}</span>
                    <span className="text-text-secondary">
                      {formatCurrency(order.orderValue)} · {formatDateTime(order.createdAt)}
                    </span>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>
      )}
    </SharedModal>
  );
}
