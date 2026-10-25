import { useCallback } from "react";
import { Lock, ShieldAlert, ShieldCheck } from "lucide-react";
import { trustApi } from "../api/trustApi";
import { useApiResource } from "../hooks/useApiResource";
import type { TrustStanding } from "../types/trust.types";
import {
  METRIC_LABELS,
  formatCurrency,
  formatDateTime,
  formatMetricValue,
  formatSignedPoints,
} from "../utils/format";
import { PanelError, PanelSkeleton } from "./PanelState";
import { ProgressBar } from "./ProgressBar";

const STANDING: Record<
  TrustStanding,
  { label: string; description: string; className: string; icon: typeof ShieldCheck }
> = {
  GoodStanding: {
    label: "Good standing",
    description: "You have no active penalties.",
    className: "border-primary-100 bg-primary-50 text-primary-700",
    icon: ShieldCheck,
  },
  Restricted: {
    label: "Recovering from a penalty",
    description: "Finish the recovery requirements in the Penalties tab to clear it.",
    className: "border-accent-100 bg-accent-50 text-accent-700",
    icon: ShieldAlert,
  },
  OfflineLocked: {
    label: "Pay at shop unavailable",
    description: "Pay at shop is permanently turned off for your account.",
    className: "border-danger/20 bg-danger/10 text-danger",
    icon: Lock,
  },
};

export function TrustOverviewPanel() {
  const fetchOverview = useCallback(() => trustApi.getOverview(), []);
  const fetchProgress = useCallback(() => trustApi.getLevelProgress(), []);

  const overview = useApiResource("trust:overview", fetchOverview);
  const progress = useApiResource("trust:level-progress", fetchProgress);

  const reloadAll = () => {
    overview.reload();
    progress.reload();
  };

  if (overview.error && !overview.data)
    return <PanelError message={overview.error} onRetry={reloadAll} />;
  if (!overview.data) return <PanelSkeleton rows={3} />;

  const data = overview.data;
  const standing = STANDING[data.standing] ?? STANDING.GoodStanding;
  const StandingIcon = standing.icon;

  return (
    <div className="space-y-6">
      {/* UC-44: score, level and standing */}
      <section className="rounded-xl border border-border-default bg-bg-surface p-6 shadow-sm">
        <div className="flex flex-col gap-6 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <p className="text-sm font-medium text-text-secondary">Your Trust Score</p>
            <p className="mt-1 text-6xl font-bold tabular-nums text-text-primary">
              {data.currentScore}
            </p>
            <p className="mt-2 text-sm text-text-secondary">
              Updated {formatDateTime(data.updatedAt)}
            </p>
          </div>

          <div className="sm:text-right">
            <p className="text-sm font-medium text-text-secondary">Current level</p>
            <p className="mt-1 text-3xl font-bold text-primary-700">{data.level.name}</p>
            {data.isHighestLevel && (
              <p className="mt-1 text-sm text-text-secondary">You are on the highest level.</p>
            )}
          </div>
        </div>

        <div className={`mt-6 flex items-start gap-3 rounded-lg border p-4 ${standing.className}`}>
          <StandingIcon className="mt-0.5 size-5 shrink-0" />
          <div>
            <p className="text-sm font-semibold">{standing.label}</p>
            <p className="text-sm opacity-90">{standing.description}</p>
          </div>
        </div>

        <dl className="mt-6 grid gap-4 sm:grid-cols-3">
          <div className="rounded-lg bg-neutral-50 p-4">
            <dt className="text-xs font-medium text-text-secondary">Completed orders</dt>
            <dd className="mt-1 text-xl font-bold tabular-nums text-text-primary">
              {data.completedOrdersCount}
            </dd>
          </div>
          <div className="rounded-lg bg-neutral-50 p-4">
            <dt className="text-xs font-medium text-text-secondary">Total order value</dt>
            <dd className="mt-1 text-xl font-bold tabular-nums text-text-primary">
              {formatCurrency(data.accumulatedOrderValue)}
            </dd>
          </div>
          <div className="rounded-lg bg-neutral-50 p-4">
            <dt className="text-xs font-medium text-text-secondary">Active penalties</dt>
            <dd className="mt-1 text-xl font-bold tabular-nums text-text-primary">
              {data.activePenaltyCount}
            </dd>
          </div>
        </dl>
      </section>

      {/* Benefits of the current level */}
      <section className="rounded-xl border border-border-default bg-bg-surface p-6 shadow-sm">
        <h3 className="text-base font-bold text-text-primary">{data.level.name} benefits</h3>
        {data.level.description ? (
          <p className="mt-2 whitespace-pre-line text-sm leading-relaxed text-text-secondary">
            {data.level.description}
          </p>
        ) : (
          <p className="mt-2 text-sm italic text-text-muted">
            No benefits are listed for this level yet.
          </p>
        )}
      </section>

      {/* UC-45: progress towards the next level */}
      <section className="rounded-xl border border-border-default bg-bg-surface p-6 shadow-sm">
        <h3 className="text-base font-bold text-text-primary">Level progress</h3>

        {progress.error && !progress.data ? (
          <div className="mt-4">
            <PanelError message={progress.error} onRetry={progress.reload} />
          </div>
        ) : !progress.data ? (
          <div className="mt-4">
            <PanelSkeleton rows={2} />
          </div>
        ) : progress.data.isHighestLevel || !progress.data.nextLevel ? (
          <p className="mt-2 text-sm text-text-secondary">
            You have reached the highest level. Keep completing orders to maintain it.
          </p>
        ) : (
          <div className="mt-4 space-y-5">
            <ProgressBar
              label={`Progress to ${progress.data.nextLevel.name}`}
              percent={progress.data.overallPercent}
              valueText={`${Math.round(progress.data.overallPercent)}%`}
            />

            <div className="space-y-4 border-t border-border-default pt-4">
              {progress.data.metrics.map((metric) => (
                <ProgressBar
                  key={metric.key}
                  label={METRIC_LABELS[metric.key] ?? metric.key}
                  percent={metric.percent}
                  valueText={`${formatMetricValue(metric.key, metric.current)} / ${formatMetricValue(metric.key, metric.required)}`}
                />
              ))}
            </div>

            {progress.data.requirementsMet && (
              <p className="rounded-lg bg-primary-50 p-3 text-sm text-primary-700">
                You meet every requirement for {progress.data.nextLevel.name}.
              </p>
            )}

            <p className="text-sm text-text-secondary">
              Completed online orders: <strong>{progress.data.onlineOrderCompletedPoints == null ? "No active rule" : `${formatSignedPoints(progress.data.onlineOrderCompletedPoints)} points`}</strong>.
              {" "}Completed pay-at-shop orders: <strong>{progress.data.payAtShopOrderCompletedPoints == null ? "No active rule" : `${formatSignedPoints(progress.data.payAtShopOrderCompletedPoints)} points`}</strong>.
            </p>
          </div>
        )}
      </section>
    </div>
  );
}
