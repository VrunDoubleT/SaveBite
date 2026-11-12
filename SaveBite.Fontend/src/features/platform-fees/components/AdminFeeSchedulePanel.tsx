import { InlineSkeleton } from "@/shared/components/ui/LoadingSkeleton";
import { LoadingSkeleton } from "@/shared/components/ui/LoadingSkeleton";
import { useForm, useWatch } from "react-hook-form";
import { useCallback, useEffect, useState, type FormEvent } from "react";
import { CalendarClock, CheckCircle2, Clock3, LoaderCircle, Pencil, X } from "lucide-react";
import { platformFeeApi } from "../api/platformFeeApi";
import type { PlatformFeeConfigResponse } from "../types/platformFee.types";
import { getApiErrorMessage } from "@/shared/api/httpClient";
import { toast } from "@/shared/stores/toastStore";
import { Pagination } from "@/shared/components/pagination";

const HISTORY_PAGE_SIZE = 5;
interface FeeVersionForm {
  rate: string;
  mode: "scheduled" | "now";
  effective: string;
  reason: string;
  futurePolicy: "keep" | "cancel";
}

function validateEffective(value: string, values: FeeVersionForm) {
  return (
    values.mode !== "scheduled" ||
    (Number.isFinite(Date.parse(value)) && Date.parse(value) > Date.now()) ||
    "Choose a future start date and time."
  );
}

const dateTime = (value?: string | null) =>
  value ? new Date(value).toLocaleString() : "Open-ended";
const localInput = (value: Date) =>
  new Date(value.getTime() - value.getTimezoneOffset() * 60_000).toISOString().slice(0, 16);
const nextMonth = () => {
  const d = new Date();
  return localInput(new Date(d.getFullYear(), d.getMonth() + 1, 1));
};
const inputClass =
  "mt-2 min-h-11 w-full rounded-xl border border-slate-300 bg-white px-3 py-2 text-sm text-slate-900 focus:outline-none focus-visible:ring-2 focus-visible:ring-emerald-600 disabled:opacity-60";
const buttonClass =
  "inline-flex min-h-11 items-center justify-center gap-2 rounded-xl border border-slate-200 px-3 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-emerald-600 disabled:cursor-not-allowed disabled:opacity-50";
const statusClass: Record<PlatformFeeConfigResponse["status"], string> = {
  Active: "bg-emerald-100 text-emerald-800",
  Scheduled: "bg-blue-100 text-blue-800",
  Ended: "bg-slate-100 text-slate-700",
  Cancelled: "bg-red-50 text-red-800",
  Superseded: "bg-amber-50 text-amber-900",
};

export function AdminFeeSchedulePanel() {
  const [historyPage, setHistoryPage] = useState(1);
  const [rows, setRows] = useState<PlatformFeeConfigResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [loadError, setLoadError] = useState("");
  const [editing, setEditing] = useState<PlatformFeeConfigResponse | null>(null);
  const {
    register,
    control,
    handleSubmit,
    reset: resetForm,
    setError,
    clearErrors,
    formState: { errors, isSubmitting },
  } = useForm<FeeVersionForm>({
    defaultValues: {
      rate: "",
      mode: "scheduled",
      effective: nextMonth(),
      reason: "",
      futurePolicy: "keep",
    },
    mode: "onTouched",
  });
  const mode = useWatch({ control, name: "mode" });
  const [cancelling, setCancelling] = useState<PlatformFeeConfigResponse | null>(null);
  const [cancelReason, setCancelReason] = useState("");
  const [cancelError, setCancelError] = useState("");
  const upcoming = rows
    .filter((r) => r.status === "Scheduled")
    .sort((a, b) => Date.parse(a.effectiveFrom) - Date.parse(b.effectiveFrom));
  const active = rows.find((r) => r.status === "Active");
  const history = rows.filter((r) => r.status !== "Active" && r.status !== "Scheduled");
  const historyTotalPages = Math.max(1, Math.ceil(history.length / HISTORY_PAGE_SIZE));
  const currentHistoryPage = Math.min(historyPage, historyTotalPages);
  const visibleHistory = history.slice(
    (currentHistoryPage - 1) * HISTORY_PAGE_SIZE,
    currentHistoryPage * HISTORY_PAGE_SIZE,
  );
  const limit = rows[0]?.maxFutureSchedules ?? 5;
  const full = mode === "scheduled" && !editing && upcoming.length >= limit;
  const zone = Intl.DateTimeFormat().resolvedOptions().timeZone;

  const load = useCallback(async () => {
    try {
      setRows(await platformFeeApi.getAllConfigs());
      setHistoryPage(1);
      setLoadError("");
    } catch (e) {
      setLoadError(
        getApiErrorMessage(e) ||
          "Unable to load fee schedules. Please reopen this page to try again.",
      );
    } finally {
      setLoading(false);
    }
  }, []);
  useEffect(() => {
    const initial = window.setTimeout(() => void load(), 0);
    const timer = window.setInterval(() => void load(), 60_000);
    return () => {
      window.clearTimeout(initial);
      window.clearInterval(timer);
    };
  }, [load]);

  function reset() {
    setEditing(null);
    resetForm({
      rate: "",
      mode: "scheduled",
      effective: nextMonth(),
      reason: "",
      futurePolicy: "keep",
    });
  }
  async function submit(values: FeeVersionForm) {
    if (saving) return;
    clearErrors("root");
    if (full || loading || loadError) {
      setError("root", {
        message: "Fee schedules are unavailable or all future slots are in use.",
      });
      return;
    }
    const { rate, reason, effective, mode, futurePolicy } = values;
    setSaving(true);
    try {
      await platformFeeApi.updateConfig({
        feeRate: Number(rate),
        reason: reason.trim(),
        effectiveFrom: mode === "scheduled" ? new Date(effective).toISOString() : undefined,
        replacesConfigId: editing?.id,
        cancelUpcomingScheduled: mode === "now" && futurePolicy === "cancel",
      });
      toast.success(
        editing
          ? "Schedule replaced. The previous version remains in history."
          : mode === "now"
            ? "New fee version applied."
            : "Fee schedule created.",
      );
      reset();
      await load();
    } catch (e) {
      setError("root", { message: getApiErrorMessage(e) || "Unable to save fee schedule." });
      await load();
    } finally {
      setSaving(false);
    }
  }
  async function cancel(event: FormEvent) {
    event.preventDefault();
    if (!cancelling || saving) return;
    setSaving(true);
    setCancelError("");
    try {
      await platformFeeApi.cancelScheduledConfig(cancelling.id, cancelReason.trim());
      toast.success("Schedule cancelled. Other schedules are unchanged.");
      if (editing?.id === cancelling.id) reset();
      setCancelling(null);
      setCancelReason("");
      await load();
    } catch (e) {
      setCancelError(getApiErrorMessage(e) || "Unable to cancel schedule.");
      await load();
    } finally {
      setSaving(false);
    }
  }

  return (
    <section className="space-y-4" aria-label="Commission fee schedules">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-neutral-900">
            Commission Fee Schedules
          </h1>
          <p className="mt-1.5 text-sm text-text-secondary">
            Each order keeps the rate in effect when it was completed. Times shown in {zone}.
          </p>
        </div>
      </div>

      {loadError && (
        <p
          role="alert"
          className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-800"
        >
          {loadError}
        </p>
      )}

      <div className="relative overflow-hidden rounded-2xl bg-gradient-to-r from-emerald-800 via-emerald-700 to-teal-700 px-5 py-5 text-white shadow-md shadow-emerald-900/10 sm:px-7 sm:py-6">
        <div
          aria-hidden="true"
          className="pointer-events-none absolute -right-10 -top-20 size-64 rounded-full border-[32px] border-white/5"
        />
        <div className="relative flex flex-col gap-5 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <p className="inline-flex items-center gap-2 rounded-full bg-white/15 px-3 py-1.5 text-sm font-semibold text-white ring-1 ring-inset ring-white/15">
              <CheckCircle2 size={17} aria-hidden="true" />
              Currently applied
            </p>
            <p className="mt-3 text-sm text-emerald-50">
              This rate is applied to newly completed orders.
            </p>
            {active && (
              <p className="mt-1.5 text-xs font-medium text-emerald-100">
                Effective {dateTime(active.effectiveFrom)}
                {active.effectiveTo ? ` · Ends ${dateTime(active.effectiveTo)}` : " · No end date"}
              </p>
            )}
          </div>
          <div className="flex items-baseline gap-1 border-t border-white/20 pt-4 sm:border-l sm:border-t-0 sm:pl-8 sm:pt-0">
            <p className="text-5xl font-bold tracking-tight tabular-nums text-white sm:text-6xl">
              {loading ? (
                <InlineSkeleton label="Loading current rate" className="w-16" />
              ) : active ? (
                active.feePercentage
              ) : (
                "—"
              )}
            </p>
            {!loading && active && (
              <span className="text-2xl font-semibold text-emerald-100">%</span>
            )}
            {!loading && !active && (
              <span className="ml-2 text-sm font-semibold text-emerald-50">No active rate</span>
            )}
          </div>
        </div>
      </div>

      <div className="grid items-stretch gap-4 xl:grid-cols-2">
        <section className="flex min-w-0 flex-col rounded-2xl border border-slate-200 bg-white p-4 sm:p-5">
          <div className="flex flex-wrap items-start justify-between gap-3">
            <div>
              <h3 className="flex items-center gap-2 font-bold text-slate-900">
                <CalendarClock size={18} aria-hidden="true" />
                Upcoming schedules
                <span className="sr-only">
                  {" "}
                  ({upcoming.length}/{limit})
                </span>
              </h3>
              <p className="mt-1 text-xs font-medium text-slate-500">
                {upcoming.length} of {limit} slots used
              </p>
            </div>
            <span className="rounded-full bg-blue-50 px-2.5 py-1 text-xs font-semibold text-blue-800">
              {upcoming.length}/{limit}
            </span>
          </div>
          <p className="mt-3 max-w-prose text-sm leading-5 text-slate-600">
            Rates take effect automatically in chronological order. Replacing or cancelling a
            schedule reconnects the timeline.
          </p>

          {!loading && upcoming.length === 0 && (
            <p className="mt-5 rounded-xl border border-dashed border-slate-200 bg-slate-50 px-4 py-6 text-center text-sm text-slate-500">
              No upcoming changes.
            </p>
          )}
          {loading && <LoadingSkeleton variant="list" label="Loading schedules…" />}

          <ol className="mt-3 divide-y divide-slate-100">
            {upcoming.map((item) => (
              <li key={item.id} className="py-3 first:pt-2 last:pb-1">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <div className="flex min-w-0 flex-1 items-center gap-3">
                    <p className="shrink-0 text-xl font-bold tabular-nums text-slate-900">
                      {item.feePercentage}%
                    </p>
                    <div className="min-w-0 border-l border-slate-200 pl-3">
                      <p className="text-sm font-medium text-slate-800">
                        {dateTime(item.effectiveFrom)}
                      </p>
                      <p className="text-xs text-slate-500">Until {dateTime(item.effectiveTo)}</p>
                      {item.reason && (
                        <p className="mt-1 break-words text-xs text-slate-600">{item.reason}</p>
                      )}
                    </div>
                  </div>
                  <div className="flex shrink-0 gap-1.5">
                    <button
                      className="inline-flex min-h-9 items-center justify-center gap-1.5 rounded-lg border border-slate-200 px-2.5 text-xs font-semibold text-slate-700 transition hover:bg-slate-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-emerald-600 disabled:cursor-not-allowed disabled:opacity-50"
                      type="button"
                      disabled={saving}
                      aria-label={`Replace ${item.feePercentage}% schedule starting ${dateTime(item.effectiveFrom)}`}
                      onClick={() => {
                        setEditing(item);
                        resetForm({
                          rate: String(item.feePercentage),
                          mode: "scheduled",
                          effective: localInput(new Date(item.effectiveFrom)),
                          reason: "",
                          futurePolicy: "keep",
                        });
                      }}
                    >
                      <Pencil size={13} aria-hidden="true" /> Replace
                    </button>
                    <button
                      className="inline-flex min-h-9 items-center justify-center gap-1.5 rounded-lg border border-slate-200 px-2.5 text-xs font-semibold text-slate-600 transition hover:border-red-200 hover:bg-red-50 hover:text-red-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-red-500 disabled:cursor-not-allowed disabled:opacity-50"
                      type="button"
                      disabled={saving}
                      aria-label={`Cancel ${item.feePercentage}% schedule starting ${dateTime(item.effectiveFrom)}`}
                      onClick={() => {
                        setCancelling(item);
                        setCancelReason("");
                        setCancelError("");
                      }}
                    >
                      <X size={13} aria-hidden="true" /> Cancel
                    </button>
                  </div>
                </div>
                {cancelling?.id === item.id && (
                  <form
                    onSubmit={cancel}
                    className="mt-3 space-y-3 rounded-xl border border-red-200 bg-red-50 p-3"
                  >
                    <p className="text-sm text-red-900">
                      Cancel only this schedule? Its previous fee will continue until the next
                      scheduled change.
                    </p>
                    <label className="block text-sm font-medium text-red-900">
                      Reason for cancellation
                      <input
                        className={inputClass}
                        value={cancelReason}
                        onChange={(e) => setCancelReason(e.target.value)}
                        required
                        minLength={3}
                        maxLength={500}
                        disabled={saving}
                      />
                    </label>
                    {cancelError && (
                      <p role="alert" className="text-sm text-red-800">
                        {cancelError}
                      </p>
                    )}
                    <div className="flex flex-wrap gap-2">
                      <button type="submit" className={buttonClass} disabled={saving}>
                        Confirm cancellation
                      </button>
                      <button
                        type="button"
                        className={buttonClass}
                        disabled={saving}
                        onClick={() => setCancelling(null)}
                      >
                        Keep schedule
                      </button>
                    </div>
                  </form>
                )}
              </li>
            ))}
          </ol>
        </section>

        <form
          noValidate
          onSubmit={handleSubmit(submit)}
          className="min-w-0 rounded-2xl border border-slate-200 bg-white p-4 sm:p-5"
        >
          <div className="mb-4 border-b border-slate-100 pb-3">
            <h3 className="flex items-center gap-2 font-bold text-slate-900">
              <Clock3 size={18} aria-hidden="true" />
              {editing ? "Replace upcoming schedule" : "Create fee version"}
            </h3>
            <p className="mt-1 text-sm text-slate-500">
              Set the commission percentage and when it should take effect.
            </p>
          </div>
          {editing && (
            <p className="mb-3 rounded-xl bg-amber-50 p-3 text-sm text-amber-900">
              Replacing {editing.feePercentage}% from {dateTime(editing.effectiveFrom)}. The
              previous version will remain in history.
            </p>
          )}
          <fieldset disabled={saving} className="space-y-3">
            <label className="block text-sm font-medium text-slate-800">
              Commission (%)
              <input
                className={inputClass}
                type="number"
                min={0}
                max={100}
                step="0.0001"
                aria-invalid={Boolean(errors.rate)}
                aria-describedby={errors.rate ? "fee-rate-error" : undefined}
                {...register("rate", {
                  validate: {
                    range: (value) =>
                      (value.trim() !== "" &&
                        Number.isFinite(Number(value)) &&
                        Number(value) >= 0 &&
                        Number(value) <= 100) ||
                      "Enter a commission percentage between 0 and 100.",
                    precision: (value) =>
                      Math.abs(Number(value) * 10000 - Math.round(Number(value) * 10000)) <
                        0.000001 || "Use at most 4 decimal places.",
                  },
                })}
                placeholder="e.g. 5 for 5%"
              />
              {errors.rate && (
                <span id="fee-rate-error" role="alert" className="mt-1 block text-sm text-red-700">
                  {errors.rate.message}
                </span>
              )}
            </label>
            {!editing && (
              <label className="block text-sm font-medium text-slate-800">
                When to apply
                <select
                  className={inputClass}
                  {...register("mode", {
                    onChange: () => {
                      clearErrors("effective");
                      clearErrors("root");
                    },
                  })}
                >
                  <option value="scheduled">Schedule for a future date</option>
                  <option value="now">Apply immediately</option>
                </select>
              </label>
            )}
            {mode === "scheduled" ? (
              <label className="block text-sm font-medium text-slate-800">
                Starts at ({zone})
                <input
                  className={inputClass}
                  type="datetime-local"
                  aria-invalid={Boolean(errors.effective)}
                  aria-describedby={errors.effective ? "fee-effective-error" : undefined}
                  {...register("effective", {
                    validate: validateEffective,
                  })}
                />
                {errors.effective && (
                  <span
                    id="fee-effective-error"
                    role="alert"
                    className="mt-1 block text-sm text-red-700"
                  >
                    {errors.effective.message}
                  </span>
                )}
              </label>
            ) : (
              <div className="rounded-xl border border-amber-200 bg-amber-50 p-3 text-sm text-amber-900">
                The current version ends when you save. Previously recorded order fees are
                preserved.
                {upcoming.length > 0 && (
                  <label className="mt-3 block font-medium">
                    Existing future schedules
                    <select className={inputClass} {...register("futurePolicy")}>
                      <option value="keep">Keep all {upcoming.length} future schedules</option>
                      <option value="cancel">Cancel all {upcoming.length} future schedules</option>
                    </select>
                  </label>
                )}
              </div>
            )}
            <label className="block text-sm font-medium text-slate-800">
              Reason for this change
              <textarea
                className={inputClass}
                rows={2}
                minLength={3}
                maxLength={500}
                aria-invalid={Boolean(errors.reason)}
                aria-describedby={errors.reason ? "fee-reason-error" : undefined}
                {...register("reason", {
                  validate: (value) =>
                    (value.trim().length >= 3 && value.trim().length <= 500) ||
                    "Enter a reason between 3 and 500 characters.",
                })}
              />
              {errors.reason && (
                <span
                  id="fee-reason-error"
                  role="alert"
                  className="mt-1 block text-sm text-red-700"
                >
                  {errors.reason.message}
                </span>
              )}
            </label>
            <p className="text-xs leading-5 text-slate-500">
              Admins are notified when a version becomes effective. Shop owners are notified when
              the percentage changes.
            </p>
            {full && (
              <p role="status" className="text-sm text-amber-900">
                All {limit} future slots are in use. Replace or cancel a schedule to add another.
              </p>
            )}
            {errors.root && (
              <p role="alert" className="text-sm text-red-700">
                {errors.root.message}
              </p>
            )}
            <div className="flex flex-wrap gap-2 pt-1">
              <button
                type="submit"
                disabled={saving || isSubmitting || full || loading || Boolean(loadError)}
                className="inline-flex min-h-11 items-center gap-2 rounded-xl bg-emerald-700 px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-800 focus-visible:ring-2 focus-visible:ring-emerald-600 disabled:opacity-50"
              >
                {saving && <LoaderCircle size={16} className="animate-spin" aria-hidden="true" />}
                {saving
                  ? "Saving…"
                  : editing
                    ? "Save replacement"
                    : mode === "now"
                      ? "Apply rate now"
                      : "Schedule rate"}
              </button>
              {editing && (
                <button type="button" disabled={saving} className={buttonClass} onClick={reset}>
                  Discard replacement
                </button>
              )}
            </div>
          </fieldset>
        </form>
      </div>

      <details className="rounded-2xl border border-slate-200 bg-white p-5">
        <summary className="cursor-pointer font-bold text-slate-900">
          Fee version history ({history.length})
        </summary>
        <p className="mt-2 text-sm text-slate-600">
          Ended, cancelled and replaced versions are retained. Each order links to the exact version
          and percentage it used.
        </p>
        <div className="mt-4 overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="border-b text-slate-600">
                <th className="p-3">Version / rate</th>
                <th className="p-3">Status</th>
                <th className="p-3">Effective interval</th>
                <th className="p-3">Tracking</th>
              </tr>
            </thead>
            <tbody>
              {visibleHistory.map((item) => (
                <tr key={item.id} className="border-b border-slate-100 align-top">
                  <td className="p-3">
                    <p className="font-bold">{item.feePercentage}%</p>
                    <span className="mt-1 block font-mono text-xs">{item.id}</span>
                  </td>
                  <td className="p-3">
                    <span
                      className={`rounded-full px-2 py-1 text-xs font-semibold ${statusClass[item.status]}`}
                    >
                      {item.status}
                    </span>
                  </td>
                  <td className="p-3">
                    {dateTime(item.effectiveFrom)}
                    <br />→ {dateTime(item.effectiveTo)}
                  </td>
                  <td className="max-w-sm p-3">
                    <p>{item.reason || "Previous configuration"}</p>
                    <p className="mt-1 text-xs text-slate-500">
                      Created by {item.createdByName || item.createdBy || "System"} ·{" "}
                      {dateTime(item.createdAt)}
                    </p>
                    {item.replacesConfigId && (
                      <p className="mt-1 break-all text-xs text-slate-500">
                        Replaces {item.replacesConfigId}
                      </p>
                    )}
                    {item.closedAt && (
                      <p className="mt-1 text-xs text-slate-600">
                        {item.closureReason} · {dateTime(item.closedAt)} · {item.closedBy}
                      </p>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {history.length === 0 && (
            <p className="p-3 text-sm text-slate-500">No previous versions.</p>
          )}
        </div>
        {history.length > 0 && (
          <div className="mt-4 border-t border-slate-200 pt-4">
            <Pagination
              currentPage={currentHistoryPage}
              totalPages={historyTotalPages}
              onPageChange={setHistoryPage}
              totalItems={history.length}
              pageSize={HISTORY_PAGE_SIZE}
              itemLabel="versions"
              showTotalItems
              showFirstLast
            />
          </div>
        )}
      </details>
    </section>
  );
}
