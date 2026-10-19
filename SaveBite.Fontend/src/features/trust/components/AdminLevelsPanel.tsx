import { useCallback, useState } from "react";
import { getApiErrorMessage } from "@/shared/api";
import { toast } from "@/shared/stores/toastStore";
import { adminTrustApi } from "../api/adminTrustApi";
import { useApiResource } from "../hooks/useApiResource";
import type { AdminTrustLevel, UpdateLevelRequirementPayload } from "../types/adminTrust.types";
import { formatCurrency } from "../utils/format";
import { LevelBenefitsModal } from "./LevelBenefitsModal";
import { LevelRequirementModal } from "./LevelRequirementModal";
import { PanelEmpty, PanelError, PanelSkeleton } from "./PanelState";

const editButton =
  "rounded-md border border-border-default bg-white px-3 py-1.5 text-xs font-semibold text-text-secondary transition-colors hover:bg-neutral-50";

function ActiveBadge({ isActive }: { isActive: boolean }) {
  return (
    <span
      className={`inline-flex items-center rounded-full border px-2.5 py-1 text-[11px] font-semibold ${
        isActive
          ? "border-primary-100 bg-primary-50 text-primary-700"
          : "border-neutral-200 bg-neutral-50 text-neutral-600"
      }`}
    >
      {isActive ? "Active" : "Inactive"}
    </span>
  );
}

// UC-129 / UC-130 (benefits) and UC-131 / UC-132 (requirements).
export function AdminLevelsPanel() {
  const fetchLevels = useCallback(() => adminTrustApi.getLevels(), []);
  const { data, error, reload } = useApiResource("admin-trust:levels", fetchLevels);

  const [benefitsLevel, setBenefitsLevel] = useState<AdminTrustLevel | null>(null);
  const [requirementLevel, setRequirementLevel] = useState<AdminTrustLevel | null>(null);

  const saveBenefits = async (levelId: string, description: string | null) => {
    try {
      await adminTrustApi.updateLevelBenefits(levelId, { description });
      toast.success("Level benefits updated.");
      reload();
      return null;
    } catch (caught) {
      return getApiErrorMessage(caught);
    }
  };

  const saveRequirement = async (levelId: string, payload: UpdateLevelRequirementPayload) => {
    try {
      await adminTrustApi.updateLevelRequirement(levelId, payload);
      toast.success("Level requirements updated.");
      reload();
      return null;
    } catch (caught) {
      return getApiErrorMessage(caught);
    }
  };

  if (error && !data) return <PanelError message={error} onRetry={reload} />;
  if (!data) return <PanelSkeleton rows={3} />;
  if (data.length === 0)
    return <PanelEmpty title="No levels yet" description="Trust levels have not been set up." />;

  return (
    <div className="space-y-8">
      <section className="space-y-3">
        <div>
          <h3 className="text-base font-bold text-text-primary">Level benefits</h3>
          <p className="text-sm text-text-secondary">
            What customers on each level see as their benefits.
          </p>
        </div>
        <div className="overflow-x-auto rounded-xl border border-border-default bg-bg-surface">
          <table className="min-w-full divide-y divide-border-default text-left text-sm">
            <thead className="bg-neutral-50 text-text-secondary">
              <tr>
                <th className="px-5 py-3 font-semibold">Level</th>
                <th className="px-5 py-3 font-semibold">Benefits</th>
                <th className="px-5 py-3 text-right font-semibold">Customers</th>
                <th className="px-5 py-3 font-semibold">Status</th>
                <th className="px-5 py-3 text-right font-semibold">
                  <span className="sr-only">Actions</span>
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border-default text-text-primary">
              {data.map((level) => (
                <tr key={level.id}>
                  <td className="px-5 py-3">
                    <p className="font-semibold">{level.name}</p>
                    <p className="text-xs text-text-secondary">Rank {level.rank}</p>
                  </td>
                  <td className="max-w-[360px] px-5 py-3">
                    {level.description ? (
                      <p className="line-clamp-2 whitespace-pre-line text-xs leading-relaxed text-text-secondary">
                        {level.description}
                      </p>
                    ) : (
                      <span className="text-xs italic text-text-muted">No benefits listed</span>
                    )}
                  </td>
                  <td className="px-5 py-3 text-right tabular-nums">{level.userCount}</td>
                  <td className="px-5 py-3">
                    <ActiveBadge isActive={level.isActive} />
                  </td>
                  <td className="px-5 py-3 text-right">
                    <button
                      type="button"
                      className={editButton}
                      onClick={() => setBenefitsLevel(level)}
                    >
                      Edit benefits
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <section className="space-y-3">
        <div>
          <h3 className="text-base font-bold text-text-primary">Level requirements</h3>
          <p className="text-sm text-text-secondary">
            What a customer must reach to be on each level.
          </p>
        </div>
        <div className="overflow-x-auto rounded-xl border border-border-default bg-bg-surface">
          <table className="min-w-full divide-y divide-border-default text-left text-sm">
            <thead className="bg-neutral-50 text-text-secondary">
              <tr>
                <th className="px-5 py-3 font-semibold">Level</th>
                <th className="px-5 py-3 text-right font-semibold">Min score</th>
                <th className="px-5 py-3 text-right font-semibold">Min completed orders</th>
                <th className="px-5 py-3 text-right font-semibold">Min order value</th>
                <th className="px-5 py-3 text-right font-semibold">
                  <span className="sr-only">Actions</span>
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border-default text-text-primary">
              {data.map((level) => (
                <tr key={level.id}>
                  <td className="px-5 py-3 font-semibold">{level.name}</td>
                  <td className="px-5 py-3 text-right tabular-nums">
                    {level.requirement?.minScore ?? 0}
                  </td>
                  <td className="px-5 py-3 text-right tabular-nums">
                    {level.requirement?.minCompletedOrders ?? 0}
                  </td>
                  <td className="px-5 py-3 text-right tabular-nums">
                    {formatCurrency(level.requirement?.minTotalOrderValue ?? 0)}
                  </td>
                  <td className="px-5 py-3 text-right">
                    <button
                      type="button"
                      className={editButton}
                      onClick={() => setRequirementLevel(level)}
                    >
                      Edit requirements
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <LevelBenefitsModal
        level={benefitsLevel}
        onClose={() => setBenefitsLevel(null)}
        onSave={saveBenefits}
      />
      <LevelRequirementModal
        level={requirementLevel}
        onClose={() => setRequirementLevel(null)}
        onSave={saveRequirement}
      />
    </div>
  );
}
