import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { APP_PATHS } from "@/app/router/paths";
import {
  ArrowLeft,
  CalendarClock,
  CalendarDays,
  CheckCircle2,
  Receipt,
  ShoppingBag,
  SlidersHorizontal,
  History,
  RefreshCw,
  FileText,
  Store,
} from "lucide-react";
import { platformFeeApi } from "@/features/platform-fees/api/platformFeeApi";
import type { PlatformFeeStatementDetail } from "@/features/platform-fees/types/platformFee.types";
import { StatusBadge } from "@/features/platform-fees/components/StatusBadge";
import { getApiErrorMessage } from "@/shared/api/httpClient";
import { RecordId } from "@/shared/components/RecordId";

const formatVnd = (amount: number) => {
  return new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
  }).format(amount);
};

const formatDateTime = (dateString?: string | null) => {
  if (!dateString) return "—";
  return new Date(dateString).toLocaleString("vi-VN", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });
};

export function AdminPlatformFeeDetailPage() {
  const { statementId = "" } = useParams<{ statementId: string }>();
  return <StatementDetail key={statementId} statementId={statementId} />;
}

function StatementDetail({ statementId }: { statementId: string }) {
  const [error, setError] = useState<string | null>(null);
  const [statement, setStatement] = useState<PlatformFeeStatementDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<"orders" | "adjustments" | "payments" | "histories">(
    "orders",
  );

  useEffect(() => {
    let isMounted = true;

    platformFeeApi
      .getAdminStatementDetail(statementId)
      .then((data) => {
        if (isMounted) {
          setStatement(data);
        }
      })
      .catch((err) => {
        if (isMounted) {
          setError(getApiErrorMessage(err) || "Failed to load statement details.");
        }
      })
      .finally(() => {
        if (isMounted) {
          setLoading(false);
        }
      });

    return () => {
      isMounted = false;
    };
  }, [statementId]);

  return (
    <div className="space-y-6">
      <Link to={APP_PATHS.ADMIN_PLATFORM_FEE} className="inline-flex items-center gap-2 rounded-md text-sm font-medium text-slate-600 transition-colors hover:text-slate-900 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-700">
        <ArrowLeft aria-hidden="true" className="size-4" /> Back to statements
      </Link>
      <header className="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
        <div className="space-y-1.5">
          <h1 className="text-2xl font-bold tracking-tight text-neutral-900">Statement Details</h1>
          <p className="text-sm text-text-secondary">Review shop fees, refund credits, payments and statement history.</p>
        </div>
      </header>
        {/* Header */}
        <div className="flex flex-wrap items-center justify-between gap-4 rounded-xl border border-border-default bg-white p-5 shadow-xs">
          <div className="flex min-w-0 items-center gap-3">
            {statement?.shopLogoUrl ? (
              <img
                src={statement.shopLogoUrl}
                alt={statement.shopName}
                className="size-9 shrink-0 rounded-xl object-cover border border-slate-200 shadow-2xs"
              />
            ) : (
              <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-emerald-100 text-emerald-700">
                <Store className="h-5 w-5" />
              </span>
            )}
            <div className="min-w-0">
              <div className="flex flex-wrap items-center gap-2">
                <h2 className="text-lg font-semibold text-text-primary">
                  {statement
                    ? `${statement.shopName} — Period ${statement.billingMonth}/${statement.billingYear}`
                    : "Statement Detail"}
                </h2>
                {statement && <StatusBadge status={statement.status} size="sm" />}
              </div>
              {statement?.shopAddress && <p className="mt-1 text-sm text-text-secondary">{statement.shopAddress}</p>}
              {statementId && (
                <RecordId
                  id={statementId}
                  variant="full"
                  className="mt-2"
                />
              )}
            </div>
          </div>
        </div>

        {/* Statement content */}
        {loading ? (
          <div role="status" className="flex h-72 flex-col items-center justify-center rounded-xl border border-border-default bg-white text-text-secondary">
            <RefreshCw className="h-8 w-8 animate-spin text-emerald-600 mb-2" />
            <p className="text-xs font-semibold">Loading statement details...</p>
          </div>
        ) : !statement ? (
          <div role="alert" className="rounded-xl border border-border-default bg-white p-8 text-center text-text-secondary">
            <Receipt className="mx-auto h-10 w-10 text-slate-300 mb-2" />
            <p className="text-sm font-semibold">{error || "Statement details could not be found."}</p>
          </div>
        ) : (
          <div className="space-y-6">
            {statement.paymentPauseReason && (
              <div role="status" className="rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-950">
                Payment is paused due to platform PayOS configuration. Verify and save the payment credentials to restore a new 7-day deadline.
              </div>
            )}
            <section aria-label="Statement summary metrics" className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
              {[
                { label: "Total Platform Fee", value: formatVnd(statement.totalFeeAmount), icon: Receipt, tone: "bg-neutral-100 text-neutral-600" },
                { label: "Due Date", value: formatDateTime(statement.dueAt), icon: CalendarClock, tone: "bg-amber-50 text-amber-600" },
                { label: "Issued Date", value: formatDateTime(statement.issuedAt), icon: CalendarDays, tone: "bg-blue-50 text-blue-600" },
                { label: "Paid Date", value: statement.paidAt ? formatDateTime(statement.paidAt) : "Unpaid", icon: CheckCircle2, tone: "bg-emerald-50 text-emerald-600" },
              ].map(({ label, value, icon: Icon, tone }) => (
                <div key={label} className="rounded-xl border border-slate-200 bg-white p-4 shadow-xs">
                  <div className="flex items-center gap-3">
                    <span className={`flex size-10 shrink-0 items-center justify-center rounded-lg ${tone}`}>
                      <Icon aria-hidden="true" className="size-5" />
                    </span>
                    <div className="min-w-0">
                      <p className="text-xs font-medium text-slate-500">{label}</p>
                      <p className="mt-0.5 break-words text-xl font-bold tabular-nums text-slate-900">{value}</p>
                    </div>
                  </div>
                </div>
              ))}
            </section>

            {/* Tabs */}
            <div className="flex flex-wrap gap-1 rounded-xl border border-border-default bg-white p-2">
              <button
                type="button"
                onClick={() => setActiveTab("orders")}
                className={`inline-flex min-h-11 items-center gap-2 rounded-lg px-4 py-2 text-sm font-semibold transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-600 ${
                  activeTab === "orders"
                    ? "bg-primary-50 text-primary-700"
                    : "text-text-secondary hover:bg-neutral-50 hover:text-text-primary"
                }`}
              >
                <ShoppingBag className="h-3.5 w-3.5" />
                Completed Orders ({statement.orders?.length || 0})
              </button>

              <button
                type="button"
                onClick={() => setActiveTab("adjustments")}
                className={`inline-flex min-h-11 items-center gap-2 rounded-lg px-4 py-2 text-sm font-semibold transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-600 ${
                  activeTab === "adjustments"
                    ? "bg-primary-50 text-primary-700"
                    : "text-text-secondary hover:bg-neutral-50 hover:text-text-primary"
                }`}
              >
                <SlidersHorizontal className="h-3.5 w-3.5" />
                Adjustments & Refunds ({statement.adjustments?.length || 0})
              </button>

              <button
                type="button"
                onClick={() => setActiveTab("payments")}
                className={`inline-flex min-h-11 items-center gap-2 rounded-lg px-4 py-2 text-sm font-semibold transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-600 ${
                  activeTab === "payments"
                    ? "bg-primary-50 text-primary-700"
                    : "text-text-secondary hover:bg-neutral-50 hover:text-text-primary"
                }`}
              >
                <History className="h-3.5 w-3.5" />
                Payments ({statement.payments?.length || 0})
              </button>

              <button
                type="button"
                onClick={() => setActiveTab("histories")}
                className={`inline-flex min-h-11 items-center gap-2 rounded-lg px-4 py-2 text-sm font-semibold transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary-600 ${
                  activeTab === "histories"
                    ? "bg-primary-50 text-primary-700"
                    : "text-text-secondary hover:bg-neutral-50 hover:text-text-primary"
                }`}
              >
                <FileText className="h-3.5 w-3.5" />
                Statement History ({statement.histories?.length || 0})
              </button>
            </div>

            {/* Tab 1: Orders */}
            {activeTab === "orders" && (
              <div className="overflow-hidden rounded-xl border border-slate-200/80 bg-white shadow-2xs">
                <div className="overflow-x-auto">
                  <table className="w-full text-left text-sm">
                    <thead className="border-b border-border-default bg-neutral-50 text-xs font-semibold text-text-secondary">
                      <tr>
                        <th className="px-4 py-3">Order ID</th>
                        <th className="px-4 py-3">Order Revenue</th>
                        <th className="px-4 py-3">Fee Rate</th>
                        <th className="px-4 py-3">Fee After Refunds</th>
                        <th className="px-4 py-3">Calculated At</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {!statement.orders || statement.orders.length === 0 ? (
                        <tr>
                          <td colSpan={5} className="py-8 text-center text-slate-400">
                            No orders associated with this statement period.
                          </td>
                        </tr>
                      ) : (
                        statement.orders.map((ord) => (
                          <tr key={ord.id} className="hover:bg-slate-50/50 transition">
                            <td className="px-4 py-3 font-mono text-slate-700 font-semibold">
                              <RecordId id={ord.orderId} />
                            </td>
                            <td className="px-4 py-3 font-bold text-slate-800">
                              {formatVnd(ord.grossAmount)}
                            </td>
                            <td className="px-4 py-3 font-medium text-slate-600">
                              {Number((ord.feeRate * 100).toFixed(4))}%
                              <span className="mt-1 block break-all font-mono text-xs text-slate-500">Version {ord.feeConfigId}</span>
                            </td>
                            <td className="px-4 py-3 font-semibold text-emerald-600">
                              {formatVnd(ord.netFeeAmount ?? Math.max(0, ord.feeAmount - (ord.refundCreditAmount ?? 0)))}
                              {(ord.refundCreditAmount ?? 0) > 0 && (
                                <div className="mt-1 space-y-0.5 text-xs font-medium text-slate-600">
                                  <p>Original fee: {formatVnd(ord.feeAmount)}</p>
                                  <p>Refund credit: −{formatVnd(ord.refundCreditAmount ?? 0)}</p>
                                  <p>Credit may be applied to this or a later bill.</p>
                                </div>
                              )}
                            </td>
                            <td className="px-4 py-3 text-slate-500">
                              {formatDateTime(ord.calculatedAt)}
                            </td>
                          </tr>
                        ))
                      )}
                    </tbody>
                  </table>
                </div>
              </div>
            )}

            {/* Tab 2: Adjustments */}
            {activeTab === "adjustments" && (
              <div className="overflow-hidden rounded-xl border border-slate-200/80 bg-white shadow-2xs">
                <div className="overflow-x-auto">
                  <table className="w-full text-left text-sm">
                    <thead className="border-b border-border-default bg-neutral-50 text-xs font-semibold text-text-secondary">
                      <tr>
                        <th className="px-4 py-3">Adjustment ID</th>
                        <th className="px-4 py-3">Type / Reason</th>
                        <th className="px-4 py-3">Amount</th>
                        <th className="px-4 py-3">Created At</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {!statement.adjustments || statement.adjustments.length === 0 ? (
                        <tr>
                          <td colSpan={4} className="py-8 text-center text-slate-400">
                            No adjustments or refunds recorded for this period.
                          </td>
                        </tr>
                      ) : (
                        statement.adjustments.map((adj) => (
                          <tr key={adj.id} className="hover:bg-slate-50/50 transition">
                            <td className="px-4 py-3 font-mono text-slate-600"><RecordId id={adj.id} /></td>
                            <td className="px-4 py-3 text-slate-800">{adj.reason || (adj.refundId ? "Refund credit" : "Fee adjustment")}</td>
                            <td className="px-4 py-3 font-bold text-slate-900">
                              {formatVnd(adj.amount)}
                            </td>
                            <td className="px-4 py-3 text-slate-500">
                              {formatDateTime(adj.createdAt)}
                            </td>
                          </tr>
                        ))
                      )}
                    </tbody>
                  </table>
                </div>
              </div>
            )}

            {/* Tab 3: Payments */}
            {activeTab === "payments" && (
              <div className="overflow-hidden rounded-xl border border-slate-200/80 bg-white shadow-2xs">
                <div className="overflow-x-auto">
                  <table className="w-full text-left text-sm">
                    <thead className="border-b border-border-default bg-neutral-50 text-xs font-semibold text-text-secondary">
                      <tr>
                        <th className="px-4 py-3">Payment ID</th>
                        <th className="px-4 py-3">Method</th>
                        <th className="px-4 py-3">Amount</th>
                        <th className="px-4 py-3">Transaction ID</th>
                        <th className="px-4 py-3">Status</th>
                        <th className="px-4 py-3">Paid Date</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {!statement.payments || statement.payments.length === 0 ? (
                        <tr>
                          <td colSpan={6} className="py-8 text-center text-slate-400">
                            No payment transactions recorded yet.
                          </td>
                        </tr>
                      ) : (
                        statement.payments.map((p) => (
                          <tr key={p.id} className="hover:bg-slate-50/50 transition">
                            <td className="px-4 py-3 font-mono text-slate-600 text-[11px]">
                              {p.id}
                            </td>
                            <td className="px-4 py-3 font-semibold text-slate-800">
                              {p.paymentMethod === "OnlinePayment" ||
                              String(p.paymentMethod) === "0"
                                ? "Online Payment (PayOS)"
                                : p.paymentMethod === "PayAtShop" || String(p.paymentMethod) === "1"
                                  ? "Manual / Shop Payment"
                                  : p.paymentMethod || "Direct Payment"}
                            </td>
                            <td className="px-4 py-3 font-semibold text-emerald-600">
                              {formatVnd(p.amount)}
                            </td>
                            <td className="px-4 py-3 font-mono text-slate-500 text-[11px]">
                              {p.gatewayTransactionId || "—"}
                            </td>
                            <td className="px-4 py-3">
                              <span
                                className={`inline-flex rounded-full px-2 py-0.5 text-xs font-bold border ${
                                  p.status === "Succeeded"
                                    ? "bg-emerald-50 text-emerald-700 border-emerald-200"
                                    : p.status === "Pending"
                                      ? "bg-amber-50 text-amber-700 border-amber-200"
                                      : "bg-rose-50 text-rose-700 border-rose-200"
                                }`}
                              >
                                {p.status}
                              </span>
                            </td>
                            <td className="px-4 py-3 text-slate-500">
                              {formatDateTime(p.paidAt || p.createdAt)}
                            </td>
                          </tr>
                        ))
                      )}
                    </tbody>
                  </table>
                </div>
              </div>
            )}

            {/* Tab 4: Histories */}
            {activeTab === "histories" && (
              <div className="overflow-hidden rounded-xl border border-slate-200/80 bg-white shadow-2xs">
                <div className="overflow-x-auto">
                  <table className="w-full text-left text-sm">
                    <thead className="border-b border-border-default bg-neutral-50 text-xs font-semibold text-text-secondary">
                      <tr>
                        <th className="px-4 py-3">Timestamp</th>
                        <th className="px-4 py-3">Change Type</th>
                        <th className="px-4 py-3">Previous</th>
                        <th className="px-4 py-3">Adjustment</th>
                        <th className="px-4 py-3">New Amount</th>
                        <th className="px-4 py-3">Reason</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-slate-100">
                      {!statement.histories || statement.histories.length === 0 ? (
                        <tr>
                          <td colSpan={6} className="py-8 text-center text-slate-400">
                            No history records logged for this statement.
                          </td>
                        </tr>
                      ) : (
                        statement.histories.map((h) => (
                          <tr key={h.id} className="hover:bg-slate-50/50 transition">
                            <td className="px-4 py-3 text-slate-500 whitespace-nowrap">
                              {formatDateTime(h.createdAt)}
                            </td>
                            <td className="px-4 py-3">
                              <span className="inline-flex rounded-full bg-slate-100 px-2 py-0.5 text-xs font-bold text-slate-700">
                                {h.changeType === "PaymentPaused" ? "Payment paused" : h.changeType === "PaymentResumed" ? "Payment restored" : h.changeType}
                              </span>
                            </td>
                            <td className="px-4 py-3 text-slate-600">
                              {formatVnd(h.previousAmount)}
                            </td>
                            <td
                              className={`px-4 py-3 font-bold ${(h.adjustmentAmount ?? h.changeAmount) < 0 ? "text-rose-600" : "text-emerald-600"}`}
                            >
                              {(h.adjustmentAmount ?? h.changeAmount) > 0
                                ? `+${formatVnd(h.adjustmentAmount ?? h.changeAmount)}`
                                : formatVnd(h.adjustmentAmount ?? h.changeAmount)}
                            </td>
                            <td className="px-4 py-3 font-semibold text-slate-900">
                              {formatVnd(h.newAmount)}
                            </td>
                            <td
                              className="px-4 py-3 text-slate-700 max-w-xs truncate"
                              title={h.reason}
                            >
                              {h.reason || "—"}
                            </td>
                          </tr>
                        ))
                      )}
                    </tbody>
                  </table>
                </div>
              </div>
            )}
          </div>
        )}

    </div>
  );
}
