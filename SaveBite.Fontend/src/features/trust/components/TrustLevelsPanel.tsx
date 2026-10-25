import { useCallback } from "react";
import { Check, Lock } from "lucide-react";
import { trustApi } from "../api/trustApi";
import { useApiResource } from "../hooks/useApiResource";
import { formatCurrency } from "../utils/format";
import { PanelEmpty, PanelError, PanelSkeleton } from "./PanelState";

export function TrustLevelsPanel() {
  const fetchLevels = useCallback(() => trustApi.getLevels(), []);
  const { data, error, reload } = useApiResource("trust:levels", fetchLevels);

  if (error && !data) return <PanelError message={error} onRetry={reload} />;
  if (!data) return <PanelSkeleton rows={2} />;
  if (data.length === 0)
    return <PanelEmpty title="No levels yet" description="Trust levels have not been set up." />;

  return (
    <div className="grid gap-4 md:grid-cols-2">
      {data.map((level) => (
        <article
          key={level.id}
          className={`rounded-xl border p-6 shadow-sm ${
            level.isCurrent
              ? "border-primary-300 bg-primary-50/40"
              : "border-border-default bg-bg-surface"
          }`}
        >
          <header className="flex items-start justify-between gap-3">
            <h3 className="text-lg font-bold text-text-primary">{level.name}</h3>
            {level.isCurrent ? (
              <span className="rounded-full bg-primary-600 px-2.5 py-1 text-[11px] font-semibold text-white">
                Your level
              </span>
            ) : level.isUnlocked ? (
              <span className="inline-flex items-center gap-1 text-xs font-medium text-primary-700">
                <Check className="size-3.5" /> Unlocked
              </span>
            ) : (
              <span className="inline-flex items-center gap-1 text-xs font-medium text-text-muted">
                <Lock className="size-3.5" /> Locked
              </span>
            )}
          </header>

          <div className="mt-4">
            <h4 className="text-sm font-semibold text-text-primary">Benefits</h4>
            {level.description ? (
              <p className="mt-1 whitespace-pre-line text-sm leading-relaxed text-text-secondary">
                {level.description}
              </p>
            ) : (
              <p className="mt-1 text-sm italic text-text-muted">No benefits listed.</p>
            )}

            {level.depositPolicy && (
              <div className="mt-3 rounded-lg border border-border-default bg-neutral-50/80 p-3 text-xs text-text-secondary">
                <p className="font-semibold text-text-primary">Pay at shop policy</p>
                {level.depositPolicy.requireFullOnlinePayment ? (
                  <p className="mt-1">100% online payment required for all orders.</p>
                ) : (
                  <ul className="mt-1 space-y-1">
                    <li>
                      Orders up to {formatCurrency(level.depositPolicy.depositFreeOrderAmount ?? 0)}:{" "}
                      <strong className="text-primary-700">No deposit required</strong>
                    </li>
                    {level.depositPolicy.ranges.map((range) => (
                      <li key={range.id}>
                        {formatCurrency(range.fromOrderAmount)} &ndash;{" "}
                        {formatCurrency(range.toOrderAmount)}:{" "}
                        <strong className="text-text-primary">
                          {formatCurrency(range.depositAmount)} fixed deposit
                        </strong>
                      </li>
                    ))}
                    <li>
                      Above {formatCurrency(level.depositPolicy.maxDepositOrderAmount ?? 0)}: 100%
                      online payment
                    </li>
                  </ul>
                )}
              </div>
            )}
          </div>

          <div className="mt-4">
            <h4 className="text-sm font-semibold text-text-primary">Requirements</h4>
            {level.requirement ? (
              <ul className="mt-1 space-y-1 text-sm text-text-secondary">
                <li>Trust Score of at least {level.requirement.minScore}</li>
                <li>At least {level.requirement.minCompletedOrders} completed orders</li>
                <li>
                  At least {formatCurrency(level.requirement.minTotalOrderValue)} in total order
                  value
                </li>
              </ul>
            ) : (
              <p className="mt-1 text-sm text-text-secondary">None.</p>
            )}
          </div>
        </article>
      ))}
    </div>
  );
}