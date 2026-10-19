import { useCallback, useState } from "react";
import { getApiErrorMessage } from "@/shared/api";
import { toast } from "@/shared/stores/toastStore";
import { adminTrustApi } from "../api/adminTrustApi";
import { useApiResource } from "../hooks/useApiResource";
import type { PenaltyRule, UpdatePenaltyRulePayload } from "../types/adminTrust.types";
import { formatCurrency, formatDateTime } from "../utils/format";
import { getRuleStatus, isEditableVersion } from "../utils/ruleStatus";
import { PanelEmpty, PanelError, PanelSkeleton } from "./PanelState";
import { PenaltyRuleModal } from "./PenaltyRuleModal";
import { RuleStatusBadge } from "./RuleStatusBadge";

// UC-133 (view) and UC-134 (update) of the no-show penalty rules.
export function PenaltyRulesPanel() {
  const fetchRules = useCallback(() => adminTrustApi.getPenaltyRules(), []);
  const { data, error, reload } = useApiResource("admin-trust:penalty-rules", fetchRules);

  const [now] = useState(() => Date.now());
  const [showHistory, setShowHistory] = useState(false);
  const [editingRule, setEditingRule] = useState<PenaltyRule | null>(null);

  const saveRule = async (ruleId: string, payload: UpdatePenaltyRulePayload) => {
    try {
      await adminTrustApi.updatePenaltyRule(ruleId, payload);
      toast.success("Penalty rule updated.");
      reload();
      return null;
    } catch (caught) {
      return getApiErrorMessage(caught);
    }
  };

  if (error && !data) return <PanelError message={error} onRetry={reload} />;
  if (!data) return <PanelSkeleton rows={3} />;
  if (data.length === 0)
    return (
      <PanelEmpty
        title="No penalty rules"
        description="No-show penalty rules have not been set up."
      />
    );

  const rows = data.filter((rule) => showHistory || getRuleStatus(rule, now) !== "replaced");

  return (
    <section className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h3 className="text-base font-bold text-text-primary">No-show penalty rules</h3>
          <p className="text-sm text-text-secondary">
            Points deducted and what a customer must do to recover after a no-show.
          </p>
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
              <th className="px-5 py-3 font-semibold">From no-show #</th>
              <th className="px-5 py-3 text-right font-semibold">Points deducted</th>
              <th className="px-5 py-3 font-semibold">Recovery requirement</th>
              <th className="px-5 py-3 font-semibold">Pay at shop</th>
              <th className="px-5 py-3 font-semibold">Status</th>
              <th className="px-5 py-3 font-semibold">Effective</th>
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
                  <td className="px-5 py-3 font-semibold tabular-nums">{rule.minimumOccurrence}</td>
                  <td className="px-5 py-3 text-right tabular-nums">-{rule.scoreDeduction}</td>
                  <td className="px-5 py-3 text-xs">
                    <p>{rule.requiredOnlineOrders} online orders</p>
                    <p>{formatCurrency(rule.requiredOnlineOrderValue)}</p>
                  </td>
                  <td className="px-5 py-3 text-xs">
                    {rule.permanentOfflineLock ? "Turned off permanently" : "Not affected"}
                  </td>
                  <td className="px-5 py-3">
                    <RuleStatusBadge status={status} />
                  </td>
                  <td className="px-5 py-3 text-xs">
                    <p>From {formatDateTime(rule.effectiveFrom)}</p>
                    {rule.effectiveTo && <p>Until {formatDateTime(rule.effectiveTo)}</p>}
                  </td>
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

      <PenaltyRuleModal rule={editingRule} onClose={() => setEditingRule(null)} onSave={saveRule} />
    </section>
  );
}
