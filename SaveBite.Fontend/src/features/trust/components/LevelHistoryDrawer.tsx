import { useCallback, useEffect } from "react";
import { Clock, History, Users, X } from "lucide-react";
import { adminTrustApi } from "../api/adminTrustApi";
import { useApiResource } from "../hooks/useApiResource";
import type { AdminTrustLevel, TrustLevelChangeType } from "../types/adminTrust.types";
import { formatCurrency, formatDateTime } from "../utils/format";
import { PanelEmpty, PanelError, PanelSkeleton } from "./PanelState";
import { RecordId } from "@/shared/components/RecordId";

interface LevelHistoryDrawerProps {
  level: AdminTrustLevel | null;
  onClose: () => void;
}

const CHANGE_TYPE_LABELS: Record<TrustLevelChangeType, { label: string; style: string }> = {
  Created: {
    label: "Created",
    style: "border-primary-200 bg-primary-50 text-primary-700",
  },
  InfoUpdated: {
    label: "Renamed",
    style: "border-blue-200 bg-blue-50 text-blue-700",
  },
  RequirementUpdated: {
    label: "Requirements updated",
    style: "border-indigo-200 bg-indigo-50 text-indigo-700",
  },
  BenefitsUpdated: {
    label: "Benefits & deposit updated",
    style: "border-amber-200 bg-amber-50 text-amber-800",
  },
  Deactivated: {
    label: "Deactivated",
    style: "border-red-200 bg-red-50 text-red-700",
  },
  Reactivated: {
    label: "Reactivated",
    style: "border-emerald-200 bg-emerald-50 text-emerald-700",
  },
};

export function LevelHistoryDrawer({ level, onClose }: LevelHistoryDrawerProps) {
  useEffect(() => {
    if (!level) return;
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") onClose();
    };
    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [level, onClose]);

  const fetchHistory = useCallback(
    () => (level ? adminTrustApi.getLevelHistory(level.id) : Promise.resolve([])),
    [level],
  );

  const { data, error, reload } = useApiResource(
    level ? `admin-trust:level-history:${level.id}:${level.updatedAt}` : "admin-trust:level-history:none",
    fetchHistory,
  );

  if (!level) return null;

  return (
    <div
      className="fixed inset-0 z-50 flex justify-end bg-neutral-900/40 backdrop-blur-xs"
      onMouseDown={(e) => {
        if (e.target === e.currentTarget) onClose();
      }}
    >
      <aside
        aria-label={`${level.name} version history`}
        className="flex h-full w-full max-w-xl flex-col bg-white shadow-2xl"
      >
        <header className="flex items-center justify-between border-b border-border-default px-6 py-4">
          <div className="flex items-center gap-2.5">
            <span className="flex size-9 items-center justify-center rounded-lg bg-primary-50 text-primary-700">
              <History className="size-5" />
            </span>
            <div>
              <h3 className="text-base font-bold text-text-primary">
                {level.name} (Rank {level.rank}) History
              </h3>
              <p className="text-xs text-text-secondary">
                Version snapshots recorded for this Trust Level
              </p>
              <RecordId id={level.id} variant="full" className="mt-2" />
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded-md p-1.5 text-neutral-400 transition-colors hover:bg-neutral-100 hover:text-neutral-700"
            aria-label="Close history drawer"
          >
            <X className="size-5" />
          </button>
        </header>

        <div className="flex-1 overflow-y-auto p-6">
          {error && !data ? (
            <PanelError message={error} onRetry={reload} />
          ) : !data ? (
            <PanelSkeleton rows={3} />
          ) : data.length === 0 ? (
            <PanelEmpty
              title="No version history"
              description="No history entries were found for this level."
            />
          ) : (
            <ol className="relative ml-2 space-y-5 border-l-2 border-neutral-200 pl-5">
              {data.map((item) => {
                const badge = CHANGE_TYPE_LABELS[item.changeType] ?? {
                  label: item.changeType,
                  style: "border-neutral-200 bg-neutral-100 text-neutral-700",
                };

                return (
                  <li key={item.id} className="relative">
                    <span className="absolute -left-[27px] top-1.5 size-3.5 rounded-full border-[3px] border-primary-600 bg-white" />

                    <div className="rounded-xl border border-border-default bg-bg-surface p-4 shadow-2xs space-y-3">
                      <div className="flex flex-wrap items-center justify-between gap-2">
                        <div className="flex items-center gap-2">
                          <span className="rounded bg-neutral-900 px-2 py-0.5 text-xs font-bold text-white">
                            v{item.versionNo}
                          </span>
                          <span
                            className={`inline-flex items-center rounded-full border px-2.5 py-0.5 text-[11px] font-semibold ${badge.style}`}
                          >
                            {badge.label}
                          </span>
                          <span
                            className={`inline-flex items-center rounded-full px-2 py-0.5 text-[11px] font-medium ${
                              item.isActive
                                ? "bg-primary-50 text-primary-700"
                                : "bg-neutral-100 text-neutral-600"
                            }`}
                          >
                            {item.isActive ? "Active" : "Inactive"}
                          </span>
                        </div>

                        <span className="inline-flex items-center gap-1 text-xs text-text-secondary">
                          <Clock className="size-3.5" />
                          {formatDateTime(item.changedAt)}
                        </span>
                      </div>

                      <div className="flex flex-wrap items-center justify-between gap-2 text-xs text-text-secondary">
                        <span>
                          Level name: <strong className="text-text-primary">{item.name}</strong> (Rank{" "}
                          {item.rank})
                        </span>
                        <span className="inline-flex items-center gap-1">
                          <Users className="size-3.5" />
                          {item.changeType === "Deactivated"
                            ? `${item.affectedUserCount} reassigned`
                            : `${item.affectedUserCount} customer(s)`}
                        </span>
                      </div>

                      <div className="grid grid-cols-3 gap-2 rounded-lg bg-neutral-50 p-2.5 text-xs">
                        <div>
                          <p className="text-text-muted">Min score</p>
                          <p className="font-semibold tabular-nums text-text-primary">
                            {item.minScore}
                          </p>
                        </div>
                        <div>
                          <p className="text-text-muted">Min orders</p>
                          <p className="font-semibold tabular-nums text-text-primary">
                            {item.minCompletedOrders}
                          </p>
                        </div>
                        <div>
                          <p className="text-text-muted">Min value</p>
                          <p className="font-semibold tabular-nums text-text-primary">
                            {formatCurrency(item.minTotalOrderValue)}
                          </p>
                        </div>
                      </div>

                      {item.description && (
                        <p className="whitespace-pre-line text-xs text-text-secondary">
                          {item.description}
                        </p>
                      )}

                      {item.depositPolicy && (
                        <div className="border-t border-border-default pt-2.5 text-xs text-text-secondary">
                          <p className="font-semibold text-text-primary">Deposit policy snapshot</p>
                          {item.depositPolicy.requireFullOnlinePayment ? (
                            <p className="mt-1 text-amber-800">100% online payment required</p>
                          ) : (
                            <div className="mt-1 space-y-1">
                              <p>
                                Free deposit: &le;{" "}
                                <strong>
                                  {formatCurrency(item.depositPolicy.depositFreeOrderAmount ?? 0)}
                                </strong>{" "}
                                &middot; Max Pay at shop:{" "}
                                <strong>
                                  {formatCurrency(item.depositPolicy.maxDepositOrderAmount ?? 0)}
                                </strong>
                              </p>
                              {item.depositPolicy.ranges.length > 0 && (
                                <ul className="space-y-0.5 pl-3 list-disc">
                                  {item.depositPolicy.ranges.map((r, idx) => (
                                    <li key={idx} className="tabular-nums">
                                      {formatCurrency(r.fromOrderAmount)} &ndash;{" "}
                                      {formatCurrency(r.toOrderAmount)}:{" "}
                                      <strong>{formatCurrency(r.depositAmount)}</strong>
                                    </li>
                                  ))}
                                </ul>
                              )}
                            </div>
                          )}
                        </div>
                      )}

                      <p className="text-[11px] text-text-muted">
                        Changed by: {item.changedByName ?? "System / Administrator"}
                      </p>
                    </div>
                  </li>
                );
              })}
            </ol>
          )}
        </div>
      </aside>
    </div>
  );
}