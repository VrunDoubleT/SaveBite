import { useEffect, useState } from "react";
import { SharedModal } from "@/shared/components/modal/BaseModal";
import { getApiErrorMessage } from "@/shared/api";
import { platformFeeApi } from "../api/platformFeeApi";
import type { PlatformFeePeriodStatistics } from "../types/platformFee.types";

const money = (amount: number) => new Intl.NumberFormat("vi-VN", { style: "currency", currency: "VND" }).format(amount);

export function PlatformFeeStatisticsModal({ onClose }: { onClose: () => void }) {
  const [period, setPeriod] = useState(() => {
    const now = new Date();
    const previousMonth = new Date(now.getFullYear(), now.getMonth() - 1, 1);
    return `${previousMonth.getFullYear()}-${String(previousMonth.getMonth() + 1).padStart(2, "0")}`;
  });
  const [result, setResult] = useState<{ key: string; data?: PlatformFeePeriodStatistics; error?: string } | null>(null);
  const requestKey = period;
  const validPeriod = /^\d{4}-(0[1-9]|1[0-2])$/.test(period) && Number(period.slice(0, 4)) > 0;
  const loading = validPeriod && result?.key !== requestKey;
  const data = result?.key === requestKey ? result.data : undefined;
  const error = result?.key === requestKey ? result.error : undefined;

  useEffect(() => {
    if (!validPeriod) return;
    const controller = new AbortController();
    const [year, month] = period.split("-").map(Number);
    void platformFeeApi.getPeriodStatistics(year, month, controller.signal)
      .then((data) => { if (!controller.signal.aborted) setResult({ key: requestKey, data }); })
      .catch((error: unknown) => {
        if (!controller.signal.aborted) setResult({ key: requestKey, error: getApiErrorMessage(error) || "Unable to load billing statistics." });
      });
    return () => controller.abort();
  }, [period, requestKey, validPeriod]);

  const percentage = data && data.totalAmount > 0 ? Math.min(100, data.paidAmount / data.totalAmount * 100) : 0;
  return (
    <SharedModal isOpen scrollBody onClose={onClose} title="Monthly fee statistics" maxWidthClass="max-w-3xl">
      <div className="space-y-5">
        <p className="text-sm text-slate-600">
          By billing month, including payments settled later. Amounts reflect refund credits applied to these bills.
          List search and status filters do not affect this summary.
        </p>
        <div className="flex flex-wrap items-end gap-3">
          <label className="space-y-1 text-sm font-semibold text-slate-700">
            <span className="block">Billing month</span>
            <input type="month" min="0001-01" max="9999-12" value={period} onChange={(event) => setPeriod(event.target.value)}
              className="min-h-11 rounded-lg border border-slate-300 bg-white px-3 focus-visible:outline-2 focus-visible:outline-emerald-600" />
          </label>
        </div>
        {!validPeriod && <p role="alert" className="text-sm text-red-700">Select a valid billing month.</p>}
        {loading && <p role="status" className="py-10 text-center text-sm text-slate-600">Loading billing statistics…</p>}
        {error && <p role="alert" className="rounded-lg bg-red-50 p-4 text-sm text-red-700">{error} Close and reopen this dialog to try again.</p>}
        {data && <>
          {data.totalShops === 0 && <p role="status" className="rounded-lg bg-slate-50 p-4 text-sm text-slate-600">No statements have been issued for this month. This does not include fees awaiting billing.</p>}
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {[
              { label: "Total billed", value: money(data.totalAmount), hint: `${data.totalShops} shops with issued bills`, tone: "text-slate-900" },
              { label: "Collected", value: money(data.paidAmount), hint: `${data.paidShops} shops with paid bills above 0đ`, tone: "text-emerald-700" },
              { label: "Outstanding", value: money(data.outstandingAmount), hint: `${data.unpaidShops} shops yet to pay`, tone: "text-amber-700" },
              { label: "Overdue", value: money(data.overdueAmount), hint: `${data.overdueShops} overdue shops · included in outstanding`, tone: "text-red-700" },
              { label: "No payment required", value: String(data.zeroFeeShops), hint: "Shops with a 0đ bill · excluded from paid shops", tone: "text-slate-900" },
            ].map((item) => <div key={item.label} className="rounded-xl border border-slate-200 bg-white p-4">
              <p className="text-sm font-medium text-slate-600">{item.label}</p>
              <p className={`mt-2 break-words text-2xl font-bold tabular-nums ${item.tone}`}>{item.value}</p>
              <p className="mt-2 text-xs text-slate-600">{item.hint}</p>
            </div>)}
          </div>
          <div className="rounded-xl bg-emerald-50 p-4">
            <div className="mb-2 flex justify-between gap-3 text-sm font-semibold text-emerald-900">
              <span>Collection progress</span><span>{percentage.toFixed(1)}%</span>
            </div>
            <progress aria-label="Percentage of billed fees collected" value={percentage} max={100} className="h-3 w-full accent-emerald-600" />
            <p className="mt-2 text-xs text-emerald-900">Collected is the value of bills marked Paid; zero-value bills do not count as money received.</p>
          </div>
          <p className="text-xs text-slate-500">Updated: {new Date(data.asOf).toLocaleString()}</p>
        </>}
      </div>
    </SharedModal>
  );
}
