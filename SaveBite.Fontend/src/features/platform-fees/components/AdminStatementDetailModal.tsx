import { useEffect, useState } from "react";
import {
  X,
  Receipt,
  ShoppingBag,
  SlidersHorizontal,
  History,
  RefreshCw,
  FileText,
} from "lucide-react";
import { platformFeeApi } from "../api/platformFeeApi";
import type { PlatformFeeStatementDetail } from "../types/platformFee.types";
import { StatusBadge } from "./StatusBadge";
import { toast } from "@/shared/stores/toastStore";
import { getApiErrorMessage } from "@/shared/api/httpClient";

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

interface AdminStatementDetailModalProps {
  isOpen: boolean;
  onClose: () => void;
  statementId: string | null;
  onOpenReconcile?: (statementId: string) => void;
}

export function AdminStatementDetailModal({
  isOpen,
  onClose,
  statementId,
  onOpenReconcile,
}: AdminStatementDetailModalProps) {
  const [statement, setStatement] = useState<PlatformFeeStatementDetail | null>(null);
  const [loading, setLoading] = useState(false);
  const [activeTab, setActiveTab] = useState<"orders" | "adjustments" | "payments" | "histories">(
    "orders",
  );

  useEffect(() => {
    if (!isOpen || !statementId) {
      setStatement(null);
      return;
    }

    let isMounted = true;
    setLoading(true);

    platformFeeApi
      .getAdminStatementDetail(statementId)
      .then((data) => {
        if (isMounted) {
          setStatement(data);
        }
      })
      .catch((err) => {
        if (isMounted) {
          toast.error(getApiErrorMessage(err) || "Failed to load statement details.");
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
  }, [isOpen, statementId]);

  if (!isOpen) return null;

  const canReconcile =
    statement && (statement.status === "Issued" || statement.status === "Overdue");

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-3 sm:p-4 bg-slate-900/60 backdrop-blur-xs transition-opacity animate-in fade-in duration-200">
      <div className="relative w-full max-w-4xl max-h-[92vh] flex flex-col overflow-hidden rounded-2xl bg-white shadow-2xl ring-1 ring-slate-900/10">
        {/* Header */}
        <div className="shrink-0 flex items-center justify-between border-b border-slate-100 bg-slate-50/70 px-6 py-4">
          <div className="flex items-center gap-2.5">
            <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-emerald-100 text-emerald-700">
              <Receipt className="h-5 w-5" />
            </span>
            <div>
              <div className="flex items-center gap-2">
                <h3 className="text-base font-bold text-slate-900">
                  {statement
                    ? `${statement.shopName} — Kỳ ${statement.billingMonth}/${statement.billingYear}`
                    : "Statement Detail"}
                </h3>
                {statement && <StatusBadge status={statement.status} size="sm" />}
              </div>
              <p className="text-xs font-mono text-slate-400">ID: {statementId}</p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg p-1 text-slate-400 hover:bg-slate-200/60 hover:text-slate-600 transition"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        {/* Modal Body */}
        {loading ? (
          <div className="flex h-72 flex-col items-center justify-center text-slate-400">
            <RefreshCw className="h-8 w-8 animate-spin text-emerald-600 mb-2" />
            <p className="text-xs font-semibold">Loading statement details...</p>
          </div>
        ) : !statement ? (
          <div className="p-8 text-center text-slate-400">
            <Receipt className="mx-auto h-10 w-10 text-slate-300 mb-2" />
            <p className="text-sm font-semibold">Statement details could not be found.</p>
          </div>
        ) : (
          <div className="flex-1 overflow-y-auto p-6 space-y-6">
            {/* Overview Metric Cards */}
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
              <div className="rounded-xl border border-slate-200/80 bg-slate-50/60 p-3.5">
                <span className="text-[10px] font-semibold uppercase tracking-wider text-slate-400">
                  Total Platform Fee
                </span>
                <div className="mt-1 text-lg font-black text-emerald-600">
                  {formatVnd(statement.totalFeeAmount)}
                </div>
              </div>

              <div className="rounded-xl border border-slate-200/80 bg-slate-50/60 p-3.5">
                <span className="text-[10px] font-semibold uppercase tracking-wider text-slate-400">
                  Due Date
                </span>
                <div className="mt-1 text-xs font-bold text-slate-800">
                  {formatDateTime(statement.dueAt)}
                </div>
              </div>

              <div className="rounded-xl border border-slate-200/80 bg-slate-50/60 p-3.5">
                <span className="text-[10px] font-semibold uppercase tracking-wider text-slate-400">
                  Issued Date
                </span>
                <div className="mt-1 text-xs font-bold text-slate-800">
                  {formatDateTime(statement.issuedAt)}
                </div>
              </div>

              <div className="rounded-xl border border-slate-200/80 bg-slate-50/60 p-3.5">
                <span className="text-[10px] font-semibold uppercase tracking-wider text-slate-400">
                  Paid Date
                </span>
                <div className="mt-1 text-xs font-bold text-slate-800">
                  {statement.paidAt ? formatDateTime(statement.paidAt) : "— (Unpaid)"}
                </div>
              </div>
            </div>

            {/* Tabs */}
            <div className="flex border-b border-slate-200">
              <button
                type="button"
                onClick={() => setActiveTab("orders")}
                className={`flex items-center gap-1.5 border-b-2 px-4 py-2.5 text-xs font-bold transition-colors ${
                  activeTab === "orders"
                    ? "border-emerald-600 text-emerald-700"
                    : "border-transparent text-slate-500 hover:text-slate-800"
                }`}
              >
                <ShoppingBag className="h-3.5 w-3.5" />
                Completed Orders ({statement.orders?.length || 0})
              </button>

              <button
                type="button"
                onClick={() => setActiveTab("adjustments")}
                className={`flex items-center gap-1.5 border-b-2 px-4 py-2.5 text-xs font-bold transition-colors ${
                  activeTab === "adjustments"
                    ? "border-emerald-600 text-emerald-700"
                    : "border-transparent text-slate-500 hover:text-slate-800"
                }`}
              >
                <SlidersHorizontal className="h-3.5 w-3.5" />
                Adjustments & Refunds ({statement.adjustments?.length || 0})
              </button>

              <button
                type="button"
                onClick={() => setActiveTab("payments")}
                className={`flex items-center gap-1.5 border-b-2 px-4 py-2.5 text-xs font-bold transition-colors ${
                  activeTab === "payments"
                    ? "border-emerald-600 text-emerald-700"
                    : "border-transparent text-slate-500 hover:text-slate-800"
                }`}
              >
                <History className="h-3.5 w-3.5" />
                Payments ({statement.payments?.length || 0})
              </button>

              <button
                type="button"
                onClick={() => setActiveTab("histories")}
                className={`flex items-center gap-1.5 border-b-2 px-4 py-2.5 text-xs font-bold transition-colors ${
                  activeTab === "histories"
                    ? "border-emerald-600 text-emerald-700"
                    : "border-transparent text-slate-500 hover:text-slate-800"
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
                  <table className="w-full text-left text-xs">
                    <thead className="bg-slate-50 text-slate-500 uppercase tracking-wider font-semibold border-b border-slate-100">
                      <tr>
                        <th className="px-4 py-3">Order ID</th>
                        <th className="px-4 py-3">Order Revenue</th>
                        <th className="px-4 py-3">Fee Rate</th>
                        <th className="px-4 py-3">Fee Amount</th>
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
                              {ord.orderId}
                            </td>
                            <td className="px-4 py-3 font-bold text-slate-800">
                              {formatVnd(ord.grossAmount)}
                            </td>
                            <td className="px-4 py-3 font-medium text-slate-600">
                              {(ord.feeRate * 100).toFixed(1)}%
                            </td>
                            <td className="px-4 py-3 font-black text-emerald-600">
                              {formatVnd(ord.feeAmount)}
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
                  <table className="w-full text-left text-xs">
                    <thead className="bg-slate-50 text-slate-500 uppercase tracking-wider font-semibold border-b border-slate-100">
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
                            <td className="px-4 py-3 font-mono text-slate-600">{adj.id}</td>
                            <td className="px-4 py-3 text-slate-800">{adj.reason || adj.type}</td>
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
                  <table className="w-full text-left text-xs">
                    <thead className="bg-slate-50 text-slate-500 uppercase tracking-wider font-semibold border-b border-slate-100">
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
                            <td className="px-4 py-3 font-black text-emerald-600">
                              {formatVnd(p.amount)}
                            </td>
                            <td className="px-4 py-3 font-mono text-slate-500 text-[11px]">
                              {p.gatewayTransactionId || "—"}
                            </td>
                            <td className="px-4 py-3">
                              <span
                                className={`inline-flex rounded-full px-2 py-0.5 text-[10px] font-bold border ${
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
                  <table className="w-full text-left text-xs">
                    <thead className="bg-slate-50 text-slate-500 uppercase tracking-wider font-semibold border-b border-slate-100">
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
                              <span className="inline-flex rounded-full bg-slate-100 px-2 py-0.5 text-[10px] font-bold text-slate-700">
                                {h.changeType}
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
                            <td className="px-4 py-3 font-black text-slate-900">
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

        {/* Footer */}
        <div className="shrink-0 flex items-center justify-between border-t border-slate-100 bg-slate-50/70 px-6 py-3">
          <div>
            {canReconcile && onOpenReconcile && statement && (
              <button
                type="button"
                onClick={() => {
                  onClose();
                  onOpenReconcile(statement.id);
                }}
                className="inline-flex items-center gap-1.5 rounded-xl bg-emerald-600 px-3 py-1.5 text-xs font-bold text-white shadow-xs hover:bg-emerald-700 transition"
              >
                <Receipt className="h-3.5 w-3.5" />
                Reconcile Statement
              </button>
            )}
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded-xl px-4 py-1.5 text-xs font-semibold text-slate-600 hover:bg-slate-200/60 transition"
          >
            Close
          </button>
        </div>
      </div>
    </div>
  );
}
