import { useEffect, useState, useCallback } from "react";
import { useParams, Link } from "react-router-dom";
import {
  ArrowLeft,
  Receipt,
  CreditCard,
  ShoppingBag,
  SlidersHorizontal,
  History,
  RefreshCw,
} from "lucide-react";
import { ownerShopApi, type OwnerShop } from "@/features/shops/api/ownerShopApi";
import { platformFeeApi } from "@/features/platform-fees/api/platformFeeApi";
import type {
  PlatformFeeStatementDetail,
  PlatformFeePaymentLinkResponse,
} from "@/features/platform-fees/types/platformFee.types";
import { normalizeStatementStatus } from "@/features/platform-fees/utils/platformFeeUtils";
import { StatusBadge } from "@/features/platform-fees/components/StatusBadge";
import { PlatformFeePaymentModal } from "@/features/platform-fees/components/PlatformFeePaymentModal";
import { toast } from "@/shared/stores/toastStore";
import { getApiErrorMessage } from "@/shared/api/httpClient";

export function PlatformFeeDetailPage() {
  const { statementId } = useParams<{ statementId: string }>();

  const [shop, setShop] = useState<OwnerShop | null>(null);
  const [statement, setStatement] = useState<PlatformFeeStatementDetail | null>(null);
  const [loading, setLoading] = useState(true);

  // Active tab state
  const [activeTab, setActiveTab] = useState<"orders" | "adjustments" | "payments">("orders");

  // Payment modal state
  const [paymentData, setPaymentData] = useState<PlatformFeePaymentLinkResponse | null>(null);
  const [isCreatingLink, setIsCreatingLink] = useState(false);

  const loadData = useCallback(async () => {
    if (!statementId) return;
    setLoading(true);
    try {
      const shopData = await ownerShopApi.getMyShopProfile();
      setShop(shopData);

      const stmtData = await platformFeeApi.getStatementDetail(shopData.id, statementId);
      setStatement(stmtData);
    } catch (error) {
      toast.error(getApiErrorMessage(error) || "Unable to load fee statement details.");
    } finally {
      setLoading(false);
    }
  }, [statementId]);

  useEffect(() => {
    void loadData();
  }, [loadData]);

  // Handle Pay Online (UC-81)
  const handleOpenPayment = async () => {
    if (!shop?.id || !statement?.id) return;
    setIsCreatingLink(true);
    try {
      const linkData = await platformFeeApi.createPaymentLink(shop.id, statement.id);
      setPaymentData(linkData);
    } catch (error) {
      toast.error(getApiErrorMessage(error) || "Unable to generate payment link.");
    } finally {
      setIsCreatingLink(false);
    }
  };

  const formatVnd = (amount: number) => {
    return new Intl.NumberFormat("vi-VN", {
      style: "currency",
      currency: "VND",
    }).format(amount);
  };

  if (loading) {
    return (
      <div className="flex h-96 flex-col items-center justify-center text-slate-400">
        <RefreshCw className="h-8 w-8 animate-spin text-emerald-600 mb-3" />
        <p className="text-sm font-medium">Loading statement details...</p>
      </div>
    );
  }

  if (!statement) {
    return (
      <div className="rounded-2xl border border-slate-200 bg-white p-12 text-center">
        <Receipt className="mx-auto h-12 w-12 text-slate-300 mb-3" />
        <h3 className="text-base font-bold text-slate-900">Fee Statement Not Found</h3>
        <p className="mt-1 text-xs text-slate-500">
          The requested fee statement does not exist or you do not have permission to view it.
        </p>
        <Link
          to="/store-owner/platform-fees"
          className="mt-4 inline-flex items-center gap-1.5 rounded-xl bg-slate-900 px-4 py-2 text-xs font-bold text-white hover:bg-slate-800"
        >
          <ArrowLeft className="h-4 w-4" /> Back to Statements
        </Link>
      </div>
    );
  }

  const normalizedStatus = normalizeStatementStatus(statement.status);
  const canPay = normalizedStatus === "Issued" || normalizedStatus === "Overdue";

  return (
    <div className="space-y-6 pb-12">
      {/* Back button */}
      <div>
        <Link
          to="/store-owner/platform-fees"
          className="inline-flex items-center gap-1.5 text-xs font-bold text-slate-500 hover:text-slate-800 transition-colors"
        >
          <ArrowLeft className="h-4 w-4" /> Back to Fee Statements
        </Link>
      </div>

      {/* Statement Header Card */}
      <div className="overflow-hidden rounded-3xl border border-slate-200/80 bg-white shadow-xs">
        <div className="bg-gradient-to-r from-slate-900 via-slate-800 to-slate-900 p-6 sm:p-8 text-white">
          <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <div className="space-y-1">
              <div className="flex items-center gap-3">
                <span className="text-xl sm:text-2xl font-black tracking-tight">
                  Statement for Month {statement.billingMonth}/{statement.billingYear}
                </span>
                <StatusBadge status={statement.status} />
              </div>
              <p className="text-xs text-slate-400 font-mono">Statement ID: {statement.id}</p>
            </div>

            {canPay && (
              <button
                type="button"
                onClick={() => void handleOpenPayment()}
                disabled={isCreatingLink}
                className="inline-flex items-center justify-center gap-2 rounded-2xl bg-emerald-500 px-6 py-3.5 text-sm font-bold text-white shadow-lg shadow-emerald-500/20 transition-all hover:bg-emerald-600 active:scale-98 disabled:opacity-50"
              >
                {isCreatingLink ? (
                  <>
                    <RefreshCw className="h-4 w-4 animate-spin" /> Creating PayOS Link...
                  </>
                ) : (
                  <>
                    <CreditCard className="h-4 w-4" /> Pay Online via PayOS
                  </>
                )}
              </button>
            )}
          </div>
        </div>

        {/* Breakdown bar */}
        <div className="grid grid-cols-2 divide-x divide-slate-100 border-b border-slate-100 sm:grid-cols-4">
          <div className="p-5">
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
              Total Platform Fee
            </span>
            <div className="mt-1 text-xl font-black text-emerald-600">
              {formatVnd(statement.totalFeeAmount)}
            </div>
          </div>

          <div className="p-5">
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
              Payment Due Date
            </span>
            <div className="mt-1 text-sm font-bold text-slate-800">
              {statement.dueAt ? new Date(statement.dueAt).toLocaleDateString("en-US") : "—"}
            </div>
          </div>

          <div className="p-5">
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
              Issued Date
            </span>
            <div className="mt-1 text-sm font-bold text-slate-800">
              {statement.issuedAt ? new Date(statement.issuedAt).toLocaleDateString("en-US") : "—"}
            </div>
          </div>

          <div className="p-5">
            <span className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
              Settlement Date
            </span>
            <div className="mt-1 text-sm font-bold text-slate-800">
              {statement.paidAt ? new Date(statement.paidAt).toLocaleDateString("en-US") : "Unpaid"}
            </div>
          </div>
        </div>
      </div>

      {/* Tabs Control */}
      <div className="flex border-b border-slate-200">
        <button
          type="button"
          onClick={() => setActiveTab("orders")}
          className={`flex items-center gap-2 border-b-2 px-5 py-3 text-xs font-bold transition-colors ${
            activeTab === "orders"
              ? "border-emerald-600 text-emerald-700"
              : "border-transparent text-slate-500 hover:text-slate-800"
          }`}
        >
          <ShoppingBag className="h-4 w-4" />
          Completed Orders ({statement.orders?.length || 0})
        </button>

        <button
          type="button"
          onClick={() => setActiveTab("adjustments")}
          className={`flex items-center gap-2 border-b-2 px-5 py-3 text-xs font-bold transition-colors ${
            activeTab === "adjustments"
              ? "border-emerald-600 text-emerald-700"
              : "border-transparent text-slate-500 hover:text-slate-800"
          }`}
        >
          <SlidersHorizontal className="h-4 w-4" />
          Adjustments & Penalties ({statement.adjustments?.length || 0})
        </button>

        <button
          type="button"
          onClick={() => setActiveTab("payments")}
          className={`flex items-center gap-2 border-b-2 px-5 py-3 text-xs font-bold transition-colors ${
            activeTab === "payments"
              ? "border-emerald-600 text-emerald-700"
              : "border-transparent text-slate-500 hover:text-slate-800"
          }`}
        >
          <History className="h-4 w-4" />
          Payment Transactions ({statement.payments?.length || 0})
        </button>
      </div>

      {/* Tab 1: Orders breakdown */}
      {activeTab === "orders" && (
        <div className="overflow-hidden rounded-2xl border border-slate-200/80 bg-white shadow-xs">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-slate-50 text-slate-500 uppercase tracking-wider font-semibold border-b border-slate-100">
                <tr>
                  <th className="px-5 py-3.5">Order ID</th>
                  <th className="px-5 py-3.5">Gross Revenue</th>
                  <th className="px-5 py-3.5">Commission Rate</th>
                  <th className="px-5 py-3.5">Platform Fee</th>
                  <th className="px-5 py-3.5">Completion Date</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {!statement.orders || statement.orders.length === 0 ? (
                  <tr>
                    <td colSpan={5} className="py-10 text-center text-slate-400">
                      No order fee records found for this period.
                    </td>
                  </tr>
                ) : (
                  statement.orders.map((ord) => (
                    <tr key={ord.id} className="hover:bg-slate-50/50 transition">
                      <td className="px-5 py-3.5 font-mono text-slate-700 font-semibold">
                        {ord.orderId}
                      </td>
                      <td className="px-5 py-3.5 font-bold text-slate-800">
                        {formatVnd(ord.grossAmount)}
                      </td>
                      <td className="px-5 py-3.5 font-semibold text-slate-600">
                        {(ord.feeRate * 100).toFixed(1)}%
                      </td>
                      <td className="px-5 py-3.5 font-black text-emerald-600">
                        {formatVnd(ord.feeAmount)}
                      </td>
                      <td className="px-5 py-3.5 text-slate-500">
                        {new Date(ord.calculatedAt).toLocaleString("en-US")}
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
        <div className="overflow-hidden rounded-2xl border border-slate-200/80 bg-white shadow-xs">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-slate-50 text-slate-500 uppercase tracking-wider font-semibold border-b border-slate-100">
                <tr>
                  <th className="px-5 py-3.5">Adjustment Type</th>
                  <th className="px-5 py-3.5">Amount</th>
                  <th className="px-5 py-3.5">Reason</th>
                  <th className="px-5 py-3.5">Applied Date</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {!statement.adjustments || statement.adjustments.length === 0 ? (
                  <tr>
                    <td colSpan={4} className="py-10 text-center text-slate-400">
                      No adjustments or penalties recorded in this statement.
                    </td>
                  </tr>
                ) : (
                  statement.adjustments.map((adj) => (
                    <tr key={adj.id} className="hover:bg-slate-50/50 transition">
                      <td className="px-5 py-3.5 font-bold text-slate-800">{adj.type}</td>
                      <td
                        className={`px-5 py-3.5 font-extrabold ${
                          adj.amount < 0 ? "text-rose-600" : "text-emerald-600"
                        }`}
                      >
                        {formatVnd(adj.amount)}
                      </td>
                      <td className="px-5 py-3.5 text-slate-600">{adj.reason}</td>
                      <td className="px-5 py-3.5 text-slate-500">
                        {new Date(adj.createdAt).toLocaleString("en-US")}
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
        <div className="overflow-hidden rounded-2xl border border-slate-200/80 bg-white shadow-xs">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-slate-50 text-slate-500 uppercase tracking-wider font-semibold border-b border-slate-100">
                <tr>
                  <th className="px-5 py-3.5">Gateway Transaction Code</th>
                  <th className="px-5 py-3.5">Method</th>
                  <th className="px-5 py-3.5">Amount</th>
                  <th className="px-5 py-3.5">Status</th>
                  <th className="px-5 py-3.5">Timestamp</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {!statement.payments || statement.payments.length === 0 ? (
                  <tr>
                    <td colSpan={5} className="py-10 text-center text-slate-400">
                      No payment transactions recorded for this statement.
                    </td>
                  </tr>
                ) : (
                  statement.payments.map((pmt) => (
                    <tr key={pmt.id} className="hover:bg-slate-50/50 transition">
                      <td className="px-5 py-3.5 font-mono font-semibold text-slate-800">
                        {pmt.gatewayTransactionId || pmt.id}
                      </td>
                      <td className="px-5 py-3.5 font-medium text-slate-700">
                        {pmt.paymentMethod === "OnlinePayment" || String(pmt.paymentMethod) === "0"
                          ? "Online Payment (PayOS)"
                          : pmt.paymentMethod === "PayAtShop" || String(pmt.paymentMethod) === "1"
                            ? "Manual / Shop Payment"
                            : pmt.paymentMethod || "Direct Payment"}
                      </td>
                      <td className="px-5 py-3.5 font-bold text-slate-900">
                        {formatVnd(pmt.amount)}
                      </td>
                      <td className="px-5 py-3.5">
                        <span
                          className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-bold ${
                            pmt.status === "Succeeded"
                              ? "bg-emerald-50 text-emerald-700"
                              : pmt.status === "Pending"
                                ? "bg-amber-50 text-amber-700"
                                : pmt.status === "Cancelled"
                                  ? "bg-slate-100 text-slate-500"
                                  : "bg-rose-50 text-rose-700"
                          }`}
                        >
                          {pmt.status}
                        </span>
                      </td>
                      <td className="px-5 py-3.5 text-slate-500">
                        {new Date(pmt.paidAt || pmt.createdAt).toLocaleString("en-US")}
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Payment Modal */}
      <PlatformFeePaymentModal
        isOpen={Boolean(paymentData)}
        onClose={() => setPaymentData(null)}
        paymentData={paymentData}
        statementPeriodText={`Month ${statement.billingMonth}/${statement.billingYear}`}
        onPaymentSuccess={() => {
          void loadData();
        }}
      />
    </div>
  );
}
