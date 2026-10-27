import { useEffect, useState, useCallback, type FormEvent } from "react";
import {
  Percent,
  Calendar,
  Save,
  Clock,
  ShieldAlert,
  FileCheck2,
  RefreshCw,
  Sliders,
  Receipt,
  Layers,
  ArrowRight,
  Info,
  Search,
  Sparkles,
  Store,
  Eye,
  Edit3,
} from "lucide-react";
import { platformFeeApi } from "@/features/platform-fees/api/platformFeeApi";
import type {
  PlatformFeeConfigResponse,
  PlatformFeeStatementSummary,
} from "@/features/platform-fees/types/platformFee.types";
import { normalizeStatementStatus } from "@/features/platform-fees/utils/platformFeeUtils";
import { StatusBadge } from "@/features/platform-fees/components/StatusBadge";
import { ManualReconciliationModal } from "@/features/platform-fees/components/ManualReconciliationModal";
import { AdminStatementDetailModal } from "@/features/platform-fees/components/AdminStatementDetailModal";
import { Pagination } from "@/shared/components/pagination";
import { toast } from "@/shared/stores/toastStore";
import { getApiErrorMessage } from "@/shared/api/httpClient";

const formatVnd = (amount: number) => {
  return new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
  }).format(amount);
};

const formatDateTime = (dateStr?: string | null) => {
  if (!dateStr) return "N/A";
  try {
    const d = new Date(dateStr);
    return d.toLocaleString("en-US", {
      year: "numeric",
      month: "short",
      day: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
    });
  } catch {
    return dateStr;
  }
};

export function PlatformFeeManagementPage() {
  const [config, setConfig] = useState<PlatformFeeConfigResponse | null>(null);
  const [allConfigs, setAllConfigs] = useState<PlatformFeeConfigResponse[]>([]);
  const [loadingConfig, setLoadingConfig] = useState(true);
  const [isUpdating, setIsUpdating] = useState(false);

  // Form states for updating config
  const [feeRatePercent, setFeeRatePercent] = useState<string>("10");
  const [effectiveDate, setEffectiveDate] = useState<string>("");
  const [isEditingUpcoming, setIsEditingUpcoming] = useState(false);

  // Statements list state for Admin
  const [statements, setStatements] = useState<PlatformFeeStatementSummary[]>([]);
  const [loadingStatements, setLoadingStatements] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [filterYear, setFilterYear] = useState<number | string>(new Date().getFullYear());
  const [filterMonth, setFilterMonth] = useState<string>("All");
  const [filterStatus, setFilterStatus] = useState<string>("All");
  const [page, setPage] = useState<number>(1);
  const [totalPages, setTotalPages] = useState<number>(1);
  const [totalCount, setTotalCount] = useState<number>(0);

  // Operation states
  const [isProcessingOverdue, setIsProcessingOverdue] = useState(false);
  const [isGeneratingMonthly, setIsGeneratingMonthly] = useState(false);
  const [genYear, setGenYear] = useState<number>(new Date().getFullYear());
  const [genMonth, setGenMonth] = useState<number>(new Date().getMonth() + 1);

  // Detail Modal state
  const [selectedDetailStatementId, setSelectedDetailStatementId] = useState<string | null>(null);

  // Reconciliation modal state
  const [showReconcileModal, setShowReconcileModal] = useState(false);
  const [selectedStatementItem, setSelectedStatementItem] =
    useState<PlatformFeeStatementSummary | null>(null);

  const loadConfigs = useCallback(async () => {
    setLoadingConfig(true);
    try {
      const [active, all] = await Promise.all([
        platformFeeApi.getActiveConfig(),
        platformFeeApi.getAllConfigs().catch(() => []),
      ]);
      setConfig(active);
      setAllConfigs(all);
      setFeeRatePercent((active.feePercentage ?? active.feeRate * 100).toString());
    } catch (error) {
      toast.error(getApiErrorMessage(error) || "Unable to load platform fee configuration.");
    } finally {
      setLoadingConfig(false);
    }
  }, []);

  const loadStatements = useCallback(async () => {
    setLoadingStatements(true);
    try {
      const result = await platformFeeApi.getAdminStatements({
        year: filterYear === "All" ? undefined : Number(filterYear),
        month: filterMonth === "All" ? undefined : parseInt(filterMonth),
        status: filterStatus === "All" ? undefined : filterStatus,
        search: searchTerm.trim() || undefined,
        page,
        pageSize: 10,
      });
      setStatements(result.items || []);
      setTotalPages(result.totalPages || 1);
      setTotalCount(result.totalItems ?? result.totalCount ?? (result.items?.length || 0));
    } catch (error) {
      toast.error(getApiErrorMessage(error) || "Unable to load platform fee statements.");
    } finally {
      setLoadingStatements(false);
    }
  }, [filterYear, filterMonth, filterStatus, searchTerm, page]);

  useEffect(() => {
    void loadConfigs();
  }, [loadConfigs]);

  useEffect(() => {
    void loadStatements();
  }, [loadStatements]);

  const handleUpdateConfig = async (e: FormEvent) => {
    e.preventDefault();
    const rateNumber = parseFloat(feeRatePercent);
    if (isNaN(rateNumber) || rateNumber < 0 || rateNumber > 100) {
      toast.error("Fee rate percentage must be between 0% and 100%.");
      return;
    }

    setIsUpdating(true);
    try {
      const effectiveFromIso = effectiveDate ? new Date(effectiveDate).toISOString() : undefined;

      await platformFeeApi.updateConfig({
        feeRate: rateNumber,
        effectiveFrom: effectiveFromIso,
      });

      toast.success(
        isEditingUpcoming || (effectiveFromIso && new Date(effectiveFromIso).getTime() > Date.now())
          ? "Upcoming scheduled fee rate updated successfully!"
          : "Platform fee configuration updated successfully!",
      );
      await loadConfigs();
      setEffectiveDate("");
      setIsEditingUpcoming(false);
    } catch (error) {
      toast.error(getApiErrorMessage(error) || "Failed to update platform fee configuration.");
    } finally {
      setIsUpdating(false);
    }
  };

  const handleProcessOverdue = async () => {
    setIsProcessingOverdue(true);
    try {
      const suspendedCount = await platformFeeApi.processOverdue();
      if (suspendedCount > 0) {
        toast.success(
          `Overdue check complete: Marked overdue statements and suspended ${suspendedCount} delinquent shop(s).`,
        );
      } else {
        toast.info("Overdue check complete: No overdue fee statements past grace period found.");
      }
      await loadStatements();
    } catch (error) {
      toast.error(getApiErrorMessage(error) || "Failed to process overdue fee statements.");
    } finally {
      setIsProcessingOverdue(false);
    }
  };

  const handleGenerateMonthly = async () => {
    setIsGeneratingMonthly(true);
    try {
      const count = await platformFeeApi.generateMonthly(genYear, genMonth);
      if (count > 0) {
        toast.success(
          `Consolidated and generated ${count} fee statement(s) for period ${genMonth}/${genYear}!`,
        );
      } else {
        toast.info(`No unbilled completed order fees found for period ${genMonth}/${genYear}.`);
      }
      // Auto switch filter to generated month & year to view results immediately
      setFilterYear(genYear.toString());
      setFilterMonth(genMonth.toString());
      setPage(1);
    } catch (error) {
      toast.error(getApiErrorMessage(error) || "Failed to generate monthly statements.");
    } finally {
      setIsGeneratingMonthly(false);
    }
  };

  const openReconcileForStatement = (stmt: PlatformFeeStatementSummary) => {
    setSelectedStatementItem(stmt);
    setShowReconcileModal(true);
  };

  // Find latest upcoming scheduled config if any (sorted by newest created)
  const upcomingConfig = allConfigs
    .filter((c) => new Date(c.effectiveFrom).getTime() > Date.now())
    .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())[0];

  return (
    <div className="space-y-8 pb-12">
      {/* Header */}
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-black text-slate-900 tracking-tight sm:text-3xl">
            Platform Fee Management
          </h1>
          <p className="mt-1 text-sm text-slate-500">
            Configure global commission rates and oversee periodic statement reconciliation
          </p>
        </div>
        <button
          onClick={() => {
            void loadConfigs();
            void loadStatements();
          }}
          disabled={loadingConfig || loadingStatements}
          className="inline-flex items-center gap-2 self-start rounded-xl border border-slate-200 bg-white px-4 py-2 text-xs font-semibold text-slate-700 shadow-xs transition hover:bg-slate-50 disabled:opacity-50"
        >
          <RefreshCw
            className={`h-3.5 w-3.5 ${loadingConfig || loadingStatements ? "animate-spin" : ""}`}
          />
          Refresh
        </button>
      </div>

      {/* Main Grid: Active Config Hero + Update Form */}
      <div className="grid grid-cols-1 gap-6 lg:grid-cols-12">
        {/* Active Config Overview Card (5 cols) */}
        <div className="lg:col-span-5 space-y-4">
          <div className="relative overflow-hidden rounded-3xl bg-gradient-to-br from-emerald-600 via-teal-600 to-emerald-800 p-7 text-white shadow-xl ring-1 ring-black/5">
            <div className="absolute -right-6 -bottom-6 h-36 w-36 rounded-full bg-white/10 blur-xl pointer-events-none" />
            <div className="relative z-10 space-y-6">
              <div className="flex items-center justify-between">
                <span className="inline-flex items-center gap-1.5 rounded-full bg-white/20 px-3 py-1 text-xs font-bold backdrop-blur-md">
                  <span className="h-2 w-2 rounded-full bg-emerald-300 animate-pulse" />
                  Currently Active
                </span>
                <span className="flex h-10 w-10 items-center justify-center rounded-2xl bg-white/15 backdrop-blur-md shadow-inner">
                  <Percent className="h-5 w-5 text-white" />
                </span>
              </div>

              <div>
                <p className="text-xs font-medium uppercase tracking-wider text-emerald-100">
                  Current Commission Rate
                </p>
                <div className="mt-1 flex items-baseline gap-2">
                  <span className="text-5xl font-black tracking-tight">
                    {loadingConfig
                      ? "..."
                      : `${config ? (config.feePercentage ?? config.feeRate * 100).toFixed(1) : 0}%`}
                  </span>
                  <span className="text-sm text-emerald-100">of completed order gross</span>
                </div>
              </div>

              <div className="space-y-2.5 rounded-2xl bg-black/15 p-4 text-xs backdrop-blur-xs ring-1 ring-white/10">
                <div className="flex items-center justify-between">
                  <span className="text-emerald-100 flex items-center gap-1.5">
                    <Calendar className="h-3.5 w-3.5" /> Effective From:
                  </span>
                  <span className="font-semibold text-white">
                    {config?.effectiveFrom ? formatDateTime(config.effectiveFrom) : "N/A"}
                  </span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-emerald-100 flex items-center gap-1.5">
                    <Clock className="h-3.5 w-3.5" /> Config Created:
                  </span>
                  <span className="font-semibold text-white">
                    {config?.createdAt ? formatDateTime(config.createdAt) : "N/A"}
                  </span>
                </div>
              </div>

              <div className="flex items-start gap-2 text-xs text-emerald-100/90 leading-relaxed">
                <Info className="h-4 w-4 shrink-0 mt-0.5 text-emerald-200" />
                <span>
                  This fee rate applies automatically to <strong>all stores</strong> when customer
                  orders transition to Completed status.
                </span>
              </div>
            </div>
          </div>

          {/* Upcoming Scheduled Configuration Card */}
          {upcomingConfig && (
            <div className="rounded-2xl border border-amber-200 bg-amber-50/70 p-4 shadow-xs">
              <div className="flex items-start gap-3">
                <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-xl bg-amber-100 text-amber-700">
                  <Sparkles className="h-4 w-4" />
                </span>
                <div className="flex-1 text-xs">
                  <div className="flex items-center justify-between">
                    <span className="font-bold uppercase tracking-wider text-amber-800">
                      Upcoming Scheduled Rate
                    </span>
                    <div className="flex items-center gap-1.5">
                      <button
                        type="button"
                        onClick={() => {
                          setIsEditingUpcoming(true);
                          setFeeRatePercent(
                            (
                              upcomingConfig.feePercentage ?? upcomingConfig.feeRate * 100
                            ).toString(),
                          );
                          const dateObj = new Date(upcomingConfig.effectiveFrom);
                          const localIso = new Date(
                            dateObj.getTime() - dateObj.getTimezoneOffset() * 60000,
                          )
                            .toISOString()
                            .slice(0, 16);
                          setEffectiveDate(localIso);
                          document
                            .getElementById("platform-fee-form")
                            ?.scrollIntoView({ behavior: "smooth" });
                        }}
                        className="inline-flex items-center gap-1.5 rounded-lg border border-amber-300 bg-white/90 px-2.5 py-1 text-xs font-bold text-amber-900 shadow-2xs hover:bg-white transition"
                        title="Edit scheduled rate"
                      >
                        <Edit3 className="h-3.5 w-3.5" /> Edit
                      </button>
                    </div>
                  </div>
                  <p className="mt-1 font-semibold text-amber-950">
                    Rate:{" "}
                    {(upcomingConfig.feePercentage ?? upcomingConfig.feeRate * 100).toFixed(1)}%
                  </p>
                  <p className="text-slate-600">
                    Takes effect on:{" "}
                    <span className="font-medium text-slate-900">
                      {formatDateTime(upcomingConfig.effectiveFrom)}
                    </span>
                  </p>
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Update Configuration Form (7 cols) */}
        <div className="lg:col-span-7">
          <div className="rounded-3xl border border-slate-200/80 bg-white p-7 shadow-xs">
            <div className="flex items-center gap-3 mb-6">
              <span className="flex h-10 w-10 items-center justify-center rounded-2xl bg-emerald-50 text-emerald-600">
                <Sliders className="h-5 w-5" />
              </span>
              <div>
                <h2 className="text-lg font-bold text-slate-900">Configure Platform Fee</h2>
                <p className="text-xs text-slate-500">
                  Update platform commission percentage and optional scheduled effective date
                </p>
              </div>
            </div>

            {isEditingUpcoming && upcomingConfig && (
              <div className="mb-5 flex items-center justify-between rounded-2xl border border-amber-300 bg-amber-50 p-3.5 text-xs text-amber-900">
                <span className="flex items-center gap-2 font-medium">
                  <Sparkles className="h-4 w-4 text-amber-600 shrink-0" />
                  Editing scheduled rate (
                  {upcomingConfig.feePercentage ?? upcomingConfig.feeRate * 100}% on{" "}
                  {formatDateTime(upcomingConfig.effectiveFrom)})
                </span>
                <button
                  type="button"
                  onClick={() => {
                    setIsEditingUpcoming(false);
                    setEffectiveDate("");
                    if (config) {
                      setFeeRatePercent((config.feePercentage ?? config.feeRate * 100).toString());
                    }
                  }}
                  className="rounded-lg bg-amber-200/80 px-2.5 py-1 text-[11px] font-bold text-amber-950 hover:bg-amber-300 transition"
                >
                  Reset / Cancel Edit
                </button>
              </div>
            )}

            <form id="platform-fee-form" onSubmit={handleUpdateConfig} className="space-y-5">
              <div>
                <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-2">
                  Commission Fee Rate (%)
                </label>
                <div className="relative rounded-2xl shadow-xs">
                  <input
                    type="number"
                    step="0.1"
                    min="0"
                    max="100"
                    required
                    value={feeRatePercent}
                    onChange={(e) => setFeeRatePercent(e.target.value)}
                    placeholder="e.g. 10.0"
                    className="block w-full rounded-2xl border border-slate-200 bg-slate-50/50 px-4 py-3 text-sm font-semibold text-slate-800 placeholder-slate-400 transition focus:border-emerald-500 focus:bg-white focus:outline-none focus:ring-4 focus:ring-emerald-500/10"
                  />
                  <div className="pointer-events-none absolute inset-y-0 right-0 flex items-center pr-4">
                    <span className="font-bold text-slate-400">%</span>
                  </div>
                </div>
                <p className="mt-1.5 text-xs text-slate-400">
                  Enter a value between 0% and 100%. (e.g. 10 corresponds to 10% commission on
                  completed orders).
                </p>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-2">
                  Effective Start Date & Time
                </label>
                <div className="relative">
                  <input
                    type="datetime-local"
                    value={effectiveDate}
                    onChange={(e) => setEffectiveDate(e.target.value)}
                    className="block w-full rounded-2xl border border-slate-200 bg-slate-50/50 px-4 py-3 text-sm font-medium text-slate-800 transition focus:border-emerald-500 focus:bg-white focus:outline-none focus:ring-4 focus:ring-emerald-500/10"
                  />
                </div>
                <p className="mt-1.5 text-xs text-slate-400">
                  Leave blank to apply the new fee structure immediately, or pick a future date &
                  time to schedule.
                </p>
              </div>

              <div className="pt-2">
                <button
                  type="submit"
                  disabled={isUpdating}
                  className="inline-flex w-full items-center justify-center gap-2 rounded-2xl bg-slate-900 px-6 py-3.5 text-sm font-bold text-white shadow-md transition-all hover:bg-slate-800 active:scale-98 disabled:opacity-50"
                >
                  {isUpdating ? (
                    <>
                      <RefreshCw className="h-4 w-4 animate-spin" /> Updating configuration...
                    </>
                  ) : (
                    <>
                      <Save className="h-4 w-4" /> Apply New Fee Structure
                    </>
                  )}
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>

      {/* Operational Actions Section */}
      <div className="space-y-4">
        <div className="flex items-center gap-2">
          <Layers className="h-5 w-5 text-slate-700" />
          <h2 className="text-lg font-bold text-slate-900">Operational Actions - Fee Statements</h2>
        </div>

        <div className="grid grid-cols-1 gap-5 md:grid-cols-3">
          {/* Action 1: Generate Monthly Statements */}
          <div className="rounded-2xl border border-slate-200/80 bg-white p-5 shadow-xs flex flex-col justify-between">
            <div className="space-y-3">
              <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-blue-50 text-blue-600">
                <FileCheck2 className="h-5 w-5" />
              </span>
              <h3 className="text-sm font-bold text-slate-900">Generate Monthly Statement</h3>
              <p className="text-xs text-slate-500 leading-normal">
                Consolidate sales data and issue monthly fee statements for all shops.
              </p>
              <div className="grid grid-cols-2 gap-2 pt-1">
                <div>
                  <label className="text-xs font-semibold text-slate-600">Month:</label>
                  <select
                    value={genMonth}
                    onChange={(e) => setGenMonth(parseInt(e.target.value))}
                    className="mt-1 block w-full rounded-xl border border-slate-200 bg-slate-50 px-3 py-1.5 text-xs font-semibold text-slate-700"
                  >
                    {Array.from({ length: 12 }, (_, i) => i + 1).map((m) => (
                      <option key={m} value={m}>
                        Month {m}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="text-xs font-semibold text-slate-600">Year:</label>
                  <input
                    type="number"
                    value={genYear}
                    onChange={(e) => setGenYear(parseInt(e.target.value))}
                    className="mt-1 block w-full rounded-xl border border-slate-200 bg-slate-50 px-3 py-1.5 text-xs font-semibold text-slate-700"
                  />
                </div>
              </div>
            </div>
            <button
              onClick={handleGenerateMonthly}
              disabled={isGeneratingMonthly}
              className="mt-4 inline-flex items-center justify-center gap-1.5 rounded-xl bg-blue-600 px-4 py-2.5 text-xs font-bold text-white shadow-xs transition hover:bg-blue-700 disabled:opacity-50"
            >
              {isGeneratingMonthly ? (
                <RefreshCw className="h-3.5 w-3.5 animate-spin" />
              ) : (
                <ArrowRight className="h-3.5 w-3.5" />
              )}
              Generate Statements for {genMonth}/{genYear}
            </button>
          </div>

          {/* Action 2: Check and Suspend Overdue Shops */}
          <div className="rounded-2xl border border-slate-200/80 bg-white p-5 shadow-xs flex flex-col justify-between">
            <div className="space-y-3">
              <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-rose-50 text-rose-600">
                <ShieldAlert className="h-5 w-5" />
              </span>
              <h3 className="text-sm font-bold text-slate-900">Check and Suspend Overdue Shops</h3>
              <p className="text-xs text-slate-500 leading-normal">
                Automatically check overdue fee statements and suspend shops that have failed to pay
                within grace period.
              </p>
            </div>
            <button
              onClick={handleProcessOverdue}
              disabled={isProcessingOverdue}
              className="mt-4 inline-flex items-center justify-center gap-1.5 rounded-xl bg-rose-600 px-4 py-2.5 text-xs font-bold text-white shadow-xs transition hover:bg-rose-700 disabled:opacity-50"
            >
              {isProcessingOverdue ? (
                <RefreshCw className="h-3.5 w-3.5 animate-spin" />
              ) : (
                <ShieldAlert className="h-3.5 w-3.5" />
              )}
              Run Overdue Shop Check
            </button>
          </div>

          {/* Action 3: Manual Reconciliation */}
          <div className="rounded-2xl border border-slate-200/80 bg-white p-5 shadow-xs flex flex-col justify-between">
            <div className="space-y-3">
              <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-emerald-50 text-emerald-600">
                <Receipt className="h-5 w-5" />
              </span>
              <h3 className="text-sm font-bold text-slate-900">Manual Reconciliation</h3>
              <p className="text-xs text-slate-500 leading-normal">
                Record cash payments or direct bank transfers to Admin, debit accounts, and
                automatically unlock suspended shops.
              </p>
            </div>
            <button
              onClick={() => {
                setSelectedStatementItem(null);
                setShowReconcileModal(true);
              }}
              className="mt-4 inline-flex items-center justify-center gap-1.5 rounded-xl border border-slate-200 bg-slate-900 px-4 py-2.5 text-xs font-bold text-white shadow-xs transition hover:bg-slate-800"
            >
              <Receipt className="h-3.5 w-3.5" />
              Record Manual Payment
            </button>
          </div>
        </div>
      </div>

      {/* Statements Table for Admin */}
      <div className="space-y-4">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex items-center gap-2">
            <Store className="h-5 w-5 text-slate-700" />
            <h2 className="text-lg font-bold text-slate-900">Shop Platform Fee Statements</h2>
          </div>

          {/* Filters */}
          <div className="flex flex-wrap items-center gap-2">
            <div className="relative">
              <Search className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-slate-400" />
              <input
                type="text"
                placeholder="Search shop or ID..."
                value={searchTerm}
                onChange={(e) => {
                  setSearchTerm(e.target.value);
                  setPage(1);
                }}
                className="rounded-xl border border-slate-200 bg-white py-1.5 pl-8 pr-3 text-xs text-slate-700 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-emerald-500"
              />
            </div>

            <select
              value={filterYear}
              onChange={(e) => {
                setFilterYear(e.target.value === "All" ? "All" : parseInt(e.target.value));
                setPage(1);
              }}
              className="rounded-xl border border-slate-200 bg-white px-3 py-1.5 text-xs font-semibold text-slate-700 focus:outline-none focus:ring-2 focus:ring-emerald-500"
            >
              <option value="All">All Years</option>
              {[2024, 2025, 2026, 2027].map((y) => (
                <option key={y} value={y}>
                  Year {y}
                </option>
              ))}
            </select>

            <select
              value={filterMonth}
              onChange={(e) => {
                setFilterMonth(e.target.value);
                setPage(1);
              }}
              className="rounded-xl border border-slate-200 bg-white px-3 py-1.5 text-xs font-semibold text-slate-700 focus:outline-none focus:ring-2 focus:ring-emerald-500"
            >
              <option value="All">All Months</option>
              {Array.from({ length: 12 }, (_, i) => i + 1).map((m) => (
                <option key={m} value={m}>
                  Month {m}
                </option>
              ))}
            </select>

            <select
              value={filterStatus}
              onChange={(e) => {
                setFilterStatus(e.target.value);
                setPage(1);
              }}
              className="rounded-xl border border-slate-200 bg-white px-3 py-1.5 text-xs font-semibold text-slate-700 focus:outline-none focus:ring-2 focus:ring-emerald-500"
            >
              <option value="All">All Statuses</option>
              <option value="Issued">Issued</option>
              <option value="Paid">Paid</option>
              <option value="Overdue">Overdue</option>
            </select>
          </div>
        </div>

        {/* Table */}
        <div className="overflow-hidden rounded-2xl border border-slate-200/80 bg-white shadow-xs">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-slate-50/80 text-slate-500 uppercase tracking-wider font-semibold border-b border-slate-200/80">
                <tr>
                  <th className="px-5 py-4">Shop Name</th>
                  <th className="px-5 py-4">Billing Period</th>
                  <th className="px-5 py-4">Fee Amount</th>
                  <th className="px-5 py-4">Status</th>
                  <th className="px-5 py-4">Due Date</th>
                  <th className="px-5 py-4">Paid Date</th>
                  <th className="px-5 py-4 text-right">Action</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {loadingStatements ? (
                  <tr>
                    <td colSpan={7} className="py-12 text-center text-slate-400">
                      <RefreshCw className="mx-auto h-6 w-6 animate-spin text-emerald-600 mb-2" />
                      Loading statements data...
                    </td>
                  </tr>
                ) : statements.length === 0 ? (
                  <tr>
                    <td colSpan={7} className="py-12 text-center text-slate-400">
                      <Receipt className="mx-auto h-8 w-8 text-slate-300 mb-2" />
                      No fee statements found for the selected criteria.
                    </td>
                  </tr>
                ) : (
                  statements.map((stmt) => {
                    const norm = normalizeStatementStatus(stmt.status);
                    const isPending = norm === "Issued" || norm === "Overdue";
                    return (
                      <tr key={stmt.id} className="hover:bg-slate-50/60 transition-colors">
                        <td className="px-5 py-4 font-bold text-slate-900">
                          {stmt.shopName || "Shop"}
                          <div className="font-mono text-[10px] text-slate-400 font-normal">
                            {stmt.id.substring(0, 18)}...
                          </div>
                        </td>
                        <td className="px-5 py-4 font-medium text-slate-700">
                          Month {stmt.billingMonth}/{stmt.billingYear}
                        </td>
                        <td className="px-5 py-4 font-extrabold text-slate-900">
                          {formatVnd(stmt.totalFeeAmount)}
                        </td>
                        <td className="px-5 py-4">
                          <StatusBadge status={stmt.status} />
                        </td>
                        <td className="px-5 py-4 text-slate-600">
                          {stmt.dueAt ? formatDateTime(stmt.dueAt) : "—"}
                        </td>
                        <td className="px-5 py-4 text-slate-600">
                          {stmt.paidAt ? formatDateTime(stmt.paidAt) : "—"}
                        </td>
                        <td className="px-5 py-4 text-right">
                          <div className="inline-flex items-center justify-end gap-1.5">
                            <button
                              type="button"
                              onClick={() => setSelectedDetailStatementId(stmt.id)}
                              className="inline-flex items-center gap-1 rounded-lg border border-slate-200 bg-white px-2.5 py-1.5 text-xs font-semibold text-slate-700 shadow-2xs hover:bg-slate-50 transition"
                            >
                              <Eye className="h-3 w-3 text-slate-500" />
                              Details
                            </button>

                            {isPending ? (
                              <button
                                type="button"
                                onClick={() => openReconcileForStatement(stmt)}
                                className="inline-flex items-center gap-1 rounded-lg bg-emerald-600 px-2.5 py-1.5 text-xs font-semibold text-white shadow-2xs hover:bg-emerald-700 transition"
                              >
                                <Receipt className="h-3 w-3" />
                                Reconcile
                              </button>
                            ) : (
                              <span className="text-xs text-slate-400 italic px-1">Settled</span>
                            )}
                          </div>
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="border-t border-slate-100 bg-slate-50/50 p-4">
              <Pagination
                currentPage={page}
                totalPages={totalPages}
                totalItems={totalCount}
                pageSize={10}
                onPageChange={(p) => setPage(p)}
                itemLabel="statements"
              />
            </div>
          )}
        </div>
      </div>

      {/* Manual Reconciliation Modal */}
      <ManualReconciliationModal
        isOpen={showReconcileModal}
        onClose={() => {
          setShowReconcileModal(false);
          setSelectedStatementItem(null);
        }}
        statements={statements}
        initialStatement={selectedStatementItem}
        onSuccess={() => void loadStatements()}
      />

      {/* Admin Statement Detail Modal */}
      <AdminStatementDetailModal
        isOpen={Boolean(selectedDetailStatementId)}
        statementId={selectedDetailStatementId}
        onClose={() => setSelectedDetailStatementId(null)}
        onOpenReconcile={(stmtId) => {
          const target = statements.find((s) => s.id === stmtId);
          if (target) {
            openReconcileForStatement(target);
          }
        }}
      />
    </div>
  );
}
