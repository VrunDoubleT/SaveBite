import { LoadingSkeleton } from "@/shared/components/ui/LoadingSkeleton";
import { useCallback } from "react";
import { httpClient } from "@/shared/api";
import type { ApiResponse } from "@/shared/types";
import { adminTrustApi } from "../api/adminTrustApi";
import { useApiResource } from "../hooks/useApiResource";
import type { PenaltyRule } from "../types/adminTrust.types";
import { formatCurrency } from "../utils/format";
import { PanelError } from "./PanelState";

type Policy = Pick<
  PenaltyRule,
  | "minimumOccurrence"
  | "scoreDeduction"
  | "requiredOnlineOrders"
  | "requiredOnlineOrderValue"
  | "permanentOfflineLock"
>;
export function CurrentPenaltyPolicy({ admin = false }: { admin?: boolean }) {
  const fetchPolicy = useCallback(async (): Promise<Policy[]> => {
    if (admin)
      return (await adminTrustApi.getPenaltyRules()).filter((r) => r.isActive && r.isCurrent);
    const response = await httpClient.get<ApiResponse<Policy[]>>("/trust-score/penalty-policy");
    return response.data.data;
  }, [admin]);
  const { data, error, reload } = useApiResource(
    `trust:current-penalty-policy:${admin}`,
    fetchPolicy,
  );
  return (
    <section className="space-y-3" aria-label="Current no-show penalty policy">
      <h3 className="text-base font-bold text-text-primary">Current no-show penalty policy</h3>
      <p className="text-sm text-text-secondary">
        Occurrences are counted per month. The highest applicable threshold applies, including
        subsequent no-shows above the highest threshold.
      </p>
      {error ? (
        <PanelError message={error} onRetry={reload} />
      ) : !data ? (
        <LoadingSkeleton variant="levels" rows={2} label="Loading trust information" />
      ) : !data.length ? (
        <p className="rounded-lg border border-dashed p-4 text-sm text-text-secondary">
          No penalty policy is currently in effect.
        </p>
      ) : (
        <div className="grid gap-3 sm:grid-cols-2">
          {data.map((rule) => (
            <div
              key={rule.minimumOccurrence}
              className="min-w-0 space-y-2 rounded-lg border border-border-default bg-white p-4"
            >
              <h4 className="font-semibold">From no-show #{rule.minimumOccurrence}</h4>
              <p className="font-semibold text-danger">−{rule.scoreDeduction} trust points</p>
              {rule.permanentOfflineLock ? (
                <p className="text-sm text-danger">
                  Pay at shop permanently unavailable. No automatic recovery.
                </p>
              ) : (
                <p className="text-sm text-text-secondary">
                  Recover with {rule.requiredOnlineOrders} completed online orders and at least{" "}
                  {formatCurrency(rule.requiredOnlineOrderValue)} in total. Full online payment is
                  required while recovering.
                </p>
              )}
            </div>
          ))}
        </div>
      )}
    </section>
  );
}
