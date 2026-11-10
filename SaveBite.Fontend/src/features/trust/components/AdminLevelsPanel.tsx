import { useCallback, useMemo, useState } from "react";
import { History, Pencil, Plus, Power } from "lucide-react";
import { getApiErrorMessage } from "@/shared/api";
import { toast } from "@/shared/stores/toastStore";
import { adminTrustApi } from "../api/adminTrustApi";
import { useApiResource } from "../hooks/useApiResource";
import type {
  AdminTrustLevel,
  CreateTrustLevelPayload,
  UpdateLevelBenefitsPayload,
  UpdateLevelInfoPayload,
  UpdateLevelRequirementPayload,
  UpdateLevelStatusPayload,
} from "../types/adminTrust.types";
import type { TrustLevelDepositPolicy } from "../types/trust.types";
import { formatCurrency, formatDateTime } from "../utils/format";
import { RecordId } from "@/shared/components/RecordId";
import { CreateLevelModal } from "./CreateLevelModal";
import { LevelBenefitsModal } from "./LevelBenefitsModal";
import { LevelHistoryDrawer } from "./LevelHistoryDrawer";
import { LevelInfoModal } from "./LevelInfoModal";
import { LevelRequirementModal } from "./LevelRequirementModal";
import { LevelStatusModal } from "./LevelStatusModal";
import { PanelEmpty, PanelError, PanelSkeleton } from "./PanelState";

const actionButton =
  "inline-flex items-center gap-1.5 rounded-md border border-border-default bg-white px-2.5 py-1.5 text-xs font-semibold text-text-secondary transition-colors hover:bg-neutral-50 disabled:cursor-not-allowed disabled:opacity-50";

const inactiveRowClass =
  "bg-neutral-200/80 text-neutral-500 transition-colors";

function ActiveBadge({ isActive }: { isActive: boolean }) {
  return (
    <span
      className={`inline-flex items-center rounded-full border px-2.5 py-0.5 text-[11px] font-semibold ${
        isActive
          ? "border-primary-100 bg-primary-50 text-primary-700"
          : "border-neutral-400/60 bg-neutral-300/80 text-neutral-700"
      }`}
    >
      {isActive ? "Active" : "Inactive"}
    </span>
  );
}

function DepositPolicySummary({
  policy,
  isActive = true,
}: {
  policy?: TrustLevelDepositPolicy | null;
  isActive?: boolean;
}) {
  if (!policy) {
    return <span className="text-xs italic text-text-muted">Not configured</span>;
  }

  if (policy.requireFullOnlinePayment) {
    return (
      <div className="space-y-1">
        <span
          className={`inline-flex items-center rounded-full border px-2.5 py-0.5 text-[11px] font-semibold ${
            isActive
              ? "border-amber-200 bg-amber-50 text-amber-800"
              : "border-neutral-300 bg-neutral-200 text-neutral-600"
          }`}
        >
          100% online payment required
        </span>
        <p className={`text-xs ${isActive ? "text-text-secondary" : "text-neutral-500"}`}>
          Pay at shop is not available on this level.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-1.5 text-xs">
      <div
        className={`flex flex-wrap gap-x-3 gap-y-1 ${
          isActive ? "text-text-secondary" : "text-neutral-500"
        }`}
      >
        <span>
          Free deposit:{" "}
          <strong className={isActive ? "text-text-primary" : "text-neutral-600"}>
            &le; {formatCurrency(policy.depositFreeOrderAmount ?? 0)}
          </strong>
        </span>
        <span>
          Max Pay at shop:{" "}
          <strong className={isActive ? "text-text-primary" : "text-neutral-600"}>
            {formatCurrency(policy.maxDepositOrderAmount ?? 0)}
          </strong>
        </span>
      </div>
      {policy.ranges.length > 0 && (
        <ul className={`space-y-0.5 ${isActive ? "text-text-secondary" : "text-neutral-500"}`}>
          {policy.ranges.map((range) => (
            <li key={range.id} className="tabular-nums">
              {formatCurrency(range.fromOrderAmount)} &ndash; {formatCurrency(range.toOrderAmount)}:{" "}
              <span
                className={`font-semibold ${
                  isActive ? "text-primary-700" : "text-neutral-600"
                }`}
              >
                {formatCurrency(range.depositAmount)} deposit
              </span>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

// UC-129 .. UC-132 & UC-152 .. UC-155.
export function AdminLevelsPanel() {
  const fetchLevels = useCallback(() => adminTrustApi.getLevels(), []);
  const { data, error, reload } = useApiResource("admin-trust:levels", fetchLevels);

  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [infoLevel, setInfoLevel] = useState<AdminTrustLevel | null>(null);
  const [statusLevel, setStatusLevel] = useState<AdminTrustLevel | null>(null);
  const [historyLevel, setHistoryLevel] = useState<AdminTrustLevel | null>(null);
  const [benefitsLevel, setBenefitsLevel] = useState<AdminTrustLevel | null>(null);
  const [requirementLevel, setRequirementLevel] = useState<AdminTrustLevel | null>(null);

  const lowestActiveLevelId = useMemo(() => {
    if (!data) return null;
    const active = data.filter((l) => l.isActive).sort((a, b) => a.rank - b.rank);
    return active[0]?.id ?? null;
  }, [data]);

  const createLevel = async (payload: CreateTrustLevelPayload) => {
    try {
      await adminTrustApi.createLevel(payload);
      toast.success("New Trust Level created.");
      reload();
      return null;
    } catch (caught) {
      return getApiErrorMessage(caught);
    }
  };

  const saveInfo = async (levelId: string, payload: UpdateLevelInfoPayload) => {
    try {
      await adminTrustApi.updateLevelInfo(levelId, payload);
      toast.success("Level name updated.");
      reload();
      return null;
    } catch (caught) {
      return getApiErrorMessage(caught);
    }
  };

  const saveStatus = async (levelId: string, payload: UpdateLevelStatusPayload) => {
    try {
      await adminTrustApi.updateLevelStatus(levelId, payload);
      toast.success(payload.isActive ? "Level reactivated." : "Level deactivated.");
      reload();
      return null;
    } catch (caught) {
      return getApiErrorMessage(caught);
    }
  };

  const saveBenefits = async (levelId: string, payload: UpdateLevelBenefitsPayload) => {
    try {
      await adminTrustApi.updateLevelBenefits(levelId, payload);
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
      {/* 1. UC-152: View Level List + UC-153, UC-154, UC-155 */}
      <section className="space-y-3">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h3 className="text-base font-bold text-text-primary">Trust Levels</h3>
            <p className="text-sm text-text-secondary">
              Overview of all levels, customer distribution, thresholds, status, and version history.
            </p>
          </div>

          <button
            type="button"
            onClick={() => setIsCreateOpen(true)}
            className="inline-flex items-center gap-2 rounded-md bg-primary-600 px-4 py-2 text-xs font-semibold text-white shadow-sm transition-colors hover:bg-primary-700"
          >
            <Plus className="size-4" />
            Add new level
          </button>
        </div>

        <div className="overflow-x-auto rounded-xl border border-border-default bg-bg-surface">
          <table className="min-w-full divide-y divide-border-default text-left text-sm">
            <thead className="bg-neutral-50 text-text-secondary">
              <tr>
                <th className="px-5 py-3 font-semibold">ID</th>
                <th className="px-5 py-3 font-semibold">Level</th>
                <th className="px-5 py-3 font-semibold">Status</th>
                <th className="px-5 py-3 text-right font-semibold">Customers</th>
                <th className="px-5 py-3 font-semibold">Thresholds</th>
                <th className="px-5 py-3 font-semibold">Updated</th>
                <th className="px-5 py-3 text-right font-semibold">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border-default text-text-primary">
              {data.map((level) => {
                const isLowestActive = level.isActive && level.id === lowestActiveLevelId;
                const active = level.isActive;
                return (
                  <tr key={level.id} className={!active ? inactiveRowClass : ""}>
                    <td className="max-w-40 px-5 py-3.5">
                      <RecordId id={level.id} />
                    </td>
                    <td className="px-5 py-3.5">
                      <p className={`font-semibold ${active ? "text-text-primary" : "text-neutral-600 line-through decoration-neutral-400"}`}>
                        {level.name}
                      </p>
                      <p className={`text-xs ${active ? "text-text-secondary" : "text-neutral-500"}`}>
                        Rank {level.rank}
                      </p>
                    </td>
                    <td className="px-5 py-3.5">
                      <ActiveBadge isActive={active} />
                    </td>
                    <td className="px-5 py-3.5 text-right font-semibold tabular-nums">
                      {level.userCount}
                    </td>
                    <td className={`px-5 py-3.5 text-xs ${active ? "text-text-secondary" : "text-neutral-500"}`}>
                      <span className="tabular-nums">
                        Score &ge;{" "}
                        <strong className={active ? "text-text-primary" : "text-neutral-600"}>
                          {level.requirement?.minScore ?? 0}
                        </strong>
                      </span>
                      {" · "}
                      <span className="tabular-nums">
                        Orders &ge;{" "}
                        <strong className={active ? "text-text-primary" : "text-neutral-600"}>
                          {level.requirement?.minCompletedOrders ?? 0}
                        </strong>
                      </span>
                      {" · "}
                      <span className="tabular-nums">
                        Value &ge;{" "}
                        <strong className={active ? "text-text-primary" : "text-neutral-600"}>
                          {formatCurrency(level.requirement?.minTotalOrderValue ?? 0)}
                        </strong>
                      </span>
                    </td>
                    <td className={`whitespace-nowrap px-5 py-3.5 text-xs ${active ? "text-text-secondary" : "text-neutral-500"}`}>
                      {formatDateTime(level.updatedAt)}
                    </td>
                    <td className="px-5 py-3.5 text-right">
                      <div className="flex flex-wrap justify-end gap-2">
                        <button
                          type="button"
                          className={actionButton}
                          onClick={() => setInfoLevel(level)}
                          title="Rename level"
                        >
                          <Pencil className="size-3.5" />
                          Rename
                        </button>
                        <button
                          type="button"
                          className={actionButton}
                          onClick={() => setHistoryLevel(level)}
                          title="View version history"
                        >
                          <History className="size-3.5" />
                          History
                        </button>
                        <button
                          type="button"
                          disabled={isLowestActive}
                          title={
                            isLowestActive
                              ? "The lowest active level cannot be deactivated (BR-158)."
                              : active
                                ? "Deactivate level"
                                : "Reactivate level"
                          }
                          onClick={() => setStatusLevel(level)}
                          className={`inline-flex items-center gap-1.5 rounded-md border px-2.5 py-1.5 text-xs font-semibold transition-colors disabled:cursor-not-allowed disabled:opacity-40 ${
                            active
                              ? "border-red-200 bg-white text-danger hover:bg-red-50"
                              : "border-primary-300 bg-white text-primary-700 hover:bg-primary-50"
                          }`}
                        >
                          <Power className="size-3.5" />
                          {active ? "Deactivate" : "Reactivate"}
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </section>

      {/* 2. UC-129 / UC-130: Level benefits & deposit policy */}
      <section className="space-y-3">
        <div>
          <h3 className="text-base font-bold text-text-primary">Level benefits &amp; deposit policy</h3>
          <p className="text-sm text-text-secondary">
            Manage benefit descriptions and fixed Pay at shop deposit policies for each level.
          </p>
        </div>

        <div className="overflow-x-auto rounded-xl border border-border-default bg-bg-surface">
          <table className="min-w-full divide-y divide-border-default text-left text-sm">
            <thead className="bg-neutral-50 text-text-secondary">
              <tr>
                <th className="px-5 py-3 font-semibold">ID</th>
                <th className="px-5 py-3 font-semibold">Level</th>
                <th className="px-5 py-3 font-semibold">Benefits description</th>
                <th className="px-5 py-3 font-semibold">Pay at shop deposit policy</th>
                <th className="px-5 py-3 text-right font-semibold">
                  <span className="sr-only">Actions</span>
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border-default text-text-primary">
              {data.map((level) => {
                const active = level.isActive;
                return (
                  <tr
                    key={level.id}
                    className={`align-top ${!active ? inactiveRowClass : ""}`}
                  >
                    <td className="max-w-40 px-5 py-3.5">
                      <RecordId id={level.id} />
                    </td>
                    <td className="px-5 py-3.5">
                      <div className="flex flex-wrap items-center gap-2">
                        <p className={`font-semibold ${active ? "text-text-primary" : "text-neutral-600 line-through decoration-neutral-400"}`}>
                          {level.name}
                        </p>
                        {!active && <ActiveBadge isActive={false} />}
                      </div>
                      <p className={`mt-0.5 text-xs ${active ? "text-text-secondary" : "text-neutral-500"}`}>
                        Rank {level.rank}
                      </p>
                    </td>
                    <td className="max-w-[280px] px-5 py-3.5">
                      {level.description ? (
                        <p
                          className={`line-clamp-3 whitespace-pre-line text-xs leading-relaxed ${
                            active ? "text-text-secondary" : "text-neutral-500"
                          }`}
                        >
                          {level.description}
                        </p>
                      ) : (
                        <span className="text-xs italic text-text-muted">No benefits listed</span>
                      )}
                    </td>
                    <td className="min-w-[260px] px-5 py-3.5">
                      <DepositPolicySummary policy={level.depositPolicy} isActive={active} />
                    </td>
                    <td className="px-5 py-3.5 text-right">
                      <button
                        type="button"
                        className={actionButton}
                        onClick={() => setBenefitsLevel(level)}
                      >
                        Edit benefits
                      </button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </section>

      {/* 3. UC-131 / UC-132: Level requirements */}
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
                <th className="px-5 py-3 font-semibold">ID</th>
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
              {data.map((level) => {
                const active = level.isActive;
                return (
                  <tr key={level.id} className={!active ? inactiveRowClass : ""}>
                    <td className="max-w-40 px-5 py-3">
                      <RecordId id={level.id} />
                    </td>
                    <td className="px-5 py-3">
                      <div className="flex flex-wrap items-center gap-2">
                        <span className={`font-semibold ${active ? "text-text-primary" : "text-neutral-600 line-through decoration-neutral-400"}`}>
                          {level.name}
                        </span>
                        <span className={`text-xs ${active ? "text-text-secondary" : "text-neutral-500"}`}>
                          (Rank {level.rank})
                        </span>
                        {!active && <ActiveBadge isActive={false} />}
                      </div>
                    </td>
                    <td className={`px-5 py-3 text-right tabular-nums ${active ? "" : "text-neutral-500"}`}>
                      {level.requirement?.minScore ?? 0}
                    </td>
                    <td className={`px-5 py-3 text-right tabular-nums ${active ? "" : "text-neutral-500"}`}>
                      {level.requirement?.minCompletedOrders ?? 0}
                    </td>
                    <td className={`px-5 py-3 text-right tabular-nums ${active ? "" : "text-neutral-500"}`}>
                      {formatCurrency(level.requirement?.minTotalOrderValue ?? 0)}
                    </td>
                    <td className="px-5 py-3 text-right">
                      <button
                        type="button"
                        className={actionButton}
                        onClick={() => setRequirementLevel(level)}
                      >
                        Edit requirements
                      </button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </section>

      <CreateLevelModal
        isOpen={isCreateOpen}
        existingLevels={data}
        onClose={() => setIsCreateOpen(false)}
        onCreate={createLevel}
      />

      <LevelInfoModal
        level={infoLevel}
        onClose={() => setInfoLevel(null)}
        onSave={saveInfo}
      />

      <LevelStatusModal
        level={statusLevel}
        onClose={() => setStatusLevel(null)}
        onConfirm={saveStatus}
      />

      <LevelHistoryDrawer
        level={historyLevel}
        onClose={() => setHistoryLevel(null)}
      />

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