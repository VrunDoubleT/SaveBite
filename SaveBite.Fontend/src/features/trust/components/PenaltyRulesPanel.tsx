import { TableLoadingSkeleton } from "@/shared/components/ui/LoadingSkeleton";
import { useCallback, useMemo, useState } from "react";
import { Pencil, Plus, Trash2 } from "lucide-react";
import { SharedModal } from "@/shared/components/modal/BaseModal";
import { getApiErrorMessage } from "@/shared/api";
import { toast } from "@/shared/stores/toastStore";
import { adminTrustApi } from "../api/adminTrustApi";
import { useApiResource } from "../hooks/useApiResource";
import type {
  CreatePenaltyRulePayload,
  PenaltyRule,
  UpdatePenaltyRulePayload,
} from "../types/adminTrust.types";
import { formatCurrency, formatDateTime } from "../utils/format";
import { getRuleStatus, isFutureRule } from "../utils/ruleStatus";
import { PanelEmpty, PanelError } from "./PanelState";
import { PenaltyRuleModal } from "./PenaltyRuleModal";
import { RuleStatusBadge } from "./RuleStatusBadge";
import { RecordId } from "@/shared/components/RecordId";

// UC-133 (view), UC-134 (update), UC-156 (create), and delete future scheduled penalty rules.
export function PenaltyRulesPanel() {
  const fetchRules = useCallback(() => adminTrustApi.getPenaltyRules(), []);
  const { data, error, reload } = useApiResource("admin-trust:penalty-rules", fetchRules);

  const [now, setNow] = useState(() => Date.now());
  const [showHistory, setShowHistory] = useState(true);
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [editingRule, setEditingRule] = useState<PenaltyRule | null>(null);
  const [deletingRule, setDeletingRule] = useState<PenaltyRule | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const suggestedOccurrence = useMemo(() => {
    if (!data || data.length === 0) return 1;
    return Math.max(...data.map((r) => r.minimumOccurrence)) + 1;
  }, [data]);

  const createRule = async (payload: CreatePenaltyRulePayload) => {
    try {
      await adminTrustApi.createPenaltyRule(payload);
      toast.success("New penalty rule created.");
      setNow(Date.now());
      reload();
      return null;
    } catch (caught) {
      return getApiErrorMessage(caught);
    }
  };

  const saveRule = async (ruleId: string, payload: UpdatePenaltyRulePayload) => {
    try {
      await adminTrustApi.updatePenaltyRule(ruleId, payload);
      toast.success("Penalty rule updated.");
      setNow(Date.now());
      reload();
      return null;
    } catch (caught) {
      return getApiErrorMessage(caught);
    }
  };

  const confirmDeleteRule = async () => {
    if (!deletingRule) return;
    setIsDeleting(true);
    try {
      await adminTrustApi.deletePenaltyRule(deletingRule.id);
      toast.success("Future penalty rule deleted.");
      setDeletingRule(null);
      setNow(Date.now());
      reload();
    } catch (caught) {
      toast.error(getApiErrorMessage(caught));
    } finally {
      setIsDeleting(false);
    }
  };

  if (error && !data) return <PanelError message={error} onRetry={reload} />;
  if (!data)
    return (
      <TableLoadingSkeleton
        headers={[
          "ID",
          "From no-show #",
          "Points deducted",
          "Recovery requirement",
          "Pay at shop",
          "Status",
          "Effective period",
          "Actions",
        ]}
        label="Loading trust rules"
      />
    );

  const rows = data.filter((rule) => showHistory || getRuleStatus(rule, now) !== "replaced");

  return (
    <section className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h3 className="text-base font-bold text-text-primary">No-show penalty rules</h3>
          <p className="text-sm text-text-secondary">
            Points deducted, recovery requirements, and effective schedules after a no-show.
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-4">
          <label className="flex cursor-pointer items-center gap-2 text-sm text-text-secondary">
            <input
              type="checkbox"
              checked={showHistory}
              onChange={(event) => setShowHistory(event.target.checked)}
              className="size-4 rounded border-border-default text-primary-600 focus:ring-primary-500"
            />
            Show expired / replaced rules
          </label>

          <button
            type="button"
            onClick={() => setIsCreateOpen(true)}
            className="inline-flex items-center gap-2 rounded-lg bg-primary-700 px-4 py-2 text-sm font-semibold text-white shadow-sm transition-colors hover:bg-primary-800 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-700"
          >
            <Plus className="size-4" />
            Add penalty rule
          </button>
        </div>
      </div>

      {data.length === 0 ? (
        <PanelEmpty
          title="No penalty rules"
          description="No-show penalty rules have not been set up."
        />
      ) : (
        <div className="overflow-x-auto rounded-lg border border-border-default bg-bg-surface shadow-sm">
          <table className="w-full min-w-[1200px] table-fixed divide-y divide-border-default text-left text-sm">
            <thead className="bg-neutral-50 text-xs font-semibold uppercase tracking-wide text-text-secondary">
              <tr>
                <th className="px-3 py-3">ID</th>
                <th className="px-3 py-3">From no-show #</th>
                <th className="px-3 py-3 text-right">Points deducted</th>
                <th className="px-3 py-3">Recovery requirement</th>
                <th className="px-3 py-3">Pay at shop</th>
                <th className="px-3 py-3">Status</th>
                <th className="px-3 py-3">Effective period</th>
                <th className="px-3 py-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border-default text-text-primary">
              {rows.map((rule) => {
                const status = getRuleStatus(rule, now);
                const canDelete = isFutureRule(rule, now);

                return (
                  <tr
                    key={rule.id}
                    className={
                      status === "replaced" || status === "disabled"
                        ? "bg-neutral-100/80 text-text-muted"
                        : ""
                    }
                  >
                    <td className="max-w-40 px-3 py-3">
                      <RecordId id={rule.id} />
                    </td>
                    <td className="px-3 py-3 font-semibold tabular-nums">
                      #{rule.minimumOccurrence}
                    </td>
                    <td className="px-3 py-3 text-right font-semibold tabular-nums text-danger">
                      -{rule.scoreDeduction}
                    </td>
                    <td className="px-3 py-3 text-xs">
                      <p>{rule.requiredOnlineOrders} online orders</p>
                      <p>{formatCurrency(rule.requiredOnlineOrderValue)}</p>
                    </td>
                    <td className="px-3 py-3 text-xs">
                      {rule.permanentOfflineLock
                        ? "Turned off permanently"
                        : "Online only until recovered"}
                    </td>
                    <td className="px-3 py-3">
                      <RuleStatusBadge status={status} />
                    </td>
                    <td className="px-3 py-3 text-xs">
                      <p>
                        From:{" "}
                        <span className="font-medium">{formatDateTime(rule.effectiveFrom)}</span>
                      </p>
                      <p className="text-text-secondary">
                        Until:{" "}
                        <span className="font-medium">
                          {rule.effectiveTo ? formatDateTime(rule.effectiveTo) : "Indefinite"}
                        </span>
                      </p>
                    </td>
                    <td className="px-3 py-3 text-right">
                      <div className="flex justify-end gap-2">
                        <button
                          type="button"
                          onClick={() => setEditingRule(rule)}
                          title="Edit rule"
                          aria-label="Edit rule"
                          className="inline-flex items-center gap-1.5 rounded-md border border-border-default bg-white px-2.5 py-1.5 text-xs font-semibold text-neutral-600 transition hover:bg-neutral-50 hover:text-neutral-900 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-700"
                        >
                          <Pencil className="size-4" aria-hidden="true" />
                          <span>Edit</span>
                        </button>

                        {canDelete && (
                          <button
                            type="button"
                            onClick={() => setDeletingRule(rule)}
                            aria-label="Delete rule"
                            title="Delete future scheduled rule"
                            className="inline-flex items-center gap-1.5 rounded-md border border-border-default bg-white px-2.5 py-1.5 text-xs font-semibold text-neutral-600 transition hover:bg-neutral-50 hover:text-neutral-900 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-700"
                          >
                            <Trash2 className="size-4" aria-hidden="true" />
                            <span>Delete</span>
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      <PenaltyRuleModal
        isOpen={isCreateOpen || editingRule !== null}
        rule={editingRule}
        existingRules={data}
        suggestedOccurrence={suggestedOccurrence}
        onClose={() => {
          setIsCreateOpen(false);
          setEditingRule(null);
        }}
        onCreate={createRule}
        onSave={saveRule}
      />

      <SharedModal
        isOpen={deletingRule !== null}
        onClose={() => {
          if (!isDeleting) setDeletingRule(null);
        }}
        title="Delete scheduled penalty rule"
        maxWidthClass="max-w-md"
      >
        {deletingRule && (
          <div className="space-y-4">
            <p className="text-sm text-text-secondary">
              Are you sure you want to delete the scheduled penalty rule for{" "}
              <strong className="text-text-primary">
                no-show #{deletingRule.minimumOccurrence}
              </strong>
              ?
            </p>
            <div className="rounded-lg border border-border-default bg-neutral-50 p-3 text-xs text-text-secondary space-y-1">
              <p>
                Points deducted:{" "}
                <strong className="text-danger">-{deletingRule.scoreDeduction}</strong>
              </p>
              <p>
                Scheduled from: <strong>{formatDateTime(deletingRule.effectiveFrom)}</strong>
              </p>
              {deletingRule.effectiveTo && (
                <p>
                  Scheduled until: <strong>{formatDateTime(deletingRule.effectiveTo)}</strong>
                </p>
              )}
            </div>
            <div className="flex justify-end gap-3 pt-2">
              <button
                type="button"
                disabled={isDeleting}
                onClick={() => setDeletingRule(null)}
                className="rounded-md border border-border-default bg-white px-4 py-2 text-sm font-semibold text-text-secondary hover:bg-neutral-50 disabled:opacity-50"
              >
                Cancel
              </button>
              <button
                type="button"
                disabled={isDeleting}
                onClick={() => void confirmDeleteRule()}
                className="rounded-md bg-danger px-4 py-2 text-sm font-semibold text-white hover:bg-red-700 disabled:opacity-60"
              >
                {isDeleting ? "Deleting..." : "Delete rule"}
              </button>
            </div>
          </div>
        )}
      </SharedModal>
    </section>
  );
}
