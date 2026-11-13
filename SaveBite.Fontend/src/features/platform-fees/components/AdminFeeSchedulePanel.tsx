import { useCallback, useEffect, useState, type FormEvent } from "react";
import {
  CalendarClock,
  CheckCircle2,
  Clock3,
  LoaderCircle,
  Pencil,
  RefreshCw,
  X,
} from "lucide-react";
import { platformFeeApi } from "../api/platformFeeApi";
import type { PlatformFeeConfigResponse } from "../types/platformFee.types";
import { getApiErrorMessage } from "@/shared/api/httpClient";
import { toast } from "@/shared/stores/toastStore";

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
  const [rows, setRows] = useState<PlatformFeeConfigResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [loadError, setLoadError] = useState("");
  const [error, setError] = useState("");
  const [mode, setMode] = useState<"scheduled" | "now">("scheduled");
  const [rate, setRate] = useState("");
  const [effective, setEffective] = useState(nextMonth);
  const [reason, setReason] = useState("");
  const [editing, setEditing] = useState<PlatformFeeConfigResponse | null>(null);
  const [futurePolicy, setFuturePolicy] = useState<"keep" | "cancel">("keep");
  const [cancelling, setCancelling] = useState<PlatformFeeConfigResponse | null>(null);
  const [cancelReason, setCancelReason] = useState("");
  const [cancelError, setCancelError] = useState("");
  const upcoming = rows
    .filter((r) => r.status === "Scheduled")
    .sort((a, b) => Date.parse(a.effectiveFrom) - Date.parse(b.effectiveFrom));
  const active = rows.find((r) => r.status === "Active");
  const history = rows.filter((r) => r.status !== "Active" && r.status !== "Scheduled");
  const limit = rows[0]?.maxFutureSchedules ?? 5;
  const full = mode === "scheduled" && !editing && upcoming.length >= limit;
  const zone = Intl.DateTimeFormat().resolvedOptions().timeZone;

  const load = useCallback(async () => {
    try {
      setRows(await platformFeeApi.getAllConfigs());
      setLoadError("");
    } catch (e) {
      setLoadError(getApiErrorMessage(e) || "Unable to load fee schedules. Please refresh.");
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
    setRate("");
    setMode("scheduled");
    setEffective(nextMonth());
    setReason("");
    setFuturePolicy("keep");
    setError("");
  }
  async function submit(event: FormEvent) {
    event.preventDefault();
    if (saving) return;
    setError("");
    if (!rate.trim() || !Number.isFinite(Number(rate)) || Number(rate) < 0 || Number(rate) > 100) {
      setError("Enter a commission percentage between 0 and 100.");
      return;
    }
    if (reason.trim().length < 3) {
      setError("Enter a reason of at least 3 characters.");
      return;
    }
    if (mode === "scheduled" && (!effective || Date.parse(effective) <= Date.now())) {
      setError("Choose a future start date and time.");
      return;
    }
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
      setError(getApiErrorMessage(e) || "Unable to save fee schedule.");
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
          <h2 className="text-lg font-bold text-slate-900">Commission fee schedules</h2>
          <p className="mt-1 text-sm text-slate-600">
            Each order keeps the rate in effect when it was completed. Times shown in {zone}.
          </p>
        </div>
        <button type="button" className={buttonClass} disabled={saving} onClick={() => void load()}>
          <RefreshCw size={16} aria-hidden="true" /> Refresh schedules
        </button>
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
              {loading ? "…" : active ? active.feePercentage : "—"}
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
          {loading && (
            <p
              role="status"
              className="mt-5 rounded-xl bg-slate-50 px-4 py-6 text-center text-sm text-slate-500"
            >
              Loading schedules…
            </p>
          )}

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
                        setRate(String(item.feePercentage));
                        setMode("scheduled");
                        setEffective(localInput(new Date(item.effectiveFrom)));
                        setReason("");
                        setError("");
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
          onSubmit={submit}
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
                required
                value={rate}
                onChange={(e) => setRate(e.target.value)}
                placeholder="e.g. 5 for 5%"
              />
            </label>
            {!editing && (
              <label className="block text-sm font-medium text-slate-800">
                When to apply
                <select
                  className={inputClass}
                  value={mode}
                  onChange={(e) => setMode(e.target.value as "scheduled" | "now")}
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
                  required
                  value={effective}
                  onChange={(e) => setEffective(e.target.value)}
                />
              </label>
            ) : (
              <div className="rounded-xl border border-amber-200 bg-amber-50 p-3 text-sm text-amber-900">
                The current version ends when you save. Previously recorded order fees are
                preserved.
                {upcoming.length > 0 && (
                  <label className="mt-3 block font-medium">
                    Existing future schedules
                    <select
                      className={inputClass}
                      value={futurePolicy}
                      onChange={(e) => setFuturePolicy(e.target.value as "keep" | "cancel")}
                    >
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
                required
                value={reason}
                onChange={(e) => setReason(e.target.value)}
              />
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
            {error && (
              <p role="alert" className="text-sm text-red-700">
                {error}
              </p>
            )}
            <div className="flex flex-wrap gap-2 pt-1">
              <button
                type="submit"
                disabled={saving || full || loading || Boolean(loadError)}
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
              {history.map((item) => (
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
                      Created by {item.createdByName || item.createdBy} · {dateTime(item.createdAt)}
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
      </details>
    </section>
  );
}
