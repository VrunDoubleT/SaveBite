import { Badge, Card } from "@/shared/components/ui";
import { useCallback } from "react";
import { Award, Check, CreditCard, Gift, Lock, Target, WalletCards } from "lucide-react";
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
        <Card
          key={level.id}
          className={`!p-0 overflow-hidden md:row-span-4 md:grid md:grid-rows-subgrid md:gap-y-0 ${
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
          </div>
          <div className="min-w-0 px-4 pb-4 sm:px-5 sm:pb-5">
            {level.depositPolicy && (
              <section
                aria-label={`Pay at shop policy for ${level.name}`}
                className="h-full overflow-hidden rounded-md border border-border-default bg-bg-surface"
              >
                <div className="flex items-center gap-2 border-b border-border-default px-3 py-3">
                  <WalletCards
                    size={17}
                    aria-hidden="true"
                    className="shrink-0 text-secondary-700"
                  />
                  <h4 className="text-body font-semibold text-text-primary">Pay at shop policy</h4>
                </div>
                {level.depositPolicy.requireFullOnlinePayment ? (
                  <div className="flex items-start gap-3 p-4">
                    <span className="flex size-9 shrink-0 items-center justify-center rounded-md bg-secondary-50 text-secondary-700">
                      <CreditCard size={18} aria-hidden="true" />
                    </span>
                    <div>
                      <p className="text-body font-semibold text-text-primary">
                        Pay online in full
                      </p>
                      <p className="mt-1 text-caption leading-relaxed text-text-secondary">
                        All orders require 100% online payment at this level.
                      </p>
                    </div>
                  </div>
                ) : (
                  <>
                    <div className="grid grid-cols-[minmax(0,1.2fr)_minmax(0,1fr)] gap-3 bg-neutral-50 px-3 py-2 text-caption font-medium text-text-secondary">
                      <p>Order value</p>
                      <p className="text-right">Payment required</p>
                    </div>
                    <dl className="divide-y divide-border-default">
                      <div className="grid grid-cols-[minmax(0,1.2fr)_minmax(0,1fr)] items-center gap-3 bg-primary-50/40 px-3 py-3">
                        <dt className="min-w-0 text-caption text-text-secondary">
                          Up to{" "}
                          <span className="mt-1 block text-body font-semibold tabular-nums text-text-primary">
                            {formatCurrency(level.depositPolicy.depositFreeOrderAmount ?? 0)}
                          </span>
                        </dt>
                        <dd className="min-w-0 text-right text-body font-semibold text-primary-700">
                          No deposit
                          <span className="mt-1 block text-caption font-normal text-text-secondary">
                            Pay at pickup
                          </span>
                        </dd>
                      </div>
                      {level.depositPolicy.ranges.map((range) => (
                        <div
                          key={range.id}
                          className="grid grid-cols-[minmax(0,1.2fr)_minmax(0,1fr)] items-center gap-3 px-3 py-3"
                        >
                          <dt className="min-w-0 text-body font-medium tabular-nums text-text-primary [overflow-wrap:anywhere]">
                            {formatCurrency(range.fromOrderAmount)}
                            <span className="block text-caption font-normal text-text-muted">
                              to {formatCurrency(range.toOrderAmount)}
                            </span>
                          </dt>
                          <dd className="min-w-0 text-right text-body font-semibold tabular-nums text-primary-700 [overflow-wrap:anywhere]">
                            {formatCurrency(range.depositAmount)}
                            <span className="mt-1 block text-caption font-normal text-text-secondary">
                              Fixed online deposit
                            </span>
                          </dd>
                        </div>
                      ))}
                      <div className="grid grid-cols-[minmax(0,1.2fr)_minmax(0,1fr)] items-center gap-3 bg-neutral-50/60 px-3 py-3">
                        <dt className="min-w-0 text-caption text-text-secondary">
                          Above
                          <span className="mt-1 block text-body font-semibold tabular-nums text-text-primary">
                            {formatCurrency(level.depositPolicy.maxDepositOrderAmount ?? 0)}
                          </span>
                        </dt>
                        <dd className="min-w-0 text-right text-body font-semibold text-text-primary">
                          100% online
                          <span className="mt-1 block text-caption font-normal text-text-secondary">
                            Full payment
                          </span>
                        </dd>
                      </div>
                    </dl>
                  </>
                )}
              </section>
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
