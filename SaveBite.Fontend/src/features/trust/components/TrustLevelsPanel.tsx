import { Badge, Card } from "@/shared/components/ui";
import { useCallback } from "react";
import { Award, Check, Gift, Lock, Target } from "lucide-react";
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
    <div className="grid items-start gap-4 md:grid-cols-2">
      {data.map((level) => (
        <Card
          key={level.id}
          className={`!p-0 overflow-hidden ${
            level.isCurrent
              ? "border-primary-300 bg-primary-50/40"
              : "border-border-default bg-bg-surface"
          }`}
        >
          <header className="flex flex-wrap items-start justify-between gap-3 border-b border-border-default bg-primary-50/40 p-4 sm:p-5">
            <div className="flex min-w-0 items-center gap-3">
              <span
                className={`flex size-12 shrink-0 items-center justify-center rounded-md ${level.isCurrent ? "bg-primary-100 text-primary-700" : "bg-secondary-50 text-secondary-700"}`}
              >
                <Award size={26} aria-hidden="true" />
              </span>
              <div className="min-w-0">
                <p className="text-caption text-text-secondary">Level {level.rank}</p>
                <h2 className="text-section font-semibold text-text-primary [overflow-wrap:anywhere]">
                  {level.name}
                </h2>
              </div>
            </div>
            {level.isCurrent ? (
              <Badge tone="primary">Your level</Badge>
            ) : level.isUnlocked ? (
              <span className="inline-flex items-center gap-1 text-xs font-medium text-primary-700">
                <Check className="size-3.5" aria-hidden="true" /> Unlocked
              </span>
            ) : (
              <span className="inline-flex items-center gap-1 text-xs font-medium text-text-muted">
                <Lock className="size-3.5" aria-hidden="true" /> Locked
              </span>
            )}
          </header>

          <div className="p-4 sm:p-5">
            <h3 className="flex items-center gap-2 text-body font-semibold text-text-primary">
              <Gift size={16} aria-hidden="true" className="text-secondary-700" />
              Benefits
            </h3>
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
                      Orders up to {formatCurrency(level.depositPolicy.depositFreeOrderAmount ?? 0)}
                      : <strong className="text-primary-700">No deposit required</strong>
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

          <div className="mx-4 border-t border-border-default py-4 sm:mx-5 sm:py-5">
            <h3 className="mb-3 flex items-center gap-2 text-body font-semibold text-text-primary">
              <Target size={16} aria-hidden="true" className="text-primary-700" />
              Requirements
            </h3>
            {level.requirement ? (
              <ul className="space-y-2 text-body text-text-secondary [&>li]:rounded-md [&>li]:bg-neutral-50 [&>li]:p-3">
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
        </Card>
      ))}
    </div>
  );
}
