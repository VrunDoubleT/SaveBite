import { Badge, Card } from "@/shared/components/ui";
import { useCallback } from "react";
import {
  Award,
  CheckCircle2,
  Gift,
  Lock,
  ShieldAlert,
  ShieldCheck,
  ShoppingBag,
  Wallet,
} from "lucide-react";
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
    className: "border-danger-soft bg-danger-soft text-danger-strong",
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
      <Card aria-label="Your membership" className="!p-0 overflow-hidden">
        <div className="flex flex-col gap-5 border-b border-border-default bg-primary-50/50 p-4 sm:p-6 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex min-w-0 items-start gap-4">
            <span className="flex size-16 shrink-0 items-center justify-center rounded-lg border border-primary-100 bg-bg-surface text-primary-700">
              <Award size={34} aria-hidden="true" />
            </span>
            <div className="min-w-0">
              <p className="text-caption font-medium text-text-secondary">
                Your current level · Level {data.level.rank}
              </p>
              <h2 className="mt-1 text-3xl font-bold text-primary-800 [overflow-wrap:anywhere]">
                {data.level.name}
              </h2>
              {data.isHighestLevel && (
                <Badge tone="primary" className="mt-2">
                  Highest level reached
                </Badge>
              )}
            </div>
          </div>
          <div className="rounded-md border border-primary-100 bg-bg-surface px-5 py-3 sm:text-right">
            <p className="text-caption font-medium text-text-secondary">Trust Score</p>
            <p className="mt-1 text-4xl font-bold tabular-nums text-primary-700">
              {data.currentScore}
              <span className="ml-2 text-body font-normal text-text-secondary">points</span>
            </p>
          </div>
        </div>
        <div className="space-y-4 p-4 sm:p-6">
          <div className={`flex items-start gap-3 rounded-md border p-3 ${standing.className}`}>
            <StandingIcon size={20} aria-hidden="true" className="mt-0.5 shrink-0" />
            <div>
              <p className="text-body font-semibold">{standing.label}</p>
              <p className="mt-1 text-body">{standing.description}</p>
            </div>
          </div>
          <dl className="grid gap-3 sm:grid-cols-3">
            {[
              {
                label: "Completed orders",
                value: data.completedOrdersCount,
                icon: ShoppingBag,
                tone: "bg-primary-50 text-primary-700",
              },
              {
                label: "Total order value",
                value: formatCurrency(data.accumulatedOrderValue),
                icon: Wallet,
                tone: "bg-secondary-50 text-secondary-700",
              },
              {
                label: "Active penalties",
                value: data.activePenaltyCount,
                icon: ShieldAlert,
                tone:
                  data.activePenaltyCount > 0
                    ? "bg-accent-50 text-accent-700"
                    : "bg-neutral-50 text-text-primary",
              },
            ].map(({ label, value, icon: Icon, tone }) => (
              <div key={label} className={`min-w-0 rounded-md p-4 ${tone}`}>
                <dt className="flex items-center gap-2 text-caption font-medium">
                  <Icon size={16} aria-hidden="true" />
                  {label}
                </dt>
                <dd className="mt-2 break-words text-xl font-bold tabular-nums">{value}</dd>
              </div>
            ))}
          </dl>
          <p className="text-caption text-text-muted">Updated {formatDateTime(data.updatedAt)}</p>
        </div>
      </Card>
      {/* Benefits of the current level */}
      <Card>
        <h2 className="flex items-center gap-2 text-section font-semibold text-text-primary">
          <Gift size={20} className="text-secondary-700" aria-hidden="true" />
          {data.level.name} benefits
        </h2>
        {data.level.description ? (
          <p className="mt-2 whitespace-pre-line text-sm leading-relaxed text-text-secondary">
            {data.level.description}
          </p>
        ) : (
          <p className="mt-2 text-sm italic text-text-muted">
            No benefits are listed for this level yet.
          </p>
        )}
      </Card>

      {/* UC-45: progress towards the next level */}
      <Card>
        <h2 className="flex items-center gap-2 text-section font-semibold text-text-primary">
          <Award size={20} className="text-primary-700" aria-hidden="true" />
          Your next level
        </h2>

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
            <div className="rounded-md bg-primary-50/50 p-4">
              <p className="mb-3 text-body font-semibold text-primary-800">
                Next: {progress.data.nextLevel.name}
              </p>
              <ProgressBar
                label={`Progress to ${progress.data.nextLevel.name}`}
                percent={progress.data.overallPercent}
                valueText={`${Math.round(progress.data.overallPercent)}%`}
              />
            </div>

            <div className="space-y-4 border-t border-border-default pt-4">
              {progress.data.metrics.map((metric) => (
                <div key={metric.key} className="rounded-md border border-border-default p-4">
                  <ProgressBar
                    label={METRIC_LABELS[metric.key] ?? metric.key}
                    percent={metric.percent}
                    valueText={`${formatMetricValue(metric.key, metric.current)} / ${formatMetricValue(metric.key, metric.required)}`}
                  />
                  <p className="mt-2 text-caption text-text-secondary">
                    {metric.remaining > 0 ? (
                      `${formatMetricValue(metric.key, metric.remaining)} still needed`
                    ) : (
                      <span className="inline-flex items-center gap-1 text-primary-700">
                        <CheckCircle2 size={14} aria-hidden="true" />
                        Requirement met
                      </span>
                    )}
                  </p>
                </div>
              ))}
            </div>

            {progress.data.requirementsMet && (
              <p className="rounded-lg bg-primary-50 p-3 text-sm text-primary-700">
                You meet every requirement for {progress.data.nextLevel.name}.
              </p>
            )}

            <p className="text-sm text-text-secondary">
              Completed online orders:{" "}
              <strong>
                {progress.data.onlineOrderCompletedPoints == null
                  ? "No active rule"
                  : `${formatSignedPoints(progress.data.onlineOrderCompletedPoints)} points`}
              </strong>
              . Completed pay-at-shop orders:{" "}
              <strong>
                {progress.data.payAtShopOrderCompletedPoints == null
                  ? "No active rule"
                  : `${formatSignedPoints(progress.data.payAtShopOrderCompletedPoints)} points`}
              </strong>
              .
            </p>
          </div>
        )}
      </Card>
    </div>
  );
}
