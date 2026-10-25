import { useCallback, useState } from "react";
import { getApiErrorMessage } from "@/shared/api";
import { toast } from "@/shared/stores/toastStore";
import { adminTrustApi } from "../api/adminTrustApi";
import { useApiResource } from "../hooks/useApiResource";
import type { TrustScoreRule, UpdateScoreRulePayload } from "../types/adminTrust.types";
import { formatDateTime, formatSignedPoints } from "../utils/format";
import { getRuleStatus, isEditableVersion } from "../utils/ruleStatus";
import { PanelEmpty, PanelError, PanelSkeleton } from "./PanelState";
import { RuleStatusBadge } from "./RuleStatusBadge";
import { ScoreRuleModal } from "./ScoreRuleModal";

interface ScoreRulesPanelProps {
  /** "all" = UC-135/136 (every Trust Score rule), "earning" = UC-137/138 (rules that add points). */
  variant: "all" | "earning";
}

const COPY = {
  all: {
    title: "Trust Score rules",
    description: "How many points each kind of event adds to or removes from a customer's score.",
    empty: "No Trust Score rules have been set up.",
    success: "Trust Score rule updated.",
  },
  earning: {
    title: "Earning rules",
    description: "How many points a customer earns, for example when an order is completed.",
    empty: "No earning rules have been set up.",
    success: "Earning rule updated.",
  },
} as const;

// UC-135 / UC-136 and UC-137 / UC-138.
export function ScoreRulesPanel({ variant }: ScoreRulesPanelProps) {
  const copy = COPY[variant];

  const fetchRules = useCallback(
    () => (variant === "earning" ? adminTrustApi.getEarningRules() : adminTrustApi.getScoreRules()),
    [variant],
  );
  const { data, error, reload } = useApiResource(`admin-trust:score-rules:${variant}`, fetchRules);

  const [now] = useState(() => Date.now());
  const [showHistory, setShowHistory] = useState(false);
  const [editingRule, setEditingRule] = useState<TrustScoreRule | null>(null);

  const saveRule = async (ruleId: string, payload: UpdateScoreRulePayload) => {
    try {
      if (variant === "earning") await adminTrustApi.updateEarningRule(ruleId, payload);
      else await adminTrustApi.updateScoreRule(ruleId, payload);
      toast.success(copy.success);
      reload();
      return null;
    } catch (caught) {
      return getApiErrorMessage(caught);
    }
  };

  if (error && !data) return <PanelError message={error} onRetry={reload} />;
  if (!data) return <PanelSkeleton rows={2} />;
  if (data.length === 0) return <PanelEmpty title="No rules" description={copy.empty} />;

  const rows = data.filter((rule) => showHistory || getRuleStatus(rule, now) !== "replaced");

  return (
    <section className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h3 className="text-base font-bold text-text-primary">{copy.title}</h3>
          <p className="text-sm text-text-secondary">{copy.description}</p>
        </div>
        <label className="flex cursor-pointer items-center gap-2 text-sm text-text-secondary">
          <input
            type="checkbox"
            checked={showHistory}
            onChange={(event) => setShowHistory(event.target.checked)}
            className="size-4 rounded border-border-default text-primary-600 focus:ring-primary-500"
          />
          Show replaced versions
        </label>
      </div>

      <div className="overflow-x-auto rounded-xl border border-border-default bg-bg-surface">
        <table className="min-w-full divide-y divide-border-default text-left text-sm">
          <thead className="bg-neutral-50 text-text-secondary">
            <tr>
              <th className="px-5 py-3 font-semibold">Type</th>
              <th className="px-5 py-3 text-right font-semibold">Points</th>
              <th className="px-5 py-3 font-semibold">Status</th>
              <th className="px-5 py-3 font-semibold">Effective</th>
              <th className="px-5 py-3 font-semibold">Updated by</th>
              <th className="px-5 py-3 text-right font-semibold">
                <span className="sr-only">Actions</span>
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border-default text-text-primary">
            {rows.map((rule) => {
              const status = getRuleStatus(rule, now);
              return (
                <tr key={rule.id} className={status === "replaced" ? "text-text-muted" : ""}>
                  <td className="px-5 py-3 font-medium">
                    {rule.ruleType === "OnlineOrderCompleted" ? "Online order completed" : rule.ruleType === "PayAtShopOrderCompleted" ? "Pay-at-shop order completed" : "NoShow penalty"}
                  </td>
                  <td
                    className={`px-5 py-3 text-right font-semibold tabular-nums ${
                      status === "replaced"
                        ? ""
                        : rule.scoreDelta >= 0
                          ? "text-primary-700"
                          : "text-danger"
                    }`}
                  >
                    {formatSignedPoints(rule.scoreDelta)}
                  </td>
                  <td className="px-5 py-3">
                    <RuleStatusBadge status={status} />
                  </td>
                  <td className="px-5 py-3 text-xs">
                    <p>From {formatDateTime(rule.effectiveFrom)}</p>
                    {rule.effectiveTo && <p>Until {formatDateTime(rule.effectiveTo)}</p>}
                  </td>
                  <td className="px-5 py-3 text-xs">{rule.createdByName ?? "—"}</td>
                  <td className="px-5 py-3 text-right">
                    {isEditableVersion(rule) && (
                      <button
                        type="button"
                        onClick={() => setEditingRule(rule)}
                        className="rounded-md border border-border-default bg-white px-3 py-1.5 text-xs font-semibold text-text-secondary transition-colors hover:bg-neutral-50"
                      >
                        Edit
                      </button>
                    )}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      <ScoreRuleModal rule={editingRule} onClose={() => setEditingRule(null)} onSave={saveRule} />
    </section>
  );
}
