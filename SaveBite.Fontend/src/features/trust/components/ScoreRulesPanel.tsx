import { LoadingSkeleton } from "@/shared/components/ui/LoadingSkeleton";
import { useCallback, useState } from "react";
import { Pencil } from "lucide-react";
import { Link } from "react-router-dom";
import { getApiErrorMessage } from "@/shared/api";
import { toast } from "@/shared/stores/toastStore";
import { adminTrustApi } from "../api/adminTrustApi";
import { useApiResource } from "../hooks/useApiResource";
import type { TrustScoreRule, UpdateScoreRulePayload } from "../types/adminTrust.types";
import { PanelEmpty, PanelError } from "./PanelState";
import { ScoreRuleModal } from "./ScoreRuleModal";
import { CurrentPenaltyPolicy } from "./CurrentPenaltyPolicy";

export function ScoreRulesPanel({ variant }: { variant: "all" | "earning" }) {
  const fetchRules = useCallback(() => adminTrustApi.getEarningRules(), []);
  const { data, error, reload } = useApiResource("admin-trust:earning-fixed", fetchRules);
  const [editingRule, setEditingRule] = useState<TrustScoreRule | null>(null);
  const saveRule = async (id: string, payload: UpdateScoreRulePayload) => {
    try {
      await adminTrustApi.updateEarningRule(id, payload);
      toast.success("Earning rule updated.");
      reload();
      return null;
    } catch (caught) {
      return getApiErrorMessage(caught);
    }
  };
  if (error && !data) return <PanelError message={error} onRetry={reload} />;
  if (!data) return <LoadingSkeleton variant="stats" rows={2} label="Loading trust information" />;
  return (
    <section className="space-y-6">
      <div>
        <h3 className="text-base font-bold text-text-primary">Order completion points</h3>
        <p className="mt-1 text-sm text-text-secondary">
          Two system rules. Edit points and status together; changes apply immediately.
        </p>
      </div>
      <div className="grid gap-4 sm:grid-cols-2">
        {data.map((rule) => (
          <div
            key={rule.id}
            className="min-w-0 space-y-3 rounded-lg border border-border-default bg-white p-4"
          >
            <div className="flex items-start justify-between gap-3">
              <h4 className="min-w-0 font-semibold">
                {rule.ruleType === "OnlineOrderCompleted"
                  ? "Online order completed"
                  : "Pay-at-shop order completed"}
              </h4>
              <button
                type="button"
                onClick={() => setEditingRule(rule)}
                className="inline-flex min-h-9 shrink-0 items-center gap-1.5 rounded-md bg-primary-50 px-3 text-xs font-semibold text-primary-700 transition-colors hover:bg-primary-100 active:bg-primary-200 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-700"
              >
                <Pencil className="size-3.5" aria-hidden="true" />
                Edit
              </button>
            </div>
            <div className="flex flex-wrap items-center justify-between gap-3">
              <p className="text-xl font-bold text-primary-700">+{rule.scoreDelta} points</p>
              <span
                className={`rounded-full px-3 py-1 text-xs font-semibold ${rule.isActive ? "bg-primary-50 text-primary-700" : "bg-neutral-100 text-neutral-600"}`}
              >
                {rule.isActive ? "Active" : "Inactive"}
              </span>
            </div>
          </div>
        ))}
      </div>
      {!data.length && (
        <PanelEmpty
          title="No earning rules"
          description="System earning defaults are unavailable."
        />
      )}
      {variant === "all" && (
        <div className="space-y-3">
          <CurrentPenaltyPolicy admin />
          <Link
            to="/admin/trust/penalty-rules"
            className="inline-block text-sm font-semibold text-primary-700 hover:underline"
          >
            Manage penalty rules
          </Link>
        </div>
      )}
      <ScoreRuleModal rule={editingRule} onClose={() => setEditingRule(null)} onSave={saveRule} />
    </section>
  );
}
